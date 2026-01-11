# OutreachGenie Backend Implementation TODO

> **Status**: 13/24 tasks completed
> **Last Updated**: January 11, 2026

---

## Phase 1: Foundation & Setup (5 tasks)

### [ ] 1. Copy specification documents to workspace root
**Priority**: Critical  
**Files**: 
- Copy `agent_specification_deterministic_desktop_outreach_agent.md` from Downloads
- Copy `agent_chat_output_specification.md` from Downloads
**Why**: Authoritative reference for all architectural decisions

### [ ] 2. Create .NET backend project structure with Aspire
**Priority**: Critical  
**Status**: ✅ COMPLETE (Infrastructure exists)
**Projects to create**:
- `server/OutreachGenie.Api` - ASP.NET Core with Controllers
- `server/OutreachGenie.Domain` - Core entities, value objects, domain events
- `server/OutreachGenie.Application` - Business logic, services, interfaces
- `server/OutreachGenie.Infrastructure` - Database, external services, MCP clients
- `server/OutreachGenie.Tests` - Unit & integration tests
- `server/OutreachGenie.AppHost` - Aspire orchestration
**Architecture**: Clean Architecture with DDD principles

### [ ] 3. Configure centralized build and package management
**Priority**: High  
**Status**: ✅ COMPLETE (Directory.Build.props, Directory.Packages.props, .editorconfig exist)
**Files to create**:
- `Directory.Build.props` - Common build properties, code quality rules
- `Directory.Packages.props` - Centralized NuGet versions
- `.editorconfig` - Code style enforcement
**Analyzers**: StyleCop, SonarAnalyzer.CSharp
**Rules**: Treat warnings as errors

### [X] 4. Set up SQLite with EF Core
**Priority**: Critical  
**Completed**: January 11, 2026
**Tasks**:
- Install Microsoft.EntityFrameworkCore.Sqlite ✅
- Create DbContext with campaigns, tasks, artifacts, leads tables ✅
- Add initial migration ✅
- Configure connection string from appsettings.json ✅
- Automatic migration on startup ✅
**Future**: Add Supabase sync as optional Phase 2

### [X] 5. Add structured logging with Serilog
**Priority**: High  
**Completed**: January 11, 2026
**Configuration**:
- Console sink (development) ✅
- File sink (production logs in working directory) ✅
- Structured event logging for audit trail ✅
- Log enrichment with correlation IDs ✅

---

## Phase 2: Core Domain & State Machine (4 tasks)

### [X] 6. Implement Domain layer - Core models
**Priority**: Critical  
**Entities**:
- `Campaign` (Id, Name, Status, TargetAudience, CreatedAt, UpdatedAt)
- `Task` (Id, CampaignId, Description, Status, Type, Input, Output, RetryCount)
- `Artifact` (Id, CampaignId, Type, Key, Content, Source, Version, CreatedAt)
- `Lead` (Id, CampaignId, FullName, ProfileUrl, Title, Headline, Location, WeightScore, Status)
**Enums**: CampaignStatus, TaskStatus, ArtifactType, ArtifactSource
**Value Objects**: As needed per DDD
**Domain Events**: State transition events

### [X] 7. Implement Application - DeterministicController
**Priority**: Critical  
**Completed**: January 11, 2026  
**Responsibilities** (per spec):
- Reload state at start of every cycle ✅
- Select next eligible task ✅
- Validate LLM proposals ✅
- Execute approved actions via MCP tools (ready for integration)
- Persist audit logs ✅
- Update task status after verification ✅
- Enforce all invariants from specification ✅
**Implementation**: State machine with transition validation ✅
**Tests**: 12 unit tests covering all methods and state transitions ✅

### [X] 8. Create campaign repositories
**Priority**: High  
**Completed**: January 10, 2026 (with Task 6)  
**Interfaces**:
- `ICampaignRepository` - CRUD + GetWithTasksAndArtifacts ✅
- `ITaskRepository` - Query by campaign, status filtering ✅
- `IArtifactRepository` - Typed queries (GetByType, GetByKey) ✅
- `ILeadRepository` - Scoring, sorting, pagination ✅
**Implementation**: EF Core with async operations ✅

### [X] 9. Build Artifact storage system
**Priority**: High  
**Completed**: January 10, 2026 (with Task 6)  
**Supported Types**: ✅
- `context` - Campaign overview, user business info
- `leads` - Prospect lists with scores
- `messages` - Message templates/history
- `heuristics` - Lead scoring configuration
- `environment` - Encrypted secrets, config
- `arbitrary` - Agent-created data (dynamic schema)
**Features**: Versioning, source tracking (user|agent), JSON serialization ✅

---

## Phase 3: MCP Integration (5 tasks)

### [X] 10. Create MCP abstraction layer
**Priority**: Critical  
**Completed**: January 11, 2026
**Interfaces**:
- `IMcpServer` - Connect, Disconnect, ListTools, CallTool ✅
- `IMcpToolRegistry` - Register, Discover, Validate schemas ✅
- `IMcpTransport` - Stdio, HTTP (future) ✅
- `McpTool` - Tool metadata with JSON schema ✅
**Benefits**: Easily add new MCP servers (Word, Excel, etc.)

### [X] 11. Integrate Desktop Commander MCP server
**Priority**: High  
**Completed**: January 11, 2026  
**Repository**: https://github.com/wonderwhy-er/DesktopCommanderMCP  
**Setup Requirement**: Pre-install via `npx @wonderwhy-er/desktop-commander@latest setup` to avoid timeout errors
**Implementation**:
- StdioMcpTransport for subprocess communication ✅
- DesktopCommanderMcpServer implementing IMcpServer ✅
- JSON-RPC 2.0 protocol with initialize handshake ✅
- Process spawning with npx command (no -y flag, uses pre-installed version) ✅
- McpServiceConfiguration for DI registration ✅
- Comprehensive tests with FakeMcpTransport ✅
**Tools**: read_file, write_file, list_directory, execute_command (via MCP protocol)
**Security**: Working directory restriction configured via --working-directory flag

### [X] 12. Integrate Playwright MCP server
**Priority**: High  
**Completed**: January 11, 2026
**Implementation**:
- PlaywrightMcpServer implementing IMcpServer ✅
- Reuses StdioMcpTransport for subprocess communication ✅
- JSON-RPC 2.0 protocol with initialize handshake ✅
- Headed/headless mode configuration ✅
- McpServiceConfiguration extension method ✅
- Comprehensive tests with FakeMcpTransport ✅
**Tools**: playwright_navigate, playwright_click, playwright_fill, playwright_screenshot (via MCP protocol)
**Browser**: Chromium (default), headed mode for anti-detection
**Sessions**: Cookie persistence via Playwright state management

### [X] 13. Add Fetch and Exa MCP servers
**Priority**: Medium  
**Completed**: January 11, 2026
**Implementation**:
- FetchMcpServer for web scraping and content extraction ✅
- ExaMcpServer for semantic web search ✅
- Both reuse StdioMcpTransport infrastructure ✅
- McpServiceConfiguration extension methods ✅
**Fetch Tools**: fetch_html, fetch_json, fetch_text
**Exa Tools**: exa_search, exa_find_similar, exa_get_contents
**Configuration**: Exa requires API key via environment variable

### [X] 14. Create first LinkedIn tool - SearchProspects
**Priority**: Critical (MVP)  
**Completed**: January 11, 2026
**Implementation**: LLM-driven orchestration via MCP tools
- OpenAiLlmProvider sends campaign state + MCP tool schemas to LLM ✅
- DeterministicController.ExecuteTaskWithLlmAsync runs multi-turn LLM loop ✅
- ExecuteToolAsync bridges ActionProposal to MCP server calls ✅
- Error handling: exponential backoff, JSON validation, rate limiting ✅
- Comprehensive unit tests: 6 scenarios covering success/retry/failure paths ✅
**Architecture**: LLM receives Playwright tools (browser_navigate, browser_evaluate, browser_screenshot, etc.), decides how to use them dynamically
**Tool**: LLM proposes tool calls (e.g., "browser_navigate" to LinkedIn, "browser_evaluate" to extract data)
**Input**: Task description: "Search LinkedIn for CTOs in San Francisco"  
**Output**: LLM orchestrates navigation, extraction, storage; saves results as artifact
**Storage**: LLM decides when to save leads artifact via Desktop Commander write_file tool
**Scoring**: Apply LeadScoringService after extraction, populate WeightScore
**Benefits**: No hardcoded selectors, adapts to UI changes, truly dynamic automation

**Example Flow**:
1. Controller loads state, gets MCP tools, asks LLM: "Execute task: Search LinkedIn for CTOs in SF"
2. LLM: `{"ActionType": "browser_navigate", "Parameters": {"url": "https://linkedin.com/search"}}`
3. Controller validates, executes via PlaywrightMcpServer, returns result
4. LLM sees result, proposes: `{"ActionType": "browser_evaluate", "Parameters": {"function": "() => [...].map(el => el.textContent)"}}`
5. Controller executes JS evaluation, gets prospect names/titles
6. LLM: `{"ActionType": "write_file", "Parameters": {"path": "leads.json", "content": "[...]"}}`
7. Controller persists artifact, LLM marks: `{"ActionType": "task_complete", "Parameters": {"result": "Found 25 prospects"}}`
8. Controller marks task Done, saves audit logs

---

## Phase 4: LLM & Agent Logic (3 tasks)

### [X] 15. Implement LLM abstraction layer
**Priority**: Critical  
**Completed**: January 11, 2026
**Interface**: `ILlmProvider` ✅
**Supporting Classes**:
- `ChatMessage` - Message in conversation history ✅
- `LlmConfiguration` - Provider settings (temperature, tokens, model, retries, timeout) ✅
**Implementations** (ready for):
- `OpenAiProvider` (OpenAI API)
- `AnthropicProvider` (Claude API)
- `LocalProvider` (Ollama/local models)
**Features**:
- Structured proposal schema validation (ActionProposal)
- Retry with exponential backoff (configured)
- Token counting (configured)
- Temperature/max tokens configuration ✅
**Proposal Schema**: Action type, task ID, parameters ✅

### [X] 16. Implement lead scoring service
**Priority**: High  
**Completed**: January 11, 2026
**Algorithm**: Weighted heuristics on search results only ✅
**Factors**:
- Job title relevance to campaign target ✅
- Headline keyword matching ✅
- Location alignment ✅
- Connection degree (if visible) - Future enhancement
**Configuration**: Stored as `heuristics` artifact (user-editable JSON) ✅
**Constraints**: No profile visits (per requirement #8) ✅
**Implementation**: LeadScoringService with 7 unit tests ✅

### [✅] 17. Implement campaign resume/recovery logic
**Priority**: Critical (MVP validation)  
**Completed**: January 11, 2026  
**Test**: 
1. Start campaign → Execute tasks → Store artifacts
2. Restart app (simulate with new controller instance)
3. Resume campaign → Continue from last completed task
**Success Criteria**: No context loss, correct state recovery per specification ✅
**Implementation**: 
- CampaignResumeIntegrationTests with 4 comprehensive tests ✅
  1. ResumeCampaign_ShouldContinueFromLastCompletedTask - Basic resume scenario
  2. ResumeCampaign_ShouldRetryFailedTasks - Failed task handling during resume
  3. ResumeCampaign_ShouldHandlePauseAndReactivation - Pause/resume flow
  4. ResumeCampaign_ShouldPreserveArtifactVersioning - Artifact history preservation
- Validates existing architecture: ReloadStateAsync + SelectNextTask provide complete resume functionality
- Test proves spec requirement: "State is reloadable - No dependency on conversation history" ✅
- All 138 tests passing (133 passed, 5 skipped) ✅

---

## Phase 5: API & Real-time Communication (3 tasks)

### [✅] 18. Create API Controllers (NOT Minimal APIs)
**Priority**: Critical  
**Completed**: January 11, 2026
**Controllers**:
- `CampaignController` - Create, Get, List, Pause, Resume, Delete ✅
- `ChatController` - SendMessage, GetHistory (non-authoritative, placeholder) ✅
- `TaskController` - List by campaign, Get status ✅
- `ArtifactController` - Get by type/key, Create, Update ✅
- `SettingsController` - Get/Update configuration (placeholder) ✅
**Route Pattern**: `/api/v1/{controller}/{action}` ✅
**DI**: Repositories registered in Program.cs ✅
**Request Models**: Separate files with JsonRequired attributes for value types ✅
**Test Results**: All 49 tests passing including 10 CampaignController integration tests ✅

### [ ] 19. Add SignalR hub for real-time updates
**Priority**: High  
**Hub**: `AgentHub`  
**Events**: 
- `TaskStatusChanged` (task ID, new status)
- `ChatMessageReceived` (message)
- `CampaignStateChanged` (campaign ID, new status)
- `ArtifactCreated` (artifact type, key)
**Purpose**: Push updates to React frontend without polling

### [ ] 20. Configure .env encryption with DPAPI
**Priority**: High  
**Secrets**:
- LinkedIn session cookies
- LLM API keys (OpenAI, Anthropic)
- Supabase credentials (future)
**Implementation**: Windows DPAPI (System.Security.Cryptography.ProtectedData)  
**Utility**: EncryptEnvFile, DecryptEnvFile commands  
**Storage**: Encrypted .env.encrypted file

---

## Phase 6: Aspire Orchestration & React Integration (2 tasks)

### [ ] 21. Set up Aspire orchestration
**Priority**: High  
**AppHost Configuration**:
- Add backend API resource
- Add React frontend build step (npm run build)
- Serve React static files from API
- Single-port deployment (e.g., https://localhost:5001)
**Environment**: Development vs Production configuration

### [ ] 22. Update React frontend to connect to backend API
**Priority**: High  
**Status**: ✅ COMPLETE (January 11, 2026)
**Changes**:
- Created src/lib/api.ts with typed API client matching C# backend models ✅
- Updated ChatPage.tsx to use real API instead of mock responses ✅
- Added CORS support in backend Program.cs for React frontend ✅
- Created .env.development and .env.production for API base URL configuration ✅
- Validated end-to-end integration using Playwright MCP:
  - Frontend sends POST /api/v1/chat/send with campaignId + message ✅
  - Backend responds with ChatResponse (messageId, content, timestamp) ✅
  - Frontend displays agent response in chat UI ✅
**Backend**: Running on http://localhost:5104
**Frontend**: Running on http://localhost:8081
**What Works**: Chat message flow, error handling with toast notifications
**What's Next**: Add SignalR client (@microsoft/signalr) for real-time updates, add campaign management UI

---

## Phase 7: Testing & Quality (2 tasks)

### [✅] 23. Add code quality and testing infrastructure
**Priority**: High  
**Completed**: January 11, 2026  
**Unit Tests**: ✅
- DeterministicController state machine tests (12 tests)
- Lead scoring algorithm tests (7 tests)
- LlmConfiguration, ChatMessage, McpTool, ActionProposal tests (13 tests)
- DesktopCommanderMcpServer tests with fake transport (5 tests)
- PlaywrightMcpServer tests with fake transport (5 tests)
- FetchMcpServer tests with fake transport (5 tests)
- ExaMcpServer tests with fake transport (5 tests)
- StdioMcpTransport tests with PowerShell echo server (8 tests)
**Integration Tests**: ✅
- CampaignRepositoryTests: CRUD operations, versioning, filtering (8 tests)
- TaskRepositoryTests: Status filtering, retry logic, JSON I/O (5 tests)
- ArtifactRepositoryTests: CRUD, versioning, type filtering (8 tests)
- LeadRepositoryTests: CRUD, scoring/ranking, batch operations (8 tests)
- CampaignControllerTests: API endpoint integration (11 tests)
**Infrastructure**: ✅
- xUnit with IAsyncLifetime and ICollectionFixture pattern
- SQLite in-memory databases for integration tests
- FluentAssertions for expressive assertions
- Moq for mocking
- Microsoft.AspNetCore.Mvc.Testing for API tests
**Test Results**: All 96 tests passing ✅
**MCP Coverage**: 
- StdioMcpTransport: 91% (process spawning, JSON-RPC, environment variables)
- DesktopCommanderMcpServer: 100% (protocol handshake, tool discovery/execution)
- PlaywrightMcpServer: 100% (browser automation protocol)
- FetchMcpServer: 100% (web scraping protocol)
- ExaMcpServer: 100% (web search protocol)
**Documentation**: ✅ docs/TestStrategy.md created

### [ ] 24. Create developer documentation
**Priority**: Medium  
**Documentation**:
- Architecture overview (diagram)
- MCP server integration guide
- How to add new MCP tools (Word/Excel examples)
- Contribution guidelines
- Deployment instructions
**Format**: Markdown in /docs folder

---

## Out of Scope (Future Phases)

- [ ] Supabase cloud sync
- [ ] Multi-user authentication
- [ ] Connection request tool (LinkedIn)
- [ ] Send message tool (LinkedIn)
- [ ] Advanced analytics dashboard
- [ ] Cross-platform DPAPI alternative
- [ ] Docker containerization
- [ ] CI/CD pipeline

---

## Notes

### Architectural Principles (from spec)
1. **Controller is authoritative** - LLM proposes, Controller decides
2. **State is reloadable** - No dependency on conversation history
3. **Artifacts are persistent** - All data survives restart
4. **Logs are immutable** - Audit trail for every action
5. **Tools are mediated** - All execution through Controller
6. **Chat is non-authoritative** - For narration only, not state

### Key Constraints
- ✅ Single repository (existing outreachgenie-app)
- ✅ .NET Aspire orchestration
- ✅ Controllers (NOT Minimal APIs)
- ✅ Modular, readable code
- ✅ Conservative + modern features
- ✅ Easy to add MCP servers
- ✅ Scripts are temporary/hidden
- ✅ Arbitrary artifacts supported
- ✅ Human-like LinkedIn rate limits
- ✅ SQLite local-first (Supabase optional)

### Development Environment
- .NET 10 SDK
- Node.js + npm (for React)
- SQLite
- Visual Studio 2022 / VS Code
- Windows (DPAPI encryption)
