# OutreachGenie Backend Implementation TODO

> **Status**: 10/24 tasks completed
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

### [ ] 11. Integrate Desktop Commander MCP server
**Priority**: High  
**Repository**: https://github.com/wonderwhy-er/DesktopCommanderMCP  
**Setup**:
- Spawn as subprocess with stdio transport
- Register file operations tools
- Configure agent working directory
**Tools**: read_file, write_file, list_directory, execute_command
**Security**: Restrict to agent working directory only

### [ ] 12. Integrate Playwright MCP server
**Priority**: High  
**Setup**:
- Spawn Playwright MCP server subprocess
- Configure headed mode
- Session cookie persistence
**Anti-detection**: Human-like delays, random waits

### [ ] 13. Add Fetch and Exa MCP servers
**Priority**: Medium  
**Fetch**: Web scraping, content extraction  
**Exa**: Web search capabilities  
**Bundle**: Include executables in deployment

### [ ] 14. Create first LinkedIn tool - SearchProspects
**Priority**: Critical (MVP)  
**Tool**: `SearchLinkedInProspects`  
**Input**: query, location, maxResults  
**Output**: List of leads (name, title, headline, profileUrl)  
**Implementation**: Playwright automation via MCP  
**Storage**: Save results as `leads` artifact  
**Scoring**: Apply heuristics, populate WeightScore

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

### [ ] 17. Implement campaign resume/recovery logic
**Priority**: Critical (MVP validation)  
**Test**: 
1. Start campaign → Search LinkedIn → Score leads → Store artifacts
2. Restart app (clear chat history)
3. Resume campaign → Continue from last completed task
**Success Criteria**: No context loss, correct state recovery per specification

---

## Phase 5: API & Real-time Communication (3 tasks)

### [ ] 18. Create API Controllers (NOT Minimal APIs)
**Priority**: Critical  
**Status**: In Progress - CampaignController complete ✅
**Controllers**:
- `CampaignController` - Create, Get, List, Pause, Resume, Delete ✅
- `ChatController` - SendMessage, GetHistory (non-authoritative) - TODO
- `TaskController` - List by campaign, Get status - TODO
- `ArtifactController` - Get by type/key, Create, Update - TODO
- `SettingsController` - Get/Update configuration - TODO
**Route Pattern**: `/api/v1/{controller}/{action}` ✅
**DI**: Repositories registered in Program.cs ✅

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
**Changes**:
- Replace mock responses in ChatPage.tsx with fetch/axios calls
- Add SignalR client (@microsoft/signalr)
- Connect to `/api/v1/chat/send` endpoint
- Subscribe to AgentHub for real-time updates
- Wire campaign management UI to backend
**Environment**: API base URL from .env or config

---

## Phase 7: Testing & Quality (2 tasks)

### [✅] 23. Add code quality and testing infrastructure
**Priority**: High  
**Completed**: January 10, 2026  
**Unit Tests**:
- Ready for DeterministicController state machine tests
- Ready for lead scoring algorithm tests
- Ready for artifact storage tests
**Integration Tests**: ✅
- CampaignRepositoryTests: CRUD operations, related entity loading, cascade deletes
- TaskRepositoryTests: Status filtering, retry logic, JSON I/O, ordering
- ArtifactRepositoryTests: Versioning, type filtering, arbitrary data support
- LeadRepositoryTests: Scoring/ranking, status filtering, batch operations
**Infrastructure**: ✅
- xUnit with IClassFixture pattern
- Testcontainers with PostgreSQL for real database tests
- FluentAssertions for expressive assertions
- Moq for mocking (ready to use)
- Microsoft.AspNetCore.Mvc.Testing for API tests (package installed)
**Documentation**: ✅ docs/TestStrategy.md created
**Build Status**: All tests compile successfully with zero warnings

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
