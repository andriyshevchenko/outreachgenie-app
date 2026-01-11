# OutreachGenie MVP Implementation TODO

> **Status**: Infrastructure 100% Complete, Agent Processing 0% Complete  
> **Last Updated**: January 11, 2026  
> **Critical Gap**: Background agent service not implemented - campaigns don't process  
> **Architecture Note**: App runs locally on user machines, not cloud-deployed

---

## 🚨 PENDING TASKS (9 Tasks - Sorted by Priority)

### [ ] MVP-0: Agent Background Service (THE MISSING BRAIN)
**Priority**: 🔴🔴 BLOCKING - Makes campaigns actually process
**Status**: NOT IMPLEMENTED

**Critical Gap**: Campaigns are created but never processed - they stay in "Initializing" status forever. No LinkedIn automation happens.

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
               var campaignRepo = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
               var controller = scope.ServiceProvider.GetRequiredService<IDeterministicController>();
               var activeCampaigns = await campaignRepo.GetActiveCampaignsAsync();
               
               foreach (var campaign in activeCampaigns)
               {
                   await controller.ExecuteTaskWithLlmAsync(campaign.Id);
               }
               
               await Task.Delay(_config.PollingIntervalMs, stoppingToken);
           }
       }
   }
   ```

2. **Register in Program.cs**:
   ```csharp
   builder.Services.AddHostedService<AgentHostedService>();
   ```

3. **AgentConfiguration**:
   ```csharp
   public class AgentConfiguration
   {
       public int PollingIntervalMs { get; set; } = 60000; // 1 minute
       public int MaxConcurrentCampaigns { get; set; } = 3;
   }
   ```

**What This Enables**:
- ✅ Campaigns automatically process in background
- ✅ Agent navigates LinkedIn via Playwright MCP
- ✅ Leads are extracted and scored
- ✅ Artifacts are created with real data
- ✅ SignalR events notify UI in real-time
- ✅ Campaign status updates: Initializing → Active → Completed

**Success Criteria**:
- ✅ Service starts with application
- ✅ Polls for active campaigns every 60 seconds
- ✅ Processes campaigns concurrently (max 3)
- ✅ Handles errors gracefully with retry logic
- ✅ Logs all operations
- ✅ Campaigns transition through state machine correctly

**Estimated**: 6-9 hours

---

### [ ] MVP-4: Chat-to-LLM Integration
**Priority**: 🔴🔴 BLOCKING - Core chat functionality
**Status**: Returns hardcoded placeholder text

**Critical Gap**: Users can't interact with agent via chat. No AI explanations or narration.

**Current Problem**:
```csharp
// server/OutreachGenie.Api/Controllers/ChatController.cs
public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
{
    // Currently: return "Agent response placeholder"
    return Ok(new { message = "Agent response placeholder" });
}
```

**What's Needed**:

1. **Wire up to LLM service**:
   ```csharp
   [HttpPost]
   public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
   {
       var campaignContext = await _campaignRepo.GetByIdAsync(request.CampaignId);
       var artifacts = await _artifactRepo.GetByCampaignIdAsync(request.CampaignId);
       
       var systemPrompt = BuildSystemPrompt(campaignContext, artifacts);
       var response = await _llmProvider.GenerateResponseAsync(
           systemPrompt,
           request.Message,
           request.ConversationHistory
       );
       
       // Emit SignalR event for real-time UI update
       await _agentHub.Clients.All.SendAsync("ChatMessageReceived", new {
           campaignId = request.CampaignId,
           message = response,
           timestamp = DateTime.UtcNow
       });
       
       return Ok(new { message = response });
   }
   ```

2. **Frontend already wired**: ChatPage.tsx calls API correctly

**What This Enables**:
- ✅ User can ask: "Show me the leads you found"
- ✅ Agent responds with actual data from artifacts
- ✅ User can ask: "Why did you score this lead highest?"
- ✅ Agent explains its scoring heuristics
- ✅ Conversation is contextual (knows campaign state)

**Success Criteria**:
- ✅ User sends message → receives LLM response (not placeholder)
- ✅ Agent has access to campaign context and artifacts
- ✅ Conversation history maintained across messages
- ✅ Error handling for LLM API failures
- ✅ Loading states in UI
- ✅ Rate limiting applied (10 msgs/min)

**Estimated**: 2-3 hours

---

### [ ] Task Execution Wiring
**Priority**: 🔴 HIGH - Required for automation
**Status**: NOT IMPLEMENTED

**Critical Gap**: DeterministicController exists but tasks never execute. The controller has the logic but nothing calls it.

**Problem**:
- `DeterministicController.ExecuteTaskWithLlmAsync()` exists with full state machine logic
- But AgentHostedService (which should call it) doesn't exist
- So tasks remain in "Pending" status forever

**Solution**: This is automatically solved by MVP-0 (AgentHostedService)

**Dependencies**: MVP-0 must be completed first

**Estimated**: Included in MVP-0

---

### [ ] Artifact Generation & Storage
**Priority**: 🔴 HIGH - Core data persistence
**Status**: PARTIAL - Infrastructure exists, no generation happens

**Critical Gap**: Artifact storage system works, but no artifacts are generated because tasks never execute.

**What Exists**:
- ✅ Artifact entity with versioning
- ✅ ArtifactRepository with CRUD operations
- ✅ Artifact API endpoints (`/api/v1/artifacts`)
- ✅ 29 passing integration tests

**What's Missing**:
- ❌ No artifacts created during campaign execution
- ❌ No lead extraction artifacts
- ❌ No message template artifacts
- ❌ No scoring heuristics artifacts

**Example Flow (Currently Broken)**:
```
1. User creates campaign "Find CTOs in SF"
2. Background service should process it (NOT IMPLEMENTED)
3. Agent navigates LinkedIn (NOT HAPPENING)
4. Agent extracts leads (NOT HAPPENING)
5. Agent creates Lead artifact (NOT HAPPENING)
6. UI displays leads (WORKING, but no data to display)
```

**Solution**: This is automatically solved by MVP-0 + MVP-4

**Dependencies**: MVP-0 and MVP-4 must be completed first

**Estimated**: Included in MVP-0/MVP-4

---

### [ ] Campaign State Transitions
**Priority**: 🔴 HIGH - Core workflow
**Status**: PARTIAL - State machine exists, transitions don't happen

**Critical Gap**: Campaigns stuck in "Initializing" status because state machine never runs.

**What Exists**:
- ✅ CampaignStatus enum (Initializing, Draft, Active, Paused, Completed, Failed)
- ✅ State transition validation rules
- ✅ Campaign entity with status tracking
- ✅ Unit tests for state transitions

**What's Missing**:
- ❌ No code triggers state transitions
- ❌ Campaigns created via UI never move from "Initializing" → "Active"
- ❌ No automatic transition to "Completed" when all tasks done
- ❌ No automatic transition to "Failed" on errors

**Example Expected Flow**:
```
User creates campaign
  ↓
Status: Initializing
  ↓
Background service picks it up (NOT IMPLEMENTED)
  ↓
Status: Active
  ↓
All tasks complete (NEVER HAPPENS)
  ↓
Status: Completed
```

**Solution**: This is automatically solved by MVP-0 (AgentHostedService calls controller which manages state)

**Dependencies**: MVP-0 must be completed first

**Estimated**: Included in MVP-0

---

### [ ] Secure End-User Secrets (Windows & Mac)
**Priority**: 🟡 MEDIUM - Users need secure credential storage
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

### [ ] Rate Limiting for LLM & API Usage
**Priority**: 🟡 MEDIUM - Budget Protection
**Status**: NOT IMPLEMENTED

**Problem**: 
- No rate limiting on LLM chat endpoints
- No rate limiting on Exa web search tool
- Users could drain API budgets through excessive requests
- No throttling for campaign processing

**Budget Risk**:
- OpenAI API costs per request
- Exa web search API costs per query
- No per-user limits
- No global rate caps

**Solution**: Implement ASP.NET Core Rate Limiting middleware

**Implementation Required**:

1. **Configure in Program.cs**:
   ```csharp
   using System.Threading.RateLimiting;
   using Microsoft.AspNetCore.RateLimiting;

   builder.Services.AddRateLimiter(options =>
   {
       // Fixed window: 10 chat messages per minute per user
       options.AddFixedWindowLimiter("chat", opt =>
       {
           opt.Window = TimeSpan.FromMinutes(1);
           opt.PermitLimit = 10;
           opt.QueueLimit = 2;
       });

       // Concurrency: Exa search (max 3 concurrent)
       options.AddConcurrencyLimiter("exa-search", opt =>
       {
           opt.PermitLimit = 3;
           opt.QueueLimit = 10;
       });

       options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
   });

   app.UseRateLimiter();
   ```

2. **Apply to Controllers**:
   ```csharp
   [EnableRateLimiting("chat")]
   public class ChatController : ControllerBase { }
   ```

**Success Criteria**:
- ✅ Chat endpoint limited to 10 requests/min
- ✅ Exa search limited to 3 concurrent requests
- ✅ 429 status with retry-after header
- ✅ All tests passing

**Estimated**: 2-3 hours

---

### [ ] Integrate .NET Aspire for Orchestration
**Priority**: 🟢 LOW-MEDIUM - Simplifies local development
**Status**: NOT IMPLEMENTED

**What is .NET Aspire?**
.NET Aspire is Microsoft's opinionated stack for building observable, cloud-ready distributed apps.

**Benefits for OutreachGenie**:
1. **User Experience**: Single-click start (no manual terminal juggling)
2. **Observability**: Users can see what agent is doing in real-time dashboard
3. **Reliability**: Automatic retry logic for LinkedIn/OpenAI calls

**Implementation**:

```bash
cd server
dotnet new aspire-apphost -n OutreachGenie.AppHost
```

**Testing**:
- Verify all services start via AppHost
- Verify dashboard shows logs and traces

**Estimated**: 4-5 hours

---

### [ ] Problem Details Exception Handler (RFC 7807)
**Priority**: 🟢 LOW - Standardized error responses
**Status**: NOT IMPLEMENTED

**Standard**: [RFC 7807 Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807.html)

**Problem**: 
- No global exception handler in Program.cs
- Unhandled exceptions return stack traces to clients
- No structured error responses

**Solution**: Use ASP.NET Core's built-in Problem Details service

**Implementation**:

```csharp
// Program.cs
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["nodeId"] = Environment.MachineName;
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();
```

**Success Criteria**:
- ✅ Standard RFC 7807 format
- ✅ No stack traces in production
- ✅ Correlation IDs for debugging

**Estimated**: 2-3 hours

---

## 📋 POST-MVP FEATURES (6 Features - Sorted by Priority)

### [ ] Lead Table Rendering in UI
**Priority**: 🔴 HIGH - Critical UX feature
**Status**: NOT IMPLEMENTED

**Purpose**: Display leads in a beautiful, interactive table

**Implementation**:

```tsx
import { DataTable } from "@/components/ui/data-table"

const columns = [
  { header: "Name", accessorKey: "name" },
  { header: "Title", accessorKey: "title" },
  { header: "Company", accessorKey: "company" },
  { header: "LinkedIn", cell: ({ row }) => (
    <a href={row.linkedinUrl}>View Profile</a>
  )}
]

<DataTable columns={columns} data={leads} />
```

**Success Criteria**:
- ✅ Table displays lead data
- ✅ Sorting/filtering works
- ✅ Export to CSV works

---

### [ ] Image Artifacts in Chat with Lead Rendering
**Priority**: 🔴 HIGH - Critical for user experience
**Status**: NOT IMPLEMENTED

**Purpose**: Display lead tables as images in chat UI

**Implementation**:
- Backend: Generate table images using `SixLabors.ImageSharp`
- Frontend: Display as image artifacts in chat
- Include user avatar in chat messages

**Success Criteria**:
- ✅ Leads rendered as beautiful table images
- ✅ User avatar displayed in chat
- ✅ Mobile-responsive

---

### [ ] File Artifacts (Word, Excel, PDF) in Chat
**Priority**: 🟡 MEDIUM - Nice to have
**Status**: NOT IMPLEMENTED (UI exists)

**Purpose**: Display file attachments with nice icons

**Implementation**:
- Chat displays files with appropriate icons (📊 Excel, 📄 Word, 📋 PDF)
- Download and preview functionality

**Success Criteria**:
- ✅ Files display with correct icons
- ✅ Download button works

---

### [ ] File Upload via UI
**Priority**: 🟡 MEDIUM - Enhances chat functionality
**Status**: NOT IMPLEMENTED (UI exists)

**Purpose**: Users can upload files to provide context to agent

**Implementation**:
- Drag & drop interface
- File validation (10MB limit)
- Support CSV, XLSX, TXT, PDF, images

**Success Criteria**:
- ✅ Drag & drop works
- ✅ File preview before upload
- ✅ Progress indicator

---

### [ ] Campaign Analytics Page
**Priority**: 🟢 LOW - Future enhancement
**Status**: NOT IMPLEMENTED (UI exists at /analytics)

**Purpose**: Display campaign performance metrics

**Implementation**:
- Campaign metrics: open rates, reply rates, conversion rates
- Charts: engagement over time
- Export as PDF/Excel

**Success Criteria**:
- ✅ Charts display correctly
- ✅ Metrics calculated accurately
- ✅ Export works

---

### [ ] Cloud Database Sync (Multi-Device)
**Priority**: 🟢 LOW - Future feature
**Status**: NOT IMPLEMENTED

**Purpose**: Sync SQLite data across devices

**Cheapest Options**:

1. **Turso (LibSQL)** - FREE up to 9GB (RECOMMENDED)
   - SQLite-compatible cloud database
   - $0/month
   - Multi-device sync

2. **Supabase** - FREE up to 500MB
   - PostgreSQL cloud database
   - $0/month

3. **OneDrive/Google Drive** - FREE
   - Store SQLite file in synced folder
   - May have corruption issues

**Success Criteria**:
- ✅ Data syncs between devices
- ✅ FREE tier sufficient
- ✅ Easy setup

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
- [X] 142 backend tests passing (96.4% pass rate)
- [X] Integration tests for repositories, controllers, services
- [X] Unit tests for DeterministicController, LeadScoringService
- [X] E2E Playwright tests for full app flows
- [X] GitHub Actions CI workflows (all 5 passing)

---

## 📊 Project Stats

- **Total Tests**: 198 (142 backend + 46 frontend + 10 E2E)
- **Test Coverage**: Backend 96.4%, Frontend 89.4%
- **CI Status**: ✅ All 5 workflows passing
- **Lines of Code**: ~15,000
- **Infrastructure Completion**: 17/17 tasks (100%)
- **Agent Processing**: 0/5 core tasks (0%)
- **Overall MVP Progress**: 17/22 tasks (77%)

---

## 🔍 Critical Understanding

### What's Built (Infrastructure - 100%)
- ✅ Beautiful React UI with real-time updates
- ✅ Complete REST API with 49 passing tests
- ✅ DeterministicController with full state machine logic
- ✅ MCP integration layer (4 servers: Playwright, Desktop Commander, Fetch, Exa)
- ✅ LLM abstraction with OpenAI provider
- ✅ SignalR real-time messaging
- ✅ SQLite database with EF Core
- ✅ Artifact storage with versioning
- ✅ 198 tests with 90%+ coverage

### What's Missing (Agent Brain - 0%)
- ❌ **AgentHostedService** - The loop that makes campaigns process
- ❌ **Chat-to-LLM wiring** - Actual AI conversations
- ❌ **Task execution** - Calling the controller's logic
- ❌ **Artifact generation** - Creating leads/messages from LinkedIn
- ❌ **State transitions** - Moving campaigns through lifecycle

### The Gap in Simple Terms
```
Infrastructure: All pipes connected, valves installed, monitoring ready
Agent: No water flowing because pump not turned on
```

The system has all the pieces (database, API, UI, MCP servers, LLM provider, state machine) but they don't connect because:
1. No background service polls campaigns and calls the controller
2. Chat endpoint returns placeholder instead of calling LLM
3. Everything else cascades from these 2 missing pieces

### Time to Complete MVP
- **MVP-0** (Background Service): 6-9 hours
- **MVP-4** (Chat-to-LLM): 2-3 hours  
- **Testing & Integration**: 2-3 hours
- **Total**: ~10-15 hours to fully working LinkedIn automation