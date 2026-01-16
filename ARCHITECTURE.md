# OutreachGenie Architecture: Microsoft Agent Framework Implementation

## Executive Summary

This architecture solves the **Section 9 issues** (GitHub Copilot failures) by:
1. **Externalizing all state** outside the LLM using Microsoft Agent Framework state management
2. **Enforcing task execution** through deterministic middleware and database constraints
3. **Providing complete auditability** via comprehensive event logging
4. **Enabling recovery** through durable state and thread persistence

## Problem Statement (Section 9)

GitHub Copilot agent failures:
- ❌ Failed to consistently follow TODO lists
- ❌ Skipped critical steps (logging, persistence)
- ❌ Lost state during conversation summarization
- ❌ Exhibited non-deterministic behavior
- ❌ Token usage increased rapidly for long campaigns

## Solution Architecture

### Core Principles

1. **LLM as Reasoning Engine Only**: The LLM provides recommendations; **the system enforces execution**
2. **State Outside LLM**: All state lives in database and Agent Framework state management
3. **Deterministic Execution**: Middleware enforces procedural guarantees
4. **Event Sourcing**: Every action is logged and auditable
5. **Fail-Fast**: Violations are detected immediately and block progression

### Technology Stack

- **Backend**: .NET 10 with ASP.NET Core
- **Agent Framework**: Microsoft Agent Framework with AG-UI protocol
- **State Management**: Entity Framework Core + Agent Framework state persistence
- **Real-time Communication**: SignalR for UI updates
- **Frontend**: React + TypeScript + shadcn/ui
- **Database**: SQL Server / SQLite (for MVP)

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    React Frontend                       │
│  (Chat UI, Campaign Monitor, Task Viewer)               │
└──────────────────┬──────────────────────────────────────┘
                   │ SignalR + HTTP
┌──────────────────▼──────────────────────────────────────┐
│              ASP.NET Core API                           │
│  - AG-UI Endpoints (Agent interaction)                  │
│  - SignalR Hubs (Real-time updates)                     │
│  - REST Controllers (Campaign CRUD)                     │
└──────────────────┬──────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│           Campaign Orchestrator                         │
│  - State Machine (enforces workflow)                    │
│  - Task Enforcement Middleware                          │
│  - Algorithm Validator                                  │
└──────────────────┬──────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│        Microsoft Agent Framework Layer                  │
│  - AIAgent (LLM reasoning)                              │
│  - State Management (persistent context)                │
│  - Tool Definitions (capabilities)                      │
│  - Event Handlers (audit logging)                       │
└──────────────────┬──────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│            Domain Services Layer                        │
│  - Campaign Service (lifecycle)                         │
│  - Task Service (enforcement)                           │
│  - Lead Service (scoring, deduplication)                │
│  - Artifact Service (file management)                   │
│  - Event Log Service (audit trail)                      │
└──────────────────┬──────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│            Data Access Layer                            │
│  - EF Core DbContext                                    │
│  - Repositories (per aggregate root)                    │
│  - Unit of Work                                         │
└──────────────────┬──────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│              Database (SQL Server)                      │
│  Tables: Campaigns, Tasks, Leads, Events, Artifacts    │
└─────────────────────────────────────────────────────────┘
```

## Solving Section 9 Issues

### 1. Task Enforcement (Prevents Skipping Steps)

**Problem**: LLM ignores TODO lists and skips critical steps

**Solution**: `TaskEnforcementMiddleware` + Database Constraints

```csharp
// Middleware intercepts every agent action
public sealed class TaskEnforcementMiddleware : DelegatingAIAgent
{
    public override async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(...)
    {
        // 1. Load current campaign state from DB (not from LLM memory)
        var campaign = await _campaignRepository.LoadById(campaignId);
        
        // 2. Get next required task
        var nextTask = await _taskService.NextRequiredTask(campaign);
        
        // 3. BLOCK if attempting to skip a task
        if (nextTask != null && !IsValidAction(update, nextTask))
        {
            throw new TaskEnforcementException(
                $"Cannot proceed. Must complete task: {nextTask.Title}");
        }
        
        // 4. Log all actions to audit table (append-only)
        await _eventLog.Append(new TaskAttemptedEvent(...));
        
        // 5. Only allow progression if task completion is verified in DB
        await foreach (var update in base.RunStreamingAsync(...))
        {
            yield return update;
        }
    }
}
```

**Guarantees**:
- ✅ Tasks cannot be skipped (enforced by middleware, not LLM)
- ✅ Task state persists in database, not in conversation
- ✅ System can resume from exact task after crash
- ✅ Every attempted action is logged (audit trail)

### 2. State Management (Prevents Context Loss)

**Problem**: Conversation summarization loses campaign state

**Solution**: Agent Framework State Management + Database Persistence

```csharp
// State lives outside LLM context
public sealed class CampaignState
{
    public Guid CampaignId { get; set; }
    public string CurrentPhase { get; set; }  // "DISCOVERY" | "SCORING" | "OUTREACH"
    public int CompletedTasks { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

// State is managed by Agent Framework, persisted to DB
public sealed class CampaignStateManager
{
    private readonly AgentThread _thread;
    private readonly IRepository<Campaign> _campaignRepo;
    
    public async Task<CampaignState> LoadState()
    {
        // Load from database, not from LLM context
        var campaign = await _campaignRepo.LoadWithTasks(campaignId);
        
        return new CampaignState
        {
            CampaignId = campaign.Id,
            CurrentPhase = campaign.Phase.Name,
            CompletedTasks = campaign.Tasks.Count(t => t.Status == TaskStatus.Completed),
            Metadata = campaign.Metadata
        };
    }
    
    public async Task SaveState(CampaignState state)
    {
        // Persist to database AND Agent Framework thread
        await _campaignRepo.Update(state);
        
        // Inject into AG-UI state for bidirectional sync
        _thread.State["campaign"] = state;
    }
}
```

**Guarantees**:
- ✅ State survives conversation truncation
- ✅ LLM hallucinations cannot corrupt state
- ✅ Campaign can be resumed from any point
- ✅ State is source of truth, not chat history

### 3. Event Sourcing (Complete Auditability)

**Problem**: No record of what agent actually did

**Solution**: Append-Only Event Log + Event Handlers

```csharp
// Every domain event is logged
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
}

public sealed class TaskCompletedEvent : IDomainEvent
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public Guid CampaignId { get; set; }
    public string CompletedBy { get; set; }  // "Agent" | "User"
    // Includes full context for reconstruction
}

// Event log repository (append-only)
public interface IEventLog
{
    Task Append(IDomainEvent @event);
    Task<IEnumerable<IDomainEvent>> GetEvents(Guid campaignId);
    Task<CampaignState> Reconstruct(Guid campaignId);  // Rebuild from events
}
```

**Guarantees**:
- ✅ Complete audit trail of all actions
- ✅ State can be reconstructed from events
- ✅ System is debuggable and explainable
- ✅ Compliance-ready logs

### 4. Algorithm Enforcement (Deterministic Execution)

**Problem**: LLM makes non-deterministic decisions

**Solution**: State Machine + Validation Rules

```csharp
// Campaign workflow is a state machine
public enum CampaignPhase
{
    Planning,      // User defines goals
    Discovery,     // Agent finds leads
    Scoring,       // Agent scores leads
    Outreach,      // Agent generates messages
    Monitoring,    // Track responses
    Complete
}

// Transitions are explicitly defined and enforced
public sealed class CampaignStateMachine
{
    private static readonly Dictionary<CampaignPhase, List<CampaignPhase>> Transitions = new()
    {
        { CampaignPhase.Planning, new() { CampaignPhase.Discovery } },
        { CampaignPhase.Discovery, new() { CampaignPhase.Scoring } },
        { CampaignPhase.Scoring, new() { CampaignPhase.Outreach } },
        // ... etc
    };
    
    public Result<CampaignPhase> Transition(CampaignPhase from, CampaignPhase to)
    {
        // ENFORCE: Invalid transitions are blocked
        if (!Transitions[from].Contains(to))
        {
            return Result.Failure<CampaignPhase>(
                $"Cannot transition from {from} to {to}");
        }
        
        // VALIDATE: All required tasks for current phase are complete
        var incomplete = _taskService.GetIncompleteTasks(campaignId, from);
        if (incomplete.Any())
        {
            return Result.Failure<CampaignPhase>(
                $"Cannot advance. Incomplete tasks: {string.Join(", ", incomplete)}");
        }
        
        return Result.Success(to);
    }
}
```

**Guarantees**:
- ✅ Campaign follows predefined algorithm
- ✅ Invalid state transitions are impossible
- ✅ LLM cannot skip phases
- ✅ Execution is predictable

### 5. Recovery & Resilience

**Problem**: System cannot recover from interruptions

**Solution**: Durable State + Idempotent Operations

```csharp
// Every operation is idempotent and resumable
public interface IResumableOperation
{
    Guid OperationId { get; }
    bool IsComplete { get; }
    Task Resume();
}

public sealed class LeadDiscoveryOperation : IResumableOperation
{
    public async Task Resume()
    {
        // Load last checkpoint from database
        var checkpoint = await _db.Checkpoints.Find(OperationId);
        
        // Resume from last successful step
        var remainingSteps = _algorithm.StepsAfter(checkpoint.LastStep);
        
        foreach (var step in remainingSteps)
        {
            await ExecuteStep(step);
            
            // Save checkpoint after each step
            await _db.Checkpoints.Update(OperationId, step);
        }
    }
}
```

**Guarantees**:
- ✅ Operations can be resumed mid-execution
- ✅ No silent failures
- ✅ System self-heals after crashes
- ✅ No duplicate work on retry

## Database Schema

```sql
-- Campaigns: Main aggregate root
CREATE TABLE Campaigns (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Phase VARCHAR(50) NOT NULL,  -- Enum: Planning, Discovery, etc.
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    Metadata NVARCHAR(MAX) -- JSON
);

-- Tasks: Enforced checklist
CREATE TABLE Tasks (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CampaignId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Campaigns(Id),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Status VARCHAR(50) NOT NULL,  -- Pending, InProgress, Completed, Blocked
    OrderIndex INT NOT NULL,  -- Execution order
    RequiresApproval BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    CONSTRAINT CHK_Task_Status CHECK (Status IN ('Pending', 'InProgress', 'Completed', 'Blocked'))
);

-- Leads: Discovered prospects
CREATE TABLE Leads (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CampaignId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Campaigns(Id),
    Source NVARCHAR(200) NOT NULL,
    Score DECIMAL(5,2) NULL,
    ScoringRationale NVARCHAR(MAX),
    Data NVARCHAR(MAX) NOT NULL,  -- JSON: profile, contact info
    CreatedAt DATETIME2 NOT NULL,
    ScoredAt DATETIME2 NULL
);

-- Events: Audit log (append-only)
CREATE TABLE Events (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType VARCHAR(100) NOT NULL,
    CampaignId UNIQUEIDENTIFIER NULL,
    Timestamp DATETIME2 NOT NULL,
    Actor VARCHAR(50) NOT NULL,  -- "Agent" | "User" | "System"
    Payload NVARCHAR(MAX) NOT NULL,  -- JSON event data
    CONSTRAINT CHK_Event_Actor CHECK (Actor IN ('Agent', 'User', 'System'))
);

-- Artifacts: Generated files
CREATE TABLE Artifacts (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CampaignId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Campaigns(Id),
    FileName NVARCHAR(500) NOT NULL,
    FilePath NVARCHAR(1000) NOT NULL,
    MimeType VARCHAR(100),
    Version INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    DeletedAt DATETIME2 NULL
);

-- Agent Threads: Persistent conversation state
CREATE TABLE AgentThreads (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CampaignId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Campaigns(Id),
    ThreadId VARCHAR(200) NOT NULL UNIQUE,  -- Agent Framework thread ID
    State NVARCHAR(MAX),  -- JSON serialized state
    CreatedAt DATETIME2 NOT NULL,
    LastAccessedAt DATETIME2 NOT NULL
);
```

## Agent Tools

The agent has access to these tools (all actions are logged):

```csharp
// Each tool enforces its own invariants
[AIFunction("Create a task in the campaign")]
public async Task<Result> CreateTask(
    Guid campaignId,
    string title,
    string description,
    int orderIndex)
{
    // 1. Validate campaign exists and is in valid phase
    // 2. Create task in database
    // 3. Log TaskCreatedEvent
    // 4. Return success/failure
}

[AIFunction("Mark task as completed")]
public async Task<Result> CompleteTask(Guid taskId)
{
    // 1. Validate task exists and is not blocked
    // 2. Update task status in database
    // 3. Log TaskCompletedEvent
    // 4. Check if campaign can advance to next phase
    // 5. Return success/failure
}

[AIFunction("Discover leads for campaign")]
public async Task<Result> DiscoverLeads(Guid campaignId, string criteria)
{
    // 1. Validate campaign is in Discovery phase
    // 2. Execute search (external API)
    // 3. Deduplicate against existing leads
    // 4. Insert leads into database
    // 5. Log LeadsDiscoveredEvent
}

[AIFunction("Score a lead")]
public async Task<Result> ScoreLead(Guid leadId, decimal score, string rationale)
{
    // 1. Validate lead exists
    // 2. Update lead score in database
    // 3. Log LeadScoredEvent
    // 4. Store rationale for auditability
}
```

## MVP Implementation Plan (5-7 Days)

### Day 1-2: Core Infrastructure
- ✅ EF Core models + migrations
- ✅ Repository pattern implementation
- ✅ Event log service
- ✅ Task enforcement middleware

### Day 3-4: Agent Framework Integration
- ✅ Agent setup with state management
- ✅ Tool definitions
- ✅ Campaign orchestrator
- ✅ State machine implementation

### Day 5-6: Frontend Integration
- ✅ SignalR hub for real-time updates
- ✅ React components for campaign monitor
- ✅ Task list viewer
- ✅ Chat interface

### Day 7: Testing & Documentation
- ✅ Recovery scenario testing
- ✅ Task enforcement testing
- ✅ Documentation

## Success Criteria

The implementation is successful if:
1. ✅ No critical step can be skipped (enforced by middleware)
2. ✅ All actions are auditable (event log)
3. ✅ Campaigns are recoverable (durable state)
4. ✅ LLM failures do not corrupt state (state outside LLM)
5. ✅ Execution is deterministic (state machine)

## Comparison: Before vs After

| Issue | GitHub Copilot (Before) | Agent Framework (After) |
|-------|------------------------|------------------------|
| **Task Execution** | LLM ignores TODO list | Middleware enforces tasks |
| **State Loss** | Context truncation loses data | State in DB, not chat |
| **Auditability** | No record of actions | Complete event log |
| **Recovery** | Cannot resume | Durable state + checkpoints |
| **Determinism** | Non-deterministic | State machine enforces workflow |
| **Token Usage** | Grows unbounded | Constant (state externalized) |

## Conclusion

This architecture **solves all Section 9 issues** by:
1. **Not trusting the LLM** for state or execution
2. **Externalizing all guarantees** to deterministic systems
3. **Providing complete observability** via event sourcing
4. **Enabling recovery** through durable state management

The LLM is treated as a **reasoning component only** - it suggests actions, but **the system enforces correctness**.
