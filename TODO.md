# OutreachGenie MVP Implementation TODO

> **Status**: Infrastructure 100% Complete, Agent Processing 0% Complete  
> **Last Updated**: January 11, 2026  
> **Critical Gap**: Background agent service not implemented - campaigns don't process

---

## 🚨 PENDING TASKS (2 Critical)

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
