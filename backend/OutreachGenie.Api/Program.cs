// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OutreachGenie.Api.Data;
using OutreachGenie.Api.Domain.Services;
using OutreachGenie.Api.Infrastructure.Repositories;
using OutreachGenie.Api.Orchestrators.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8080", "http://localhost:8081", "http://localhost:8082")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add database context factory (allows creating scoped DbContext from singleton services)
builder.Services.AddDbContextFactory<OutreachGenieDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=outreachgenie.db"));

// Register repositories as transient (will use DbContextFactory)
builder.Services.AddTransient<ICampaignRepository, CampaignRepository>();

// Note: GenericRepository is not needed with specific repositories

// Register domain services as transient
builder.Services.AddTransient<IEventLog, EventLog>();
builder.Services.AddTransient<ITaskService, TaskService>();

// Register agent tools as transient
builder.Services.AddTransient<CampaignAgentTools>();

// Configure AI Provider (supports both Azure OpenAI and OpenAI)
string? openAIApiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
string? azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

builder.Services.AddSingleton<IChatClient>(sp =>
{
    ILogger<Program> logger = sp.GetRequiredService<ILogger<Program>>();

    // Option 1: OpenAI API (easier for testing)
    if (!string.IsNullOrWhiteSpace(openAIApiKey))
    {
        string modelId = builder.Configuration["OpenAI:Model"] ?? "gpt-4o";
        logger.LogInformation("Using OpenAI API with model: {Model}", modelId);
        OpenAI.Chat.ChatClient openAIClient = new(modelId, openAIApiKey);

        // CRITICAL: Wrap with function invocation middleware for tool calling
        // The Agent Framework uses this to intercept and execute function calls
        return new ChatClientBuilder(openAIClient.AsIChatClient())
            .UseFunctionInvocation()
            .Build();
    }

    // Option 2: Azure OpenAI
    if (!string.IsNullOrWhiteSpace(azureOpenAIEndpoint))
    {
        string deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        logger.LogInformation("Using Azure OpenAI at {Endpoint} with deployment: {Deployment}", azureOpenAIEndpoint, deploymentName);
        AzureOpenAIClient azureClient = new(new Uri(azureOpenAIEndpoint), new DefaultAzureCredential());
        ChatClient chatClient = azureClient.GetChatClient(deploymentName);

        // CRITICAL: Wrap with function invocation middleware for tool calling
        // The Agent Framework uses this to intercept and execute function calls
        return new ChatClientBuilder(chatClient.AsIChatClient())
            .UseFunctionInvocation()
            .Build();
    }

    throw new InvalidOperationException(
        "No AI provider configured. Please set either:\n" +
        "  - OpenAI: Set OpenAI:ApiKey in appsettings.json or OPENAI_API_KEY environment variable\n" +
        "  - Azure OpenAI: Set AzureOpenAI:Endpoint in appsettings.json or AZURE_OPENAI_ENDPOINT environment variable");
});

// Configure tools for direct IChatClient use (no Agent Framework)
builder.Services.AddSingleton<IReadOnlyList<AITool>>(sp =>
{
    ILogger<Program> logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Creating OutreachGenie tools for direct IChatClient");

    // Get tools instance (transient with pooled DbContext)
    CampaignAgentTools toolsInstance = sp.GetRequiredService<CampaignAgentTools>();
    List<AITool> tools =
    [
        AIFunctionFactory.Create(toolsInstance.CreateCampaign),
        AIFunctionFactory.Create(toolsInstance.CreateTask),
        AIFunctionFactory.Create(toolsInstance.CompleteTask),
        AIFunctionFactory.Create(toolsInstance.GetCampaignStatus),
        AIFunctionFactory.Create(toolsInstance.DiscoverLeads),
        AIFunctionFactory.Create(toolsInstance.ScoreLead),
    ];

    logger.LogInformation(
        "Registered {ToolCount} tools: {Tools}",
        tools.Count,
        string.Join(", ", tools.Select(t => t.Name)));

    return tools;
});

// System prompt for the assistant
builder.Services.AddSingleton<string>(sp =>
{
    return """
        You are OutreachGenie, an AI assistant for managing LinkedIn outreach campaigns.

        You have access to tools/functions to interact with the campaign system.
        Use these tools to create campaigns, manage tasks, discover leads, and score prospects.

        WORKFLOW:
        When a user asks to create a campaign:
        1. Call CreateCampaign(name, description) to create it
        2. Call CreateTask() to add necessary tasks (Lead Discovery, Lead Scoring, Message Drafting, etc.)
        3. Use DiscoverLeads() to find prospects
        4. Use ScoreLead() to prioritize leads
        5. Use GetCampaignStatus() to check progress
        6. Use CompleteTask() to mark tasks done
        
        After calling tools and completing the user's request, provide a summary of what you did.
        
        EXAMPLES:
        
        User: "Create a LinkedIn outreach campaign for SaaS CTOs"
        Response:
        [Call CreateCampaign("SaaS CTO Outreach", "Targeting CTOs at B2B SaaS companies")]
        [Call CreateTask("Lead Discovery", "Find CTOs at B2B SaaS companies", ...)]
        [Call CreateTask("Lead Scoring", "Score and prioritize leads", ...)]
        "I've created your SaaS CTO Outreach campaign with 2 initial tasks: Lead Discovery and Lead Scoring."
        
        User: "Find 20 leads for campaign X"
        Response:
        [Call DiscoverLeads(campaignId, "description", 20)]
        "I've discovered 20 leads for your campaign."

        IMPORTANT:
        - Use tools to perform actions
        - After tool calls succeed, summarize what you did
        - Don't call tools repeatedly without reason
        - If you've completed the user's request, provide a final summary
        """;
});

// Add AG-UI support
builder.Services.AddAGUI();

WebApplication app = builder.Build();

// Ensure database is created
using (IServiceScope scope = app.Services.CreateScope())
{
    OutreachGenieDbContext db = scope.ServiceProvider.GetRequiredService<OutreachGenieDbContext>();
    await db.Database.EnsureCreatedAsync();
    app.Logger.LogInformation("Database initialized");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// AG-UI removed since we're using direct IChatClient instead of AIAgent
// Use the REST API at /api/agentchat/stream for chat functionality

app.Logger.LogInformation("OutreachGenie API started successfully. Using OpenAI with model: {Model}",
    builder.Configuration["OpenAI:Model"] ?? "gpt-4o");

await app.RunAsync();

