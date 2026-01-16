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
        policy.WithOrigins("http://localhost:5173", "http://localhost:8081")
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
        return openAIClient.AsIChatClient();
    }

    // Option 2: Azure OpenAI
    if (!string.IsNullOrWhiteSpace(azureOpenAIEndpoint))
    {
        string deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        logger.LogInformation("Using Azure OpenAI at {Endpoint} with deployment: {Deployment}", azureOpenAIEndpoint, deploymentName);
        AzureOpenAIClient azureClient = new(new Uri(azureOpenAIEndpoint), new DefaultAzureCredential());
        ChatClient chatClient = azureClient.GetChatClient(deploymentName);
        return chatClient.AsIChatClient();
    }

    throw new InvalidOperationException(
        "No AI provider configured. Please set either:\n" +
        "  - OpenAI: Set OpenAI:ApiKey in appsettings.json or OPENAI_API_KEY environment variable\n" +
        "  - Azure OpenAI: Set AzureOpenAI:Endpoint in appsettings.json or AZURE_OPENAI_ENDPOINT environment variable");
});

// Configure Microsoft Agent Framework (singleton with transient dependencies)
builder.Services.AddSingleton<AIAgent>(sp =>
{
    IChatClient chatClient = sp.GetRequiredService<IChatClient>();
    ILogger<Program> logger = sp.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Creating OutreachGenie Agent with configured AI provider");

    // Get tools instance (transient with pooled DbContext)
    CampaignAgentTools toolsInstance = sp.GetRequiredService<CampaignAgentTools>();
    IList<AITool> tools = new List<AITool>
    {
        AIFunctionFactory.Create(toolsInstance.CreateTask),
        AIFunctionFactory.Create(toolsInstance.CompleteTask),
        AIFunctionFactory.Create(toolsInstance.GetCampaignStatus),
        AIFunctionFactory.Create(toolsInstance.DiscoverLeads),
        AIFunctionFactory.Create(toolsInstance.ScoreLead),
    };

    string agentInstructions = """
            You are OutreachGenie, an AI assistant for managing marketing and sales campaigns.

            CRITICAL RULES:
            1. Tasks must be completed in order - you cannot skip ahead
            2. When you see a pending task in the system message, focus on completing it first
            3. Use the provided tools to interact with the campaign system
            4. All actions are logged and auditable
            5. State persists in the database, not in conversation memory

            Your capabilities:
            - Create structured task lists for campaigns
            - Mark tasks as completed (this advances the campaign)
            - Discover and score leads
            - Track campaign progress

            Always confirm actions with the user before marking critical tasks as complete.
            Provide clear explanations of what you're doing and why.
            """;

    AIAgent baseAgent = chatClient.CreateAIAgent(
        name: "OutreachGenieAgent",
        instructions: agentInstructions,
        tools: tools);

    // Wrap with task enforcement middleware (transient dependencies)
    ITaskService taskService = sp.GetRequiredService<ITaskService>();
    ILogger<TaskEnforcementAgent> enforcementLogger = sp.GetRequiredService<ILogger<TaskEnforcementAgent>>();
    return new TaskEnforcementAgent(baseAgent, taskService, enforcementLogger);
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

// Map AG-UI endpoint for agent interaction
AIAgent agent = app.Services.GetRequiredService<AIAgent>();
app.MapAGUI("/api/agent", agent);

app.Logger.LogInformation("OutreachGenie API started successfully");
app.Logger.LogInformation("AG-UI endpoint: /api/agent");
app.Logger.LogInformation("Using Azure OpenAI: {Endpoint}", azureOpenAIEndpoint);

await app.RunAsync();
