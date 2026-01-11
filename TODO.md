# OutreachGenie MVP Implementation TODO

> **Status**: Infrastructure 100% Complete, Agent Processing 0% Complete  
> **Last Updated**: January 11, 2026  
> **Critical Gap**: Background agent service not implemented - campaigns don't process  
> **Security Alert**: 🔴 No global exception handler  
> **Architecture Note**: App runs locally on user machines, not cloud-deployed

---

## 🚨 PENDING TASKS (4 Critical)

### [ ] Rate Limiting for LLM & API Usage
**Priority**: 🔴 CRITICAL - Budget Protection
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

1. **Add Rate Limiting Package**:
   ```bash
   # Already included in .NET 7+
   # Microsoft.AspNetCore.RateLimiting
   ```

2. **Configure in Program.cs**:
   ```csharp
   using System.Threading.RateLimiting;
   using Microsoft.AspNetCore.RateLimiting;

   // Add before builder.Build()
   builder.Services.AddRateLimiter(options =>
   {
       // Fixed window: 10 chat messages per minute per user
       options.AddFixedWindowLimiter("chat", opt =>
       {
           opt.Window = TimeSpan.FromMinutes(1);
           opt.PermitLimit = 10;
           opt.QueueLimit = 2;
       });

       // Sliding window: 100 API calls per hour per IP
       options.AddSlidingWindowLimiter("api", opt =>
       {
           opt.Window = TimeSpan.FromHours(1);
           opt.PermitLimit = 100;
           opt.SegmentsPerWindow = 6;
       });

       // Token bucket: Campaign processing (burst support)
       options.AddTokenBucketLimiter("campaigns", opt =>
       {
           opt.TokenLimit = 5;
           opt.ReplenishmentPeriod = TimeSpan.FromMinutes(5);
           opt.TokensPerPeriod = 1;
           opt.AutoReplenishment = true;
       });

       // Concurrency: Exa search (max 3 concurrent)
       options.AddConcurrencyLimiter("exa-search", opt =>
       {
           opt.PermitLimit = 3;
           opt.QueueLimit = 10;
       });

       // Global fallback
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
           context => RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
               factory: _ => new FixedWindowRateLimiterOptions
               {
                   Window = TimeSpan.FromMinutes(1),
                   PermitLimit = 100
               }));

       options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
   });

   // Enable middleware (after app.Build())
   app.UseRateLimiter();
   ```

3. **Apply to Controllers**:
   ```csharp
   // ChatController.cs
   [ApiController]
   [Route("api/v1/[controller]")]
   [EnableRateLimiting("chat")]  // Apply chat rate limit
   public class ChatController : ControllerBase
   {
       [HttpPost]
       [EnableRateLimiting("api")]  // Additional API limit
       public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
       {
           // Will return 429 Too Many Requests if limit exceeded
       }
   }

   // Campaign processing
   [EnableRateLimiting("campaigns")]
   public async Task<IActionResult> StartCampaign(int id) { }
   ```

4. **Custom Response Headers**:
   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.OnRejected = async (context, token) =>
       {
           context.HttpContext.Response.StatusCode = 429;
           
           if (context.Lease.TryGetMetadata(
               MetadataName.RetryAfter, out var retryAfter))
           {
               context.HttpContext.Response.Headers.RetryAfter = 
                   retryAfter.TotalSeconds.ToString();
           }

           await context.HttpContext.Response.WriteAsJsonAsync(
               new { error = "Rate limit exceeded. Please try again later." },
               cancellationToken: token);
       };
   });
   ```

5. **Configuration Settings**:
   ```json
   // appsettings.json
   {
     "RateLimiting": {
       "Chat": {
         "PermitLimit": 10,
         "WindowMinutes": 1
       },
       "ExaSearch": {
         "ConcurrentLimit": 3,
         "QueueLimit": 10
       },
       "Api": {
         "PermitLimit": 100,
         "WindowHours": 1
       }
     }
   }
   ```

**Testing Required**:
- Unit test: Verify rate limiter configuration
- Integration test: Exceed chat limit, verify 429 response
- Integration test: Verify retry-after header
- Load test: Validate limits under concurrent requests
- E2E test: User exceeds limit, receives proper error message

**Success Criteria**:
- ✅ Chat endpoint limited to 10 requests/min
- ✅ Exa search limited to 3 concurrent requests
- ✅ 429 status with retry-after header
- ✅ Global IP-based fallback active
- ✅ Campaign processing throttled
- ✅ All tests passing

**References**:
- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Rate Limiting Middleware](https://devblogs.microsoft.com/dotnet/announcing-rate-limiting-for-dotnet/)

---

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

## 📋 POST-MVP FEATURES (6 Features)

### [ ] Image Artifacts in Chat with Lead Rendering
**Priority**: 🔴 HIGH - Critical for user experience
**Status**: NOT IMPLEMENTED

**Purpose**: Display lead tables as images in chat UI with proper rendering

**What**: 
- Agent generates lead data (CSV format)
- Backend converts to nicely formatted table image
- Frontend displays as image artifact in chat
- User can view leads in beautiful, formatted table UI

**Implementation**:

1. **Backend Image Generation** (OutreachGenie.Api/Services/ImageRenderer.cs):
   ```csharp
   public interface IImageRenderer
   {
       Task<byte[]> RenderLeadsTableAsync(List<Lead> leads);
   }
   
   // Use ImageSharp or System.Drawing
   public class LeadsTableImageRenderer : IImageRenderer
   {
       public async Task<byte[]> RenderLeadsTableAsync(List<Lead> leads)
       {
           // 1. Create image with nice styling
           // 2. Add table headers (Name, Title, Company, etc)
           // 3. Add rows with alternating colors
           // 4. Add icons/badges for engagement status
           // 5. Return PNG bytes
       }
   }
   ```

2. **Chat Message Type**:
   ```csharp
   public class ChatMessage
   {
       public string Content { get; set; }
       public MessageArtifact? Artifact { get; set; }
   }
   
   public class MessageArtifact
   {
       public string Type { get; set; } // "image", "file", "table"
       public string Url { get; set; }   // /api/v1/artifacts/{id}
       public Dictionary<string, object> Metadata { get; set; }
   }
   ```

3. **Frontend ChatMessage Component** (src/components/chat/ChatMessage.tsx):
   ```tsx
   {message.artifact?.type === 'image' && (
     <img 
       src={message.artifact.url} 
       alt="Lead table"
       className="rounded-lg shadow-lg max-w-full"
     />
   )}
   ```

4. **User Avatar**: Display user profile picture alongside messages

**Dependencies**:
- `SixLabors.ImageSharp` (image generation)
- File storage for artifacts (temp directory)

**Testing**:
- Unit test: Image generation with mock lead data
- Integration test: Chat returns image artifact URL
- E2E test: Verify image displays in chat UI

**Success Criteria**:
- ✅ Leads rendered as beautiful table images
- ✅ User avatar displayed in chat
- ✅ Images load and display correctly
- ✅ Mobile-responsive image sizing

---

### [ ] File Artifacts (Word, Excel, PDF) in Chat
**Priority**: 🟡 MEDIUM - Nice to have
**Status**: NOT IMPLEMENTED (UI exists)

**Purpose**: Display file attachments with nice icons and previews

**What**:
- Agent generates files (Excel reports, Word docs, PDFs)
- Chat displays with appropriate icons (📊 Excel, 📄 Word, 📋 PDF)
- User can download or preview inline

**Implementation**:

1. **File Artifact Types**:
   ```csharp
   public enum ArtifactType
   {
       Excel,    // .xlsx
       Word,     // .docx
       Pdf,      // .pdf
       Csv,      // .csv
       Image     // .png, .jpg
   }
   ```

2. **Frontend File Artifact Component**:
   ```tsx
   <FileArtifact
     type="excel"
     fileName="Campaign_Report.xlsx"
     fileSize="245 KB"
     downloadUrl="/api/v1/artifacts/123/download"
     icon={<FileSpreadsheet className="w-6 h-6" />}
   />
   ```

3. **Icon Library**: Use Lucide icons for file types

**Success Criteria**:
- ✅ Files display with correct icons
- ✅ Download button works
- ✅ File size and name shown
- ✅ Preview for images/PDFs

---

### [ ] File Upload via UI
**Priority**: 🟡 MEDIUM - Enhances chat functionality
**Status**: NOT IMPLEMENTED (UI exists)

**Purpose**: Users can upload files to provide context to agent

**What**:
- User drags/drops files into chat
- Agent receives file content in context
- Useful for: uploading lead lists, campaign templates, etc.

**Implementation**:

1. **Upload Endpoint** (ChatController.cs):
   ```csharp
   [HttpPost("upload")]
   public async Task<IActionResult> UploadFile(IFormFile file)
   {
       // 1. Validate file type and size
       // 2. Save to temp storage
       // 3. Return file ID for chat context
   }
   ```

2. **Frontend Upload Component** (src/components/chat/FileUpload.tsx):
   ```tsx
   <div
     onDrop={handleDrop}
     onDragOver={handleDragOver}
     className="border-2 border-dashed rounded-lg p-4"
   >
     <input type="file" onChange={handleFileSelect} />
     Drop files here or click to browse
   </div>
   ```

3. **File Size Limits**:
   - Max 10MB per file
   - Types: CSV, XLSX, TXT, PDF, images

**Success Criteria**:
- ✅ Drag & drop works
- ✅ File preview before upload
- ✅ Progress indicator
- ✅ Agent can access uploaded files

---

### [ ] Campaign Analytics Page
**Priority**: 🟢 LOW - Future enhancement
**Status**: NOT IMPLEMENTED (UI exists at /analytics)

**Purpose**: Display campaign performance metrics and insights

**What**:
- Campaign metrics: open rates, reply rates, conversion rates
- Charts: engagement over time, top-performing messages
- Lead funnel visualization
- Export analytics as PDF/Excel

**Implementation**:

1. **Analytics Controller**:
   ```csharp
   [Route("api/v1/analytics")]
   public class AnalyticsController : ControllerBase
   {
       [HttpGet("campaign/{id}")]
       public async Task<CampaignAnalytics> GetCampaignAnalytics(int id)
       {
           // Query metrics from database
           // Calculate rates and trends
       }
   }
   ```

2. **Frontend Analytics Page** (src/pages/AnalyticsPage.tsx):
   ```tsx
   // Already exists, needs backend data
   <Chart data={campaignMetrics} type="line" />
   <MetricCard title="Open Rate" value="42%" />
   ```

3. **Metrics to Track**:
   - Messages sent/delivered/opened
   - Replies received
   - Meetings scheduled
   - Conversion rate
   - Time-to-response

**Success Criteria**:
- ✅ Charts display correctly
- ✅ Metrics calculated accurately
- ✅ Export to Excel/PDF works
- ✅ Filters by date range

---

### [ ] Cloud Database Sync (Multi-Device)
**Priority**: 🟢 LOW - Future feature
**Status**: NOT IMPLEMENTED

**Purpose**: Sync SQLite data across devices via cloud storage

**Problem**: 
- Currently: Each device has separate SQLite database
- User wants: Access campaigns from laptop + desktop

**Cheapest Options**:

1. **OneDrive/Google Drive Sync** (FREE):
   - Store SQLite file in synced folder (`~/OneDrive/OutreachGenie/`)
   - OS handles sync automatically
   - **Cost**: $0 (uses existing cloud storage)
   - **Limitation**: Concurrent writes may corrupt database

2. **Supabase Free Tier** (FREE up to 500MB):
   - PostgreSQL cloud database
   - Replace SQLite with Supabase
   - **Cost**: $0/month (up to 500MB, 2GB bandwidth)
   - **Pros**: Multi-device, real-time sync, no corruption

3. **Turso (LibSQL)** (FREE up to 9GB):
   - SQLite-compatible cloud database
   - Minimal code changes (keep Entity Framework)
   - **Cost**: $0/month (up to 9GB, 1B row reads)
   - **Pros**: SQLite syntax, multi-device, no migration pain

4. **PocketBase** (Self-hosted, FREE):
   - User hosts on their own server/Raspberry Pi
   - SQLite-based with sync
   - **Cost**: $0 (user provides hardware)

**Recommended**: Turso (FREE + SQLite-compatible)

**Implementation**:

1. **Add Turso NuGet Package**:
   ```bash
   dotnet add package Turso.EntityFrameworkCore
   ```

2. **Update Program.cs**:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(options =>
   {
       var tursoUrl = builder.Configuration["Turso:DatabaseUrl"];
       var tursoToken = builder.Configuration["Turso:AuthToken"];
       options.UseTurso(tursoUrl, tursoToken);
   });
   ```

3. **User Configuration**: Settings page to enter Turso credentials

**Success Criteria**:
- ✅ Data syncs between devices
- ✅ No data loss on concurrent edits
- ✅ FREE tier sufficient for MVP
- ✅ Easy setup for users

---

### [ ] Lead Table Rendering in UI
**Priority**: 🔴 HIGH - Critical UX feature
**Status**: NOT IMPLEMENTED

**Purpose**: Display leads in a beautiful, interactive table

**What**: 
- Agent finds leads → displays in sortable/filterable table
- Actions: Click to open LinkedIn, mark as contacted, etc.
- Export to CSV/Excel

**Implementation**:

1. **Backend**: Returns lead data as structured JSON

2. **Frontend LeadsTable Component** (src/components/LeadsTable.tsx):
   ```tsx
   import { DataTable } from "@/components/ui/data-table"
   
   const columns = [
     { header: "Name", accessorKey: "name" },
     { header: "Title", accessorKey: "title" },
     { header: "Company", accessorKey: "company" },
     { header: "LinkedIn", cell: ({ row }) => (
       <a href={row.linkedinUrl}>View Profile</a>
     )},
     { header: "Actions", cell: ({ row }) => (
       <Button onClick={() => markContacted(row.id)}>
         Mark Contacted
       </Button>
     )}
   ]
   
   <DataTable columns={columns} data={leads} />
   ```

3. **Features**:
   - Sorting by any column
   - Filtering by company/title
   - Pagination (50 leads per page)
   - Export to CSV
   - Bulk actions (select multiple)

4. **Styling**: Use shadcn/ui Table component with custom styling

**Dependencies**:
- `@tanstack/react-table` (already available)
- shadcn/ui `table` component

**Success Criteria**:
- ✅ Table displays lead data
- ✅ Sorting/filtering works
- ✅ Mobile-responsive
- ✅ Export to CSV works

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
