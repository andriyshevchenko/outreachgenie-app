# OutreachGenie MVP Implementation TODO

> **Status**: Infrastructure 100% Complete, Agent Processing 0% Complete  
> **Last Updated**: January 11, 2026  
> **Critical Gap**: Background agent service not implemented - campaigns don't process  
> **Security Alert**: 🔴 No global exception handler  
> **Architecture Note**: App runs locally on user machines, not cloud-deployed

---

## 🚨 PENDING TASKS (3 Critical)

### [ ] Problem Details Exception Handler (RFC 7807)
**Priority**: 🔴 CRITICAL - Production error exposure risk
**Status**: NOT IMPLEMENTED

**Standard**: [RFC 7807 Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807.html)

**Current Problem**: 
- No global exception handler in Program.cs
- Unhandled exceptions return stack traces to clients
- No structured error responses
- API returns 500 with implementation details exposed

**Security Risk**: Stack traces reveal:
- File paths (`C:\Users\...`)
- Internal method names
- Database schema details
- Environment configuration

**Solution**: Use ASP.NET Core's built-in Problem Details service (RFC 7807 compliant)

**Implementation Required**:

1. **Register Problem Details Service in Program.cs**:
   ```csharp
   // Add before builder.Services.AddControllers()
   builder.Services.AddProblemDetails(options =>
   {
       options.CustomizeProblemDetails = ctx =>
       {
           // Add machine name for diagnostics
           ctx.ProblemDetails.Extensions["nodeId"] = Environment.MachineName;
           
           // Add trace ID for correlation
           ctx.ProblemDetails.Extensions["traceId"] = 
               ctx.HttpContext.TraceIdentifier;
           
           // In production, sanitize sensitive data
           if (!ctx.HttpContext.RequestServices
               .GetRequiredService<IWebHostEnvironment>()
               .IsDevelopment())
           {
               ctx.ProblemDetails.Detail = null; // Remove stack trace
           }
       };
   });
   ```

2. **Enable Exception Handler Middleware in Program.cs**:
   ```csharp
   // Add after app.Build(), before app.UseSerilogRequestLogging()
   
   if (!app.Environment.IsDevelopment())
   {
       app.UseExceptionHandler();
       app.UseHsts();
   }
   else
   {
       // Development: Show detailed errors in Problem Details format
       app.UseExceptionHandler();
   }
   
   app.UseStatusCodePages(); // Handles 4xx errors
   ```

3. **Optional: Custom Exception Handler for Specific Types**:
   ```csharp
   // For advanced scenarios with custom exception mapping
   app.UseExceptionHandler(exceptionHandlerApp =>
   {
       exceptionHandlerApp.Run(async context =>
       {
           var exceptionFeature = context.Features
               .Get<IExceptionHandlerFeature>();
           
           var exception = exceptionFeature?.Error;
           
           var problemDetailsService = context.RequestServices
               .GetRequiredService<IProblemDetailsService>();
           
           // Map custom exceptions to HTTP status codes
           var (status, title, type) = exception switch
           {
               ArgumentException => (400, "Invalid argument", 
                   "https://errors.outreachgenie.com/invalid-argument"),
               KeyNotFoundException => (404, "Resource not found", 
                   "https://errors.outreachgenie.com/not-found"),
               UnauthorizedAccessException => (401, "Unauthorized", 
                   "https://errors.outreachgenie.com/unauthorized"),
               _ => (500, "Internal server error", 
                   "https://errors.outreachgenie.com/internal-error")
           };
           
           context.Response.StatusCode = status;
           
           await problemDetailsService.WriteAsync(new ProblemDetailsContext
           {
               HttpContext = context,
               ProblemDetails = 
               {
                   Status = status,
                   Title = title,
                   Type = type,
                   Detail = app.Environment.IsDevelopment() 
                       ? exception?.Message 
                       : null
               }
           });
       });
   });
   ```

4. **Example Problem Details Response**:
   ```json
   {
     "type": "https://errors.outreachgenie.com/not-found",
     "title": "Resource not found",
     "status": 404,
     "traceId": "00-abc123...-00",
     "nodeId": "USER-PC"
   }
   ```

5. **Create Custom Exception Types** (Optional):
   ```csharp
   // server/OutreachGenie.Domain/Exceptions/
   public class CampaignNotFoundException : Exception 
   {
       public CampaignNotFoundException(Guid campaignId) 
           : base($"Campaign {campaignId} not found") { }
   }
   
   public class TaskExecutionException : Exception 
   {
       public TaskExecutionException(string message, Exception inner) 
           : base(message, inner) { }
   }
   ```

6. **Testing**:
   - Unit test: Verify Problem Details response structure
   - Integration test: Throw exception, verify RFC 7807 format
   - Integration test: Verify production hides stack traces
   - Integration test: Verify development shows details
   - Test custom exception mappings (404, 400, 401)
   - Verify structured logging captures full exception

**Benefits**:
- ✅ Standard RFC 7807 format (client libraries understand it)
- ✅ No custom middleware needed (built into ASP.NET Core)
- ✅ Automatic status code page handling
- ✅ Correlation IDs for debugging
- ✅ Safe error exposure in production

**Estimated**: 2-3 hours

---

### [ ] Secure End-User Secrets (Windows & Mac)
**Priority**: 🔴 CRITICAL - Users need secure credential storage
**Status**: NOT IMPLEMENTED

**Context**: App installs on user machines - users enter their own API keys

**Problem**: No secure storage for end-user credentials:
- Users' OpenAI API keys currently in plain text config
- Users' LinkedIn session cookies in plain text
- Need OS-level credential manager integration

**Windows Solution**: Windows Credential Manager via CredentialManagement NuGet
```csharp
// Install: CredentialManagement (Windows only)
// Store API key
using (var cred = new Credential())
{
    cred.Target = "OutreachGenie.OpenAI";
    cred.Username = "ApiKey";
    cred.Password = apiKey;
    cred.Type = CredentialType.Generic;
    cred.PersistanceType = PersistanceType.LocalMachine;
    cred.Save();
}
```

**macOS Solution**: macOS Keychain via security command
```bash
# Store via CLI
security add-generic-password -a "OutreachGenie" -s "OpenAI.ApiKey" -w "sk-..."

# Retrieve in C#
var process = Process.Start(new ProcessStartInfo
{
    FileName = "security",
    Arguments = "find-generic-password -a OutreachGenie -s OpenAI.ApiKey -w",
    RedirectStandardOutput = true
});
var apiKey = process.StandardOutput.ReadToEnd().Trim();
```

**Implementation Steps**:
1. Create `ISecureStorage` interface (Windows + Mac implementations)
2. Add NuGet: `CredentialManagement` (Windows), use `security` CLI (Mac)
3. Update SettingsController to use secure storage for API keys
4. Update frontend Settings page with "Save Securely" button
5. Migrate existing plain-text settings on first launch
6. Show "🔒 Secured" badge for stored credentials

**User Experience**:
- Settings page: Enter API key → Click "Save Securely"
- App stores in OS credential manager
- Confirm with "✓ API Key Secured" message
- Never show actual key again (only "••••••••" mask)

**Estimated**: 1-2 hours

---

### [ ] Integrate .NET Aspire for Orchestration
**Priority**: 🟡 MEDIUM - Simplifies local development and deployment
**Status**: NOT IMPLEMENTED

**What is .NET Aspire?**
.NET Aspire is Microsoft's opinionated stack for building observable, cloud-ready distributed apps. It provides:
- **AppHost**: Code-first orchestration (replaces manual "start backend, start frontend")
- **Service Discovery**: Automatic service-to-service communication
- **Dashboard**: Built-in observability (logs, traces, metrics in one UI)
- **Resilience**: Built-in retry policies and health checks
- **Simplified Deployment**: Easy packaging for user machines

**Benefits for OutreachGenie**:
1. **User Experience**: Single-click start (no manual terminal juggling)
2. **Observability**: Users can see what agent is doing in real-time dashboard
3. **Reliability**: Automatic retry logic for LinkedIn/OpenAI calls
4. **Simplified Install**: Package as single executable with orchestration built-in

**Implementation**:

1. **Add Aspire Projects**:
   ```bash
   cd server
   dotnet new aspire-apphost -n OutreachGenie.AppHost
   dotnet new aspire-servicedefaults -n OutreachGenie.ServiceDefaults
   ```

2. **Configure AppHost** (server/OutreachGenie.AppHost/Program.cs):
   ```csharp
   var builder = DistributedApplication.CreateBuilder(args);
   
   // Add SQLite (local file)
   var database = builder.AddSqlite("outreachgenie-db")
       .WithDataDirectory("~/Documents/OutreachGenie");
   
   // Add Backend API
   var api = builder.AddProject<Projects.OutreachGenie_Api>("api")
       .WithReference(database)
       .WithHttpsEndpoint(5000);
   
   // Add Frontend (Vite dev server)
   var frontend = builder.AddNpmApp("frontend", "../..")
       .WithHttpEndpoint(8081)
       .WithReference(api);
   
   builder.Build().Run();
   ```

3. **Add Service Defaults** (Program.cs in Api project):
   ```csharp
   builder.AddServiceDefaults(); // Health checks, telemetry, resilience
   ```

4. **Benefits Enabled**:
   - `dotnet run --project server/OutreachGenie.AppHost` starts everything
   - Aspire Dashboard at http://localhost:15000 (logs, traces, metrics)
   - Automatic service discovery (frontend finds backend)
   - Health checks for all services

5. **User Installation**:
   - Package AppHost as self-contained exe
   - User runs OutreachGenie.exe → starts backend + frontend automatically
   - Dashboard shows: Campaign progress, API calls, errors

**Prerequisites**:
- .NET 10 SDK (already required)
- Docker Desktop (for dashboard only, optional)

**Testing**:
- Verify all services start via AppHost
- Verify dashboard shows logs and traces
- Update E2E tests to use AppHost

**Estimated**: 4-5 hours

---

### [ ] MVP-0: Agent Background Service (THE MISSING BRAIN)
**Priority**: 🔴 BLOCKING - Makes campaigns actually process
**Status**: NOT IMPLEMENTED

**File Structure**:
```
server/OutreachGenie.Api/Services/
  AgentHostedService.cs         - IHostedService implementation
  AgentConfiguration.cs          - Settings (polling interval, max concurrent)
```

**Implementation Requirements**:

1. **AgentHostedService : BackgroundService**
   ```csharp
   public class AgentHostedService : BackgroundService
   {
       private readonly IServiceScopeFactory _scopeFactory;
       private readonly AgentConfiguration _config;
       private readonly ILogger<AgentHostedService> _logger;
       
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               using var scope = _scopeFactory.CreateScope();
               await ProcessActiveCampaignsAsync(scope, stoppingToken);
               await Task.Delay(_config.PollingIntervalMs, stoppingToken);
           }
       }
   }
   ```

2. **Configuration in appsettings.json**:
   ```json
   "AgentConfiguration": {
       "PollingIntervalMs": 5000,
       "MaxConcurrentCampaigns": 3,
       "TaskExecutionTimeoutMs": 300000,
       "RateLimitDelayMs": 2000
   }
   ```

3. **DI Registration in Program.cs**:
   ```csharp
   builder.Services.Configure<AgentConfiguration>(
       builder.Configuration.GetSection("AgentConfiguration"));
   builder.Services.AddHostedService<AgentHostedService>();
   ```

4. **Processing Logic**:
   - Query campaigns: `WHERE Status IN ('Initializing', 'Active')`
   - For each campaign:
     - Create scoped services (repositories, controller, MCP servers)
     - Call `DeterministicController.ReloadStateAsync(campaignId)`
     - Call `DeterministicController.SelectNextTask()`
     - If task found: Call `DeterministicController.ExecuteTaskWithLlmAsync(task)`
     - Emit SignalR: `IAgentNotificationService.NotifyTaskStatusChanged()`
     - Emit SignalR: `IAgentNotificationService.NotifyCampaignStateChanged()`
     - Handle errors: Update task.RetryCount, mark Failed if retries exhausted
   - Rate limiting: `await Task.Delay(_config.RateLimitDelayMs)` between tasks

5. **Error Handling**:
   - Catch exceptions per campaign (don't crash entire service)
   - Log to Serilog with structured context (campaignId, taskId)
   - Update task status to Failed after max retries
   - Emit error events via SignalR

6. **Graceful Shutdown**:
   - Respect CancellationToken throughout
   - Complete current task before stopping (with timeout)
   - Log shutdown event

7. **Dependencies Needed**:
   - `IServiceScopeFactory` (for scoped DbContext per campaign)
   - `ICampaignRepository`, `ITaskRepository`, `IArtifactRepository`
   - `DeterministicController` (inject dependencies)
   - `IAgentNotificationService` (singleton for SignalR)
   - `ILlmProvider` (for ExecuteTaskWithLlmAsync)
   - All registered MCP servers

8. **Testing**:
   - Unit test: Mock repositories, verify polling logic
   - Integration test: Seed campaign with tasks, verify execution
   - Verify SignalR events emitted with correct payload

**Result**: Campaigns transition Initializing → Draft → Active → Completed, LinkedIn automation executes via MCP tools
**Estimated**: 4-6 hours

---

### [ ] MVP-4: Chat-to-LLM Integration
**Priority**: 🟡 MEDIUM - Makes chat intelligent
**Status**: ChatController returns placeholder text

**Implementation Requirements**:

1. **Update ChatController.SendMessage**:
   ```csharp
   public async Task<ActionResult<ChatResponse>> SendMessage(
       [FromBody] SendMessageRequest request,
       CancellationToken cancellationToken)
   {
       // Load campaign context
       var campaign = await _campaignRepository.GetWithTasksAndArtifactsAsync(request.CampaignId);
       var contextArtifact = campaign.Artifacts.FirstOrDefault(a => a.Type == ArtifactType.Context);
       
       // Build conversation history
       var history = new List<ChatMessage> {
           new("system", $"Campaign: {campaign.Name}, Target: {campaign.TargetAudience}, Context: {contextArtifact?.Content}")
       };
       
       // Add previous chat messages from artifact
       var chatHistory = await _artifactRepository.GetByTypeAsync(campaign.Id, ArtifactType.Messages);
       history.AddRange(DeserializeChatHistory(chatHistory?.Content));
       
       // Add user message
       history.Add(new("user", request.Message));
       
       // Call LLM
       var response = await _llmProvider.GenerateResponseAsync(history, cancellationToken);
       
       // Save to chat history artifact
       history.Add(new("assistant", response));
       await SaveChatHistoryAsync(campaign.Id, history);
       
       // Emit SignalR event
       await _notificationService.NotifyChatMessageReceivedAsync(
           campaign.Id, new ChatMessage(Guid.NewGuid(), response, DateTime.UtcNow));
       
       return Ok(new ChatResponse(Guid.NewGuid(), response, DateTime.UtcNow));
   }
   ```

2. **Dependencies**:
   - Inject `ICampaignRepository`, `IArtifactRepository`
   - Inject `ILlmProvider` (already implemented)
   - Inject `IAgentNotificationService` (for SignalR)

3. **Configuration**:
   - Ensure LLM API key configured in appsettings.json or environment
   - Set appropriate model (gpt-4-turbo, claude-3-5-sonnet)
   - Configure temperature (0.7), max tokens (1000)

4. **Testing**:
   - Unit test with mock LLM provider
   - Integration test with real OpenAI API (marked [Fact(Skip = "Requires API key")])

**Estimated**: 2-3 hours

---

## ✅ COMPLETED TASKS (17/19)

### Infrastructure & Foundation
- [X] SQLite with EF Core migrations
- [X] Serilog structured logging
- [X] Domain models (Campaign, Task, Artifact, Lead)
- [X] DeterministicController state machine (12 unit tests)
- [X] Repository pattern with EF Core (29 integration tests)
- [X] Artifact storage with versioning

### MCP Integration
- [X] MCP protocol abstraction (IMcpServer, IMcpTransport)
- [X] Desktop Commander MCP server integration
- [X] Playwright MCP server integration
- [X] Fetch & Exa MCP servers integration
- [X] StdioMcpTransport with JSON-RPC 2.0 (91% coverage)

### Business Logic
- [X] LLM provider abstraction (ILlmProvider, OpenAiLlmProvider)
- [X] Lead scoring service (7 unit tests)
- [X] Campaign resume/recovery logic (4 integration tests)

### API & Real-time
- [X] API Controllers (Campaign, Chat, Task, Artifact, Settings) - 49 tests passing
- [X] SignalR hub (AgentHub with 4 event types)
- [X] AgentNotificationService (4 unit tests)

### Frontend
- [X] React + TypeScript + Vite + shadcn/ui
- [X] ChatPage with SignalR real-time connection
- [X] CampaignsPage with full CRUD operations
- [X] API client with typed models (api.ts)
- [X] 46 frontend tests passing (89.4% coverage)
- [X] 10/10 E2E Playwright tests passing

### Testing
- [X] xUnit testing infrastructure
- [X] 142 backend tests passing (96.4% pass 
