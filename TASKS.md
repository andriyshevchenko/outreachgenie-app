# OutreachGenie Implementation Tasks

## References
- **specs.md** - Production E2E specification with data models, execution rules, and system contracts
- **user-stories.md** - User stories organized by EPIC covering campaign lifecycle, task management, logging, MCP integration, and LLM usage
- **backend-code-rules.md** - Elegant Objects principles and C# coding standards that ALL code must follow

## Infrastructure Setup

- [x] **Task 1: Complete backend project configuration** ✅
  - **Completed**: All 4 backend projects created (Api, Core, Infrastructure, Application), NuGet packages configured, solution builds successfully
  - Location: `backend/OutreachGenie.{Api,Core,Application,Infrastructure}`

- [ ] **Task 2: Create Core domain entities**
  - **References**: specs.md sections 3-5 (Task model, Campaign data model, Logging model), user-stories.md US-1.1, US-3.1, US-4.1
  - **Location**: `backend/OutreachGenie.Core/Entities/`
  - **Requirements**:
    - All entities MUST be `sealed` classes implementing interfaces (backend-code-rules.md C2, C8)
    - All entities MUST be immutable (backend-code-rules.md C12-H)
    - Entity names: adjective + noun only (backend-code-rules.md C4-C)
    - Campaign: id (UUID), name, user_goal, status, created_at per specs.md section 4.1
    - CampaignTask: id, campaign_id, title, description, status, task_type, created_by, created_at, completed_at per specs.md section 3.1
    - Lead: id, campaign_id, full_name, linkedin_url, attributes (JSONB), score, status, created_at per specs.md section 4.2
    - AuditLog: id, campaign_id, action, metadata (JSONB), created_at per specs.md section 5.1
    - EventLog: id, campaign_id, entity_type, entity_id, operation, before_state (JSONB), after_state (JSONB), created_at per specs.md section 5.2
    - ChatMessage: id, campaign_id, role, content, created_at per conversation model specs.md section 12
  - **Acceptance**: Each entity has corresponding interface (ICampaign, ICampaignTask, etc.), all fields encapsulated, constructors only contain assignments (C9-H), no public setters (C11-H)

- [ ] **Task 3: Define domain enums and value objects**
  - **References**: specs.md section 3.2 (Task types), backend-code-rules.md C16-M (no -er/-ir suffixes)
  - **Location**: `backend/OutreachGenie.Core/Enums/` and `backend/OutreachGenie.Core/ValueObjects/`
  - **Requirements**:
    - TaskType enum: Ephemeral, Enforced (specs.md section 3.2 defines behavior)
    - TaskStatus enum: Pending, InProgress, Completed, Cancelled per specs.md section 3.1
    - CampaignStatus enum: Active, Paused per user-stories.md US-1.1, US-1.3
    - CreatedBy enum: Llm, System, User per specs.md section 3.1
    - Value objects for CampaignId, TaskId, LeadId (UUID wrappers) following Elegant Objects principles
  - **Acceptance**: All enums have XML documentation explaining when each value is used, value objects are immutable and sealed

- [ ] **Task 4: Create repository interfaces**
  - **References**: specs.md sections 3-5 (data models), user-stories.md US-7.1 (lead storage), backend-code-rules.md C2-C (interface naming)
  - **Location**: `backend/OutreachGenie.Core/Interfaces/Repositories/`
  - **Requirements**:
    - ICampaign interface with methods: Task<ICampaign> Create(), Task<ICampaign?> GetById(Guid id), Task<IReadOnlyList<ICampaign>> GetAll(), Task Update()
    - ITask interface with methods: Task<ITask> Create(), Task<ITask?> GetById(Guid id), Task<IReadOnlyList<ITask>> GetByCampaignId(Guid campaignId), Task<ITask?> GetNextEnforcedTask(Guid campaignId), Task UpdateStatus()
    - ILead interface with methods: Task<ILead> Create(), Task<IReadOnlyList<ILead>> GetByCampaignId(Guid campaignId), Task UpdateScore()
    - IAuditLog interface with method: Task<IAuditLog> Create() (write-only per specs.md section 5.1)
    - IEventLog interface with method: Task<IEventLog> Create() (write-only per specs.md section 5.2)
    - IChatMessage interface with methods: Task<IChatMessage> Create(), Task<IReadOnlyList<IChatMessage>> GetRecent(Guid? campaignId, int limit)
  - **Notes**: Interface names must be nouns (C3-C), method names follow CQRS (M8-M): commands are verbs, queries are nouns
  - **Acceptance**: All repository interfaces defined, no implementation details leak into interfaces, all return domain entities not DTOs

- [ ] **Task 5: Implement DbContext and EF configurations**
  - **References**: specs.md sections 3-5 (table schemas), user-stories.md US-4.2 (event log DB changes)
  - **Location**: `backend/OutreachGenie.Infrastructure/Data/`
  - **Requirements**:
    - Create `OutreachGenieDbContext : DbContext` in Infrastructure layer
    - Configure DbContext with snake_case naming using `EFCore.NamingConventions` package
    - Add DbSet<T> for: Campaign, CampaignTask, Lead, AuditLog, EventLog, ChatMessage
    - Entity configurations using `IEntityTypeConfiguration<T>` for each entity (separate files in `Data/Configurations/`)
    - Map specs.md table schemas exactly: campaigns, tasks (note: not campaign_tasks), leads, audit_log, event_log, chat_messages
    - Configure JSONB columns for: Lead.attributes, AuditLog.metadata, EventLog.before_state, EventLog.after_state
    - Add shadow properties for auditing: CreatedAt, UpdatedAt on all entities except audit/event logs
    - Configure indexes: campaigns(status), tasks(campaign_id, status, task_type), leads(campaign_id), audit_log(campaign_id, created_at), event_log(campaign_id, entity_type, created_at)
  - **Acceptance**: DbContext builds successfully, all entity configurations are separate files, snake_case naming verified, JSONB columns configured for PostgreSQL

- [ ] **Task 6: Implement repository classes**
  - **References**: specs.md section 8.2 (task execution flow with transactions), user-stories.md US-7.1
  - **Location**: `backend/OutreachGenie.Infrastructure/Repositories/`
  - **Requirements**:
    - Implement concrete repositories: ActiveCampaignRepository, ActiveTaskRepository, ActiveLeadRepository, ActiveAuditLogRepository, ActiveEventLogRepository, ActiveChatMessageRepository
    - Class naming: adjective + noun (C4-C), must be sealed (C8-C), must implement interface from Core
    - Each repository encapsulates DbContext and ILogger<T> (C13-H)
    - All methods must use async/await patterns
    - GetNextEnforcedTask query: SELECT WHERE status='pending' AND task_type='enforced' ORDER BY created_at LIMIT 1 (specs.md section 8.1)
    - All CUD operations must be wrapped in transactions when called from application layer
    - No reflection or type checks allowed (M3-C, M4-C)
  - **Acceptance**: All repository interfaces implemented, repositories follow Elegant Objects principles, GetNextEnforcedTask returns correct task, all async operations use CancellationToken

- [ ] **Task 7: Create event logging interceptor**
  - **References**: specs.md section 5.2 (event log reconstruction), user-stories.md US-4.2 (event log DB changes)
  - **Location**: `backend/OutreachGenie.Infrastructure/Interceptors/EventLoggingInterceptor.cs`
  - **Requirements**:
    - Create `sealed class EventLoggingInterceptor : SaveChangesInterceptor`
    - Override `SavingChangesAsync` to capture entity changes BEFORE SaveChanges
    - For each modified entity (Added, Modified, Deleted), create EventLog entry with:
      - entity_type: entity's CLR type name
      - entity_id: primary key value
      - operation: "insert" | "update" | "delete"
      - before_state: serialized original values (null for insert)
      - after_state: serialized current values (null for delete)
    - EventLog entries must be persisted in SAME transaction as entity changes (specs.md section 5.2)
    - Exclude EventLog and AuditLog entities from event logging (prevent infinite loop)
  - **Acceptance**: Interceptor captures all entity changes, before/after state serialized as JSON, EventLog entries written atomically with changes, interceptor registered in DbContext options

## MCP Integration

- [ ] **Task 8: Create MCP service interfaces**
  - **References**: specs.md section 2 (MCP topology), user-stories.md US-4.1 (audit every action), US-6.2 (LinkedIn automation)
  - **Location**: `backend/OutreachGenie.Core/Interfaces/Services/Mcp/`
  - **Requirements**:
    - ISqlOperation interface: methods for query execution, transaction management
    - IFilesystemOperation interface: methods for read/write operations to AGENT_WORKING_DIR per specs.md section 6
    - IBrowserOperation interface: methods for LinkedIn navigation, element interaction, data extraction (specs.md section 10)
    - ICommandOperation interface: methods for Docker, CLI execution (specs.md section 5.2 environment validation)
    - All methods return Task<TResult> or Task for async operations
    - All methods must include parameters for audit context (campaignId, action description)
    - Interface names must be nouns (backend-code-rules.md C3-C)
  - **Acceptance**: Four MCP service interfaces defined with clear method signatures, no implementation details in interfaces, audit context included in all operations

- [ ] **Task 9: Implement audit logging decorator**
  - **References**: specs.md section 5.1 (audit log structure), user-stories.md US-4.1, US-10.2
  - **Location**: `backend/OutreachGenie.Infrastructure/Decorators/AuditedMcpServiceDecorator.cs`
  - **Requirements**:
    - Create decorator pattern wrapping each MCP service interface
    - For EVERY MCP method call, write to audit_log table BEFORE and AFTER execution:
      - Before: log action name, input parameters (as JSONB metadata)
      - After: log result, execution time, success/failure status
    - Use Scrutor for automatic decorator registration in DI
    - Decorator must be sealed class implementing same interface as wrapped service (backend-code-rules.md C8-C)
    - Decorator constructor: inject IAuditLogRepository and actual service instance (C9-H: only assignments)
    - Audit entries must include: id (UUID), campaign_id, action (method name), metadata (parameters + result), created_at
  - **Acceptance**: All MCP calls automatically audited, decorator properly wraps services, audit entries visible in database, no manual audit calls needed in application code

- [ ] **Task 10: Implement MCP service wrappers**
  - **References**: specs.md section 2 (MCP via stdio/HTTP), user-stories.md US-6.2, US-6.3 (LinkedIn automation)
  - **Location**: `backend/OutreachGenie.Infrastructure/Services/Mcp/`
  - **Requirements**:
    - PostgreSqlOperation class: connects to PostgreSQL via Npgsql, executes queries, manages transactions
    - LocalFilesystemOperation class: read/write to AGENT_WORKING_DIR/{campaign_id}/ for artifacts (specs.md section 6)
    - PlaywrightBrowserOperation class: launches Playwright, navigates LinkedIn, extracts profiles (specs.md section 10)
    - DockerCommandOperation class: executes Docker commands for PostgreSQL provisioning (user-stories.md US-5.2)
    - All classes must be sealed and implement respective interfaces (C8-C, C2-C)
    - Configuration injected via IOptions<McpSettings> with endpoints/paths from appsettings
    - Error handling: wrap external calls in try-catch, log failures, throw domain exceptions
    - MCP server communication: use HTTP client for remote MCP servers, Process for local stdio
  - **Acceptance**: All four MCP services implemented, can connect to PostgreSQL, write files to campaign directories, launch Playwright sessions, execute Docker commands

## Task Engine

- [ ] **Task 11: Create task engine interfaces**
  - **References**: specs.md section 8 (task engine execution rules), user-stories.md US-3.3 (strict task execution)
  - **Location**: `backend/OutreachGenie.Core/Interfaces/Engine/`
  - **Requirements**:
    - IEngine interface with method: Task<IExecutionResult> ExecuteNext(Guid campaignId)
      - Rule: Execute exactly ONE enforced task at a time (specs.md section 8.1)
      - Returns execution result with status, output, any errors
    - IExecutor interface with method: Task<IExecutionResult> Execute(ITask task)
      - Must be implemented by specific executors for different task types
      - Receives task, performs operation, returns result
    - IValidation interface with method: Task<IValidationResult> Validate(ITask task)
      - Checks if task can be executed (dependencies, prerequisites)
    - IExecutionResult interface: properties for Success (bool), Output (string), Error (string?), CompletedAt (DateTime)
    - IValidationResult interface: properties for IsValid (bool), Errors (IReadOnlyList<string>)
  - **Acceptance**: All engine interfaces defined, execution model clear, one-task-at-a-time rule enforced at interface level

- [ ] **Task 12: Implement task executors**
  - **References**: specs.md sections 10-11 (LinkedIn flow, scoring flow), user-stories.md US-6.2, US-6.3, US-8.2
  - **Location**: `backend/OutreachGenie.Application/Executors/`
  - **Requirements**:
    - LinkedInAuthenticationExecutor: authenticate LinkedIn via browser (user-stories.md US-6.1)
    - LinkedInSearchExecutor: search LinkedIn with criteria, save results (specs.md section 10, user-stories.md US-6.2)
    - LeadExtractionExecutor: extract profile data from LinkedIn URLs (user-stories.md US-6.3)
    - LeadScoringExecutor: apply scoring algorithm to leads (specs.md section 11, user-stories.md US-8.2)
    - DatabasePersistenceExecutor: persist leads to database (user-stories.md US-7.1)
    - FileArtifactExecutor: export leads as Excel/CSV (user-stories.md US-7.2)
    - MessageDraftingExecutor: generate outreach messages via LLM (user-stories.md US-9.1)
    - Each executor: sealed class implementing IExecutor, encapsulates MCP services (C13-H)
    - All executors registered in DI with Scrutor assembly scanning
    - Executors must write audit logs via MCP decorator (automatic)
  - **Acceptance**: All seven executors implemented, each handles specific task type, executors can be selected by task.type property, MCP operations automatically audited

- [ ] **Task 13: Implement core task engine**
  - **References**: specs.md section 8.2 (execution flow with transactions), user-stories.md US-3.3, US-11.1 (safe restart)
  - **Location**: `backend/OutreachGenie.Application/Engine/SequentialTaskEngine.cs`
  - **Requirements**:
    - Sealed class implementing IEngine
    - ExecuteNext method algorithm (specs.md section 8.2):
      1. BEGIN TRANSACTION
      2. SELECT next enforced task WHERE status='pending' using ITaskRepository.GetNextEnforcedTask()
      3. If no task found, return NoTaskPending result
      4. Validate task using IValidation
      5. Resolve appropriate IExecutor based on task.type using factory/strategy pattern
      6. Execute task via executor.Execute()
      7. Write artifacts if any (via IMcpFileSystemService)
      8. Audit log written automatically via decorator
      9. Event log written automatically via interceptor
      10. Update task status to 'completed' via ITaskRepository
      11. COMMIT TRANSACTION
      12. If ANY step fails → ROLLBACK → task stays 'pending' (specs.md section 8.2)
    - No autonomous loops: engine executes ONE task per call (specs.md section 8.1)
    - Task failures: log error, keep task pending, return failure result
  - **Acceptance**: Engine executes one task at a time, transactions properly scoped, failures rollback cleanly, completed tasks never re-execute, safe for restart (US-11.1)

## LLM Integration

- [ ] **Task 14: Create LLM service interface and models**
  - **References**: specs.md section 7 (LLM contract with bounded input/output), user-stories.md US-12.1 (bounded LLM usage)
  - **Location**: `backend/OutreachGenie.Core/Interfaces/Services/IInference.cs` and `backend/OutreachGenie.Core/Models/Llm/`
  - **Requirements**:
    - IInference interface with method: Task<IProposal> ProposeActions(IContext context)
    - IContext record (immutable): Campaign (name, goal), OpenTasks (list), CompletedTasks (list), RecentChat (last N messages) - matches specs.md section 7.2
    - IProposal record (immutable): NewTasks (list of task proposals), CompleteTasks (list of task IDs), Notes (explanation) - matches specs.md section 7.3
    - ITaskProposal record: Title, Description, TaskType (ephemeral/enforced)
    - All models must be immutable records or classes with init-only properties
    - LLM input capped at N tokens (configurable, default 4000) per user-stories.md US-12.1
    - LLM may propose tasks but NEVER mark enforced tasks complete (specs.md section 7.1)
  - **Acceptance**: Interface and DTOs defined matching specs.md section 7 exactly, immutability enforced, no conversation history passed (stateless)

- [ ] **Task 15: Implement LLM service**
  - **References**: specs.md section 7.3 (output schema), user-stories.md US-12.1 (cheap model, bounded tokens)
  - **Location**: `backend/OutreachGenie.Infrastructure/Services/OpenAiInferenceService.cs`
  - **Requirements**:
    - Sealed class implementing IInference
    - Use OpenAI API (or Anthropic as alternative) with GPT-4o-mini or equivalent cheap model
    - Inject IOptions<LlmSettings> for API key, model name, max tokens, temperature
    - ProposeActions method:
      1. Serialize IContext to JSON matching specs.md section 7.2
      2. Send to LLM with system prompt: "You propose tasks and completions. Output JSON matching schema: {specs.md section 7.3}"
      3. Parse JSON response into IProposal
      4. Validate: ensure no enforced tasks marked complete (specs.md section 7.1)
      5. Return IProposal or throw if validation fails
    - Token limits: cap input context, cap output tokens (configurable)
    - Stateless: NO conversation history maintained by LLM (specs.md section 7)
    - Error handling: retry with exponential backoff, log API errors
  - **Acceptance**: LLM service returns valid proposals, enforced task completion proposals rejected, token usage bounded, stateless operation verified

## Application Services

- [ ] **Task 16: Create campaign application services**
  - **References**: user-stories.md EPIC 1 (US-1.1, US-1.2, US-1.3), specs.md section 4.1
  - **Location**: `backend/OutreachGenie.Application/Services/CampaignOrchestrationService.cs`
  - **Requirements**:
    - Sealed class CampaignOrchestrationService encapsulating ICampaignRepository, ITaskRepository, IAuditLogRepository
    - Method CreateCampaign(string name, string userGoal): 
      - Create campaign record (US-1.1)
      - Generate initial system tasks (environment validation, authentication) per US-1.1 acceptance criteria
      - Audit campaign creation
      - Return CampaignDto
    - Method PauseCampaign(Guid id): Update status to 'paused', no enforced tasks execute while paused (US-1.3)
    - Method ResumeCampaign(Guid id): Update status to 'active', allow task execution (US-1.2)
    - Method GetCampaign(Guid id): Return campaign with task summary
    - Method ListCampaigns(): Return all campaigns
    - Use FluentValidation for input validation
    - All operations wrapped in transactions via repository layer
  - **Acceptance**: Campaign CRUD operations work, initial tasks created automatically, pause prevents execution, resume restores execution, validation prevents invalid inputs

- [ ] **Task 17: Create task application services**
  - **References**: user-stories.md EPIC 3 (US-3.1, US-3.2, US-3.3), specs.md section 3
  - **Location**: `backend/OutreachGenie.Application/Services/TaskCoordinationService.cs`
  - **Requirements**:
    - Sealed class TaskCoordinationService encapsulating ITaskRepository, IEngine, IInference
    - Method GenerateTasks(Guid campaignId, string userInput):
      - Build IContext from campaign + recent chat + existing tasks
      - Call IInference.ProposeActions() to get LLM proposals (specs.md section 7)
      - Create EPHEMERAL tasks from proposals (US-3.1)
      - Return TaskDto list
    - Method AddTask(Guid campaignId, ITaskProposal proposal): Create task record (US-3.2)
    - Method ExecuteNextTask(Guid campaignId): 
      - Verify campaign not paused
      - Call IEngine.ExecuteNext() - enforces one-at-a-time rule (US-3.3, specs.md section 8.1)
      - Return execution result
    - Method ListTasks(Guid campaignId): Return all tasks for campaign
    - Method GetTaskStatus(Guid taskId): Return task details
    - Enforced tasks can ONLY be completed by system, not by LLM proposals (specs.md section 3.2)
  - **Acceptance**: Tasks generated via LLM, ephemeral tasks created, enforced tasks execute one-at-a-time, task list queryable, strict execution rules enforced

- [ ] **Task 18: Create chat application services**
  - **References**: user-stories.md EPIC 2 (US-2.1, US-2.2, US-2.3), specs.md section 12 (conversation model)
  - **Location**: `backend/OutreachGenie.Application/Services/ChatInteractionService.cs`
  - **Requirements**:
    - Sealed class ChatInteractionService encapsulating IChatMessageRepository, IInference, ITaskCoordinationService
    - Method SendMessage(Guid? campaignId, string userMessage):
      1. Persist user message to chat_messages (US-2.1)
      2. Build IContext with campaign, tasks, recent chat (last N messages only, specs.md section 12)
      3. Call IInference.ProposeActions() to get LLM response (US-2.2, US-2.3)
      4. If proposals include new tasks, call TaskCoordinationService.GenerateTasks()
      5. If proposals include task completions (ephemeral only), update task status
      6. Generate assistant response explaining actions taken (US-2.2)
      7. Persist assistant message to chat_messages
      8. Return ChatResponseDto with message and any created tasks
    - Method GetChatHistory(Guid? campaignId, int limit): Return recent chat messages
    - Conversation stored but NOT used as primary memory (specs.md section 13: tasks are memory)
    - Assistant explains actions but does not invent actions (US-2.2 acceptance criteria)
  - **Acceptance**: Chat persisted, LLM proposes tasks, ephemeral tasks can be completed via chat, enforced tasks cannot be completed via chat, explanations accurate, no false claims

## API Layer

- [ ] **Task 19: Build API controllers - Campaign**
  - **References**: user-stories.md US-1.1, US-1.2, US-1.3, frontend/src/lib/api.ts (existing API client contract)
  - **Location**: `backend/OutreachGenie.Api/Controllers/CampaignController.cs`
  - **Requirements**:
    - Sealed class CampaignController : ControllerBase with [ApiController] and [Route("api/v1/campaign")]
    - Inject ICampaignOrchestrationService via constructor
    - Endpoints must match frontend api.ts exactly:
      - GET /api/v1/campaign → GetAll() returns Campaign[] (US-1.2)
      - GET /api/v1/campaign/{id} → GetById(Guid id) returns Campaign
      - POST /api/v1/campaign → Create(CreateCampaignRequest request) returns Campaign (US-1.1)
      - POST /api/v1/campaign/{id}/pause → Pause(Guid id) returns 204 NoContent (US-1.3)
      - POST /api/v1/campaign/{id}/resume → Resume(Guid id) returns 204 NoContent (US-1.2)
      - DELETE /api/v1/campaign/{id} → Delete(Guid id) returns 204 NoContent
    - All methods async with CancellationToken
    - Return appropriate HTTP status codes: 200 OK, 201 Created, 204 NoContent, 400 BadRequest, 404 NotFound
    - Use FluentValidation middleware for request validation
    - Add XML documentation for Swagger
  - **Acceptance**: All endpoints respond correctly, match frontend API client, validation errors return 400, not found returns 404, Swagger documentation generated

- [ ] **Task 20: Build API controllers - Task**
  - **References**: user-stories.md US-3.1, US-3.2, US-10.1, frontend/src/lib/api.ts (task endpoints)
  - **Location**: `backend/OutreachGenie.Api/Controllers/TaskController.cs`
  - **Requirements**:
    - Sealed class TaskController : ControllerBase with [ApiController] and [Route("api/v1/task")]
    - Inject ITaskCoordinationService via constructor
    - Endpoints matching frontend api.ts:
      - GET /api/v1/task/list/{campaignId} → GetTasks(Guid campaignId) returns CampaignTask[] (US-10.1)
      - GET /api/v1/task/get/{taskId} → GetTask(Guid taskId) returns CampaignTask
      - POST /api/v1/task/execute/{campaignId} → ExecuteNext(Guid campaignId) returns ExecutionResultDto (US-3.3)
    - DTOs match specs.md section 3.1: id, campaign_id, title, description, status, task_type, created_by, created_at, completed_at
    - Task status updates visible in real-time via SignalR (linked to Task 22)
    - Add XML documentation for Swagger
  - **Acceptance**: Task listing works, task details retrievable, execute endpoint triggers engine, status updates broadcast via SignalR

- [ ] **Task 21: Build API controllers - Chat**
  - **References**: user-stories.md US-2.1, US-2.2, US-2.3, specs.md section 12, frontend/src/lib/api.ts (chat endpoints)
  - **Location**: `backend/OutreachGenie.Api/Controllers/ChatController.cs`
  - **Requirements**:
    - Sealed class ChatController : ControllerBase with [ApiController] and [Route("api/v1/chat")]
    - Inject IChatInteractionService, IHubContext<ChatHub> via constructor
    - Endpoints matching frontend api.ts:
      - POST /api/v1/chat/send → SendMessage(SendMessageRequest request) returns ChatResponse (US-2.1)
        - Request: { campaignId: Guid?, message: string }
        - Response: { messageId: Guid, content: string, timestamp: DateTime }
        - After processing, broadcast message via SignalR ChatHub (Task 22)
      - GET /api/v1/chat/history → GetHistory(Guid? campaignId, int limit = 50) returns ChatMessageDto[] (US-2.1)
    - Message processing: call ChatInteractionService.SendMessage(), await LLM response, broadcast to connected clients
    - Conversation stored per specs.md section 12 but not used as primary memory
    - Add XML documentation for Swagger
  - **Acceptance**: Messages sent and persisted, LLM generates responses, chat history retrievable, real-time updates via SignalR work

- [ ] **Task 22: Implement SignalR hub**
  - **References**: frontend/src/lib/signalr.ts (existing hub client), user-stories.md US-2.1, US-10.1
  - **Location**: `backend/OutreachGenie.Api/Hubs/ChatHub.cs`
  - **Requirements**:
    - Sealed class ChatHub : Hub with route /hubs/chat
    - Hub methods:
      - Server-to-client method: ReceiveChatMessage(messageId, role, content, timestamp) - matches frontend signalRHub.onChatMessageReceived()
      - Server-to-client method: TaskStatusUpdated(campaignId, taskId, status) for real-time task updates (US-10.1)
    - Configure SignalR in Program.cs: builder.Services.AddSignalR(), app.MapHub<ChatHub>("/hubs/chat")
    - Broadcast messages from ChatController after LLM response
    - Broadcast task updates from TaskEngine after execution
    - Connection management: store connections per campaign for targeted broadcasts
    - Add CORS configuration for frontend origin
  - **Acceptance**: SignalR hub accessible at /hubs/chat, frontend connects successfully, chat messages broadcast in real-time, task updates broadcast to relevant clients

- [ ] **Task 23: Configure dependency injection**
  - **References**: specs.md section 2 (architecture topology), backend-code-rules.md (Elegant Objects via DI)
  - **Location**: `backend/OutreachGenie.Api/Program.cs` and `backend/OutreachGenie.Api/Extensions/ServiceCollectionExtensions.cs`
  - **Requirements**:
    - Create extension methods for clean DI registration:
      - AddCoreServices() - register domain interfaces/implementations
      - AddApplicationServices() - register orchestration services (Scoped)
      - AddInfrastructureServices() - register repositories (Scoped), DbContext (Scoped)
      - AddMcpServices() - register MCP wrappers with audit decorators using Scrutor
      - AddLlmServices() - register IInference implementation (Scoped)
      - AddEngineServices() - register IEngine, executors (Scoped)
    - Service lifetimes:
      - Scoped: DbContext, repositories, orchestration services, engine, executors
      - Singleton: ILogger, IConfiguration, IOptions<T>
      - Transient: validators
    - Use Scrutor for automatic decorator registration: services.Decorate<ISqlOperation, AuditedSqlOperationDecorator>()
    - Register all executors via assembly scanning: services.Scan(scan => scan.FromAssemblyOf<IExecutor>().AddClasses().AsImplementedInterfaces())
    - Configure IOptions<T> for: LlmSettings, McpSettings, DatabaseSettings from appsettings
  - **Acceptance**: All services registered with correct lifetimes, decorators applied automatically, assembly scanning finds all executors, configuration bound to IOptions

- [ ] **Task 24: Configure API middleware**
  - **References**: specs.md section 2 (API topology), user-stories.md US-5.1 (environment validation)
  - **Location**: `backend/OutreachGenie.Api/Program.cs` and `backend/OutreachGenie.Api/Middleware/`
  - **Requirements**:
    - Configure middleware pipeline in Program.cs (order matters):
      1. Serilog request logging: UseSerilogRequestLogging()
      2. CORS: UseCors() with frontend origin from appsettings
      3. Exception handling: UseExceptionHandler() with custom error response
      4. HTTPS redirection: UseHttpsRedirection()
      5. Authentication/Authorization: UseAuthentication(), UseAuthorization() (prepare for future JWT)
      6. Swagger: UseSwagger(), UseSwaggerUI() in Development only
      7. SignalR: MapHub<ChatHub>()
      8. Controllers: MapControllers()
    - Create GlobalExceptionHandlerMiddleware: catch all exceptions, log to Serilog, return standardized error response { message, statusCode, details }
    - Configure Serilog: write to Console and Seq (if configured), structured logging with campaign_id, task_id in log context
    - Configure Swagger: XML comments enabled, JWT bearer auth UI (prepare for future), API versioning v1
    - CORS policy: allow frontend origin, allow credentials for SignalR
    - Health checks: AddHealthChecks() with PostgreSQL check, expose at /health
  - **Acceptance**: Middleware pipeline configured correctly, exceptions handled gracefully, CORS allows frontend, Swagger UI accessible, health checks respond, Serilog logging works

## Deployment

- [ ] **Task 25: Create database migrations**
  - **References**: specs.md sections 3-5 (all table schemas), user-stories.md US-5.2 (provision campaign database)
  - **Location**: `backend/OutreachGenie.Infrastructure/Migrations/`
  - **Requirements**:
    - Run: dotnet ef migrations add InitialCreate --project OutreachGenie.Infrastructure --startup-project OutreachGenie.Api
    - Migration must create tables matching specs.md exactly:
      - campaigns: id (uuid PK), name (text), user_goal (text), status (text), created_at (timestamp)
      - tasks: id (uuid PK), campaign_id (uuid FK), title (text), description (text), status (text CHECK), task_type (text CHECK), created_by (text CHECK), created_at (timestamp), completed_at (timestamp nullable)
      - leads: id (uuid PK), campaign_id (uuid FK), full_name (text), linkedin_url (text), attributes (jsonb), score (numeric), status (text), created_at (timestamp)
      - audit_log: id (uuid PK), campaign_id (uuid FK nullable), action (text), metadata (jsonb), created_at (timestamp)
      - event_log: id (uuid PK), campaign_id (uuid FK nullable), entity_type (text), entity_id (uuid), operation (text CHECK), before_state (jsonb), after_state (jsonb), created_at (timestamp)
      - chat_messages: id (uuid PK), campaign_id (uuid FK nullable), role (text), content (text), created_at (timestamp)
    - Indexes: create indexes per Task 5 requirements
    - Foreign key constraints: cascade delete from campaigns to tasks/leads/chat_messages
    - Apply migration: dotnet ef database update (requires PostgreSQL running)
  - **Acceptance**: Migration file generated, schema matches specs.md, migration applies cleanly to PostgreSQL, all constraints and indexes created

- [ ] **Task 26: Create Docker Compose configuration**
  - **References**: user-stories.md US-5.2 (provision database via Docker), specs.md section 5 (environment)
  - **Location**: `docker-compose.yml` and `docker-compose.override.yml` in repository root
  - **Requirements**:
    - PostgreSQL service:
      - Image: postgres:17-alpine
      - Container name: outreachgenie-postgres
      - Environment variables: POSTGRES_DB=outreachgenie, POSTGRES_USER=outreach_user, POSTGRES_PASSWORD=<from env or secrets>
      - Port mapping: 5432:5432
      - Volume: postgres-data:/var/lib/postgresql/data (persist data)
      - Health check: pg_isready command
    - Seq service (optional, for log aggregation):
      - Image: datalust/seq:latest
      - Port mapping: 5341:80
      - Environment: ACCEPT_EULA=Y
    - Networks: create app-network for inter-service communication
    - Override file for development: expose ports, mount volumes for hot reload
  - **Acceptance**: docker-compose up starts PostgreSQL and Seq, database accessible at localhost:5432, health checks pass, data persists across restarts

- [ ] **Task 27: Create appsettings configuration**
  - **References**: specs.md sections 2, 7 (LLM settings), user-stories.md US-5.1 (validate environment)
  - **Location**: `backend/OutreachGenie.Api/appsettings.json` and `appsettings.Development.json`
  - **Requirements**:
    - ConnectionStrings:
      - DefaultConnection: "Host=localhost;Port=5432;Database=outreachgenie;Username=outreach_user;Password=<password>"
    - LlmSettings:
      - ApiKey: "<OPENAI_API_KEY or ANTHROPIC_API_KEY from environment>"
      - ModelName: "gpt-4o-mini" (cheap model per user-stories.md US-12.1)
      - MaxInputTokens: 4000
      - MaxOutputTokens: 1000
      - Temperature: 0.7
    - McpSettings:
      - AgentWorkingDir: "C:/campaigns" or "/var/campaigns" (per specs.md section 6)
      - SqlConnectionString: same as DefaultConnection
      - PlaywrightBrowserPath: "<path to Playwright browsers>"
    - Serilog:
      - MinimumLevel: Information (Debug in Development)
      - WriteTo: Console, Seq (if configured)
      - Enrich: FromLogContext, WithMachineName, WithThreadId
    - Cors:
      - AllowedOrigins: ["http://localhost:5173"] (Vite dev server)
    - Environment validation on startup: check OPENAI_API_KEY, AgentWorkingDir exists, PostgreSQL reachable (per US-5.1)
  - **Acceptance**: Configuration files complete, secrets externalized to environment variables or user secrets, validation catches missing config on startup

## Testing & Integration

- [ ] **Task 28: Update frontend API types**
  - **References**: frontend/src/lib/api.ts (existing types), specs.md sections 3-4 (domain models)
  - **Location**: `frontend/src/lib/api.ts`
  - **Requirements**:
    - Ensure TypeScript interfaces match C# DTOs exactly:
      - Campaign: id (string/UUID), name, status (enum), userGoal (not targetAudience), createdAt, updatedAt
      - CampaignTask: id, campaignId, title (not description), description, status, type (not taskType), createdBy, createdAt, completedAt
      - TaskStatus enum: Pending, InProgress, Completed, Cancelled (matches specs.md section 3.1)
      - CampaignStatus enum: Active, Paused (matches user-stories.md US-1.3, remove Draft/Completed/Cancelled)
      - ChatMessageDto: id, campaignId, role, content, timestamp (matches specs.md section 12)
    - Update API client methods to use correct DTOs
    - Fix any mismatches between frontend expectations and backend contracts
  - **Acceptance**: Frontend types match backend DTOs exactly, no type errors in api.ts, API client calls work correctly

- [ ] **Task 29: Test campaign creation flow**
  - **References**: user-stories.md US-1.1 (create campaign), specs.md section 4.1
  - **Location**: Manual test or `backend/OutreachGenie.Api.Tests/Integration/CampaignFlowTests.cs`
  - **Requirements**:
    - Test scenario:
      1. Start PostgreSQL via docker-compose up
      2. Run backend API: dotnet run --project OutreachGenie.Api
      3. Start frontend: npm run dev (in frontend directory)
      4. Navigate to http://localhost:5173
      5. Create new campaign with name "Test Campaign" and goal "Find 10 leads"
      6. Verify campaign appears in database: SELECT * FROM campaigns;
      7. Verify initial system tasks created: SELECT * FROM tasks WHERE campaign_id = '<campaign_id>';
      8. Verify audit logs: SELECT * FROM audit_log WHERE campaign_id = '<campaign_id>';
      9. Verify event logs: SELECT * FROM event_log WHERE campaign_id = '<campaign_id>';
    - Acceptance criteria from US-1.1: campaign has unique ID, stores name and goal, starts in active status, initial tasks created automatically
  - **Acceptance**: Campaign created via UI persists to database, initial tasks generated, audit and event logs populated, end-to-end flow works

- [ ] **Task 30: Test chat and task generation flow**
  - **References**: user-stories.md US-2.1, US-3.1, US-3.2 (chat generates tasks), specs.md section 7 (LLM proposes tasks)
  - **Location**: Manual test or `backend/OutreachGenie.Api.Tests/Integration/ChatFlowTests.cs`
  - **Requirements**:
    - Test scenario:
      1. Create campaign (from Task 29)
      2. Open chat interface in frontend
      3. Send message: "Help me find software engineers in Seattle"
      4. Verify LLM response appears in UI
      5. Verify new EPHEMERAL tasks created: SELECT * FROM tasks WHERE campaign_id = '<id>' AND task_type = 'ephemeral';
      6. Verify chat messages persisted: SELECT * FROM chat_messages WHERE campaign_id = '<id>';
      7. Send message: "Execute the next task"
      8. Call POST /api/v1/task/execute/{campaignId} from UI
      9. Verify ENFORCED task executed: SELECT * FROM tasks WHERE status = 'completed' AND task_type = 'enforced';
      10. Verify SignalR real-time updates in UI
    - Acceptance criteria: chat persisted, LLM proposes tasks (US-2.1), tasks created in DB (US-3.1), enforced tasks execute (US-3.3), real-time updates work (US-10.1)
  - **Acceptance**: Complete chat-to-execution flow works, tasks generated via LLM, enforced tasks execute via engine, all updates visible in UI and database

---

## Architecture Overview

This implementation follows the specifications in specs.md:

- **Task-Driven System**: Tasks stored in SQL are the primary control structure, not LLM conversation
- **Elegant Objects**: All classes follow Yegor Bugayenko's principles (sealed, interface-based, immutable)
- **DDD**: Domain-Driven Design with proper layer separation (Core, Application, Infrastructure, API)
- **Audit & Event Logging**: Every action and DB mutation is logged
- **MCP Integration**: All external operations go through MCP servers with audit wrappers
- **Stateless LLM**: LLM proposes plans; system enforces execution
- **Task Engine**: Enforces one-enforced-task-at-a-time with transaction boundaries
