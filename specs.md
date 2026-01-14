Good — this helps.  
I will reset the frame to your original intent and give you a production-grade, low-level spec that:

keeps only the features you originally wanted

removes the over-engineering we added later

preserves TODOs / tasks as a core concept

enforces audit logging, event logging, DB sync

works with MCP servers

is actually implementable by one developer

does not rely on LLM memory

runs as React UI \+ local .NET 10 API

This is not a theoretical agent spec.  
This is a build spec.

\---

PRODUCTION E2E SPEC

LinkedIn Outreach Copilot (Task-Driven, MCP-Enforced)

\---

0\. What this system actually is (precise definition)

This system is not an autonomous agent.

It is:

\> A task-driven automation runtime where the LLM proposes plans,  
and the system enforces execution, logging, and persistence.

The LLM:

proposes TODOs

proposes heuristics

proposes messages

The system:

owns tasks

owns execution

owns memory

owns side effects

This matches your Cursor / Copilot mental model, not AutoGPT.

\---

1\. Core design decision (non-negotiable)

\> The TODO list is the primary memory and control structure.

Not conversation.  
Not LLM hidden state.  
Not summaries.

Everything flows through tasks stored in SQL.

\---

2\. High-level runtime topology

React UI  
  ↓  
Local ASP.NET Core (.NET 10\)  
  ↓  
Task Engine (deterministic)  
  ↓  
 ├─ MCP: SQL (tasks, leads, logs)  
 ├─ MCP: Filesystem (artifacts)  
 ├─ MCP: Playwright (LinkedIn)  
 ├─ MCP: Desktop-Commander (CLI, Docker)  
 └─ LLM (cheap model, stateless)

\---

3\. Task model (THIS replaces tasks.md)

3.1 Tasks are first-class DB records

CREATE TABLE tasks (  
  id UUID PRIMARY KEY,  
  campaign\_id UUID NOT NULL,  
  title TEXT NOT NULL,  
  description TEXT,  
  status TEXT NOT NULL CHECK (  
    status IN ('pending','in\_progress','completed','cancelled')  
  ),  
  task\_type TEXT NOT NULL CHECK (  
    task\_type IN ('ephemeral','enforced')  
  ),  
  created\_by TEXT NOT NULL CHECK (  
    created\_by IN ('llm','system','user')  
  ),  
  created\_at TIMESTAMP NOT NULL,  
  completed\_at TIMESTAMP  
);

\---

3.2 Task types (critical)

EPHEMERAL

Strategy thinking

Research

Planning

Heuristic design

✅ LLM may complete  
❌ Cannot trigger side effects  
❌ Cannot unblock enforced tasks

ENFORCED

DB writes

File writes

Browser automation

Docker execution

Audit logging

❌ LLM cannot complete  
✅ Only system code can complete  
✅ Must emit audit \+ event log

\---

4\. Campaign data model (minimum viable)

4.1 Campaigns

CREATE TABLE campaigns (  
  id UUID PRIMARY KEY,  
  name TEXT NOT NULL,  
  user\_goal TEXT NOT NULL,  
  status TEXT NOT NULL,  
  created\_at TIMESTAMP NOT NULL  
);

\---

4.2 Leads

CREATE TABLE leads (  
  id UUID PRIMARY KEY,  
  campaign\_id UUID NOT NULL,  
  full\_name TEXT,  
  linkedin\_url TEXT,  
  attributes JSONB,  
  score NUMERIC,  
  status TEXT,  
  created\_at TIMESTAMP  
);

\---

5\. Logging model (exactly what you asked for)

5.1 Audit log (EVERY action)

CREATE TABLE audit\_log (  
  id UUID PRIMARY KEY,  
  campaign\_id UUID,  
  action TEXT NOT NULL,  
  metadata JSONB,  
  created\_at TIMESTAMP NOT NULL  
);

Logged events:

MCP tool calls

Browser navigation

File read/write

CLI execution

DB operations

\---

5.2 Event log (state reconstruction)

CREATE TABLE event\_log (  
  id UUID PRIMARY KEY,  
  campaign\_id UUID,  
  entity\_type TEXT,  
  entity\_id UUID,  
  operation TEXT CHECK (  
    operation IN ('insert','update','delete')  
  ),  
  before\_state JSONB,  
  after\_state JSONB,  
  created\_at TIMESTAMP  
);

Every DB mutation writes here.

\---

6\. Filesystem (artifacts, not memory)

Artifacts live in AGENT\_WORKING\_DIR/{campaign\_id}

Examples:

context.md

environment.md

schema.sql

leads.xlsx

scoring.json

Artifacts are:

created by enforced tasks

referenced by tasks

never relied on by the LLM as memory

\---

7\. LLM contract (strict, cheap, bounded)

7.1 What the LLM is allowed to do

The LLM may:

propose tasks

reprioritize tasks

suggest task completion (ephemeral only)

propose scoring heuristics

draft LinkedIn messages

The LLM may NOT:

mark enforced tasks completed

write files

call MCP tools directly

mutate DB state

\---

7.2 LLM input (NO full conversation)

{  
  "campaign": {  
    "name": "...",  
    "goal": "..."  
  },  
  "open\_tasks": \[  
    { "id": "...", "title": "...", "type": "ephemeral" }  
  \],  
  "completed\_tasks": \[...\],  
  "recent\_chat": \[  
    { "role": "user", "content": "..." }  
  \]  
}

\---

7.3 LLM output schema

{  
  "new\_tasks": \[  
    {  
      "title": "Create scoring heuristics",  
      "description": "...",  
      "task\_type": "ephemeral"  
    }  
  \],  
  "complete\_tasks": \[  
    "task\_id\_123"  
  \],  
  "notes": "Optional explanation"  
}

\---

8\. Task engine (THIS is the core)

8.1 Execution rule

\> The system executes exactly ONE enforced task at a time.

No auto-loops.  
No agent autonomy.

\---

8.2 Task execution flow

SELECT next enforced task WHERE status \= 'pending'  
BEGIN TRANSACTION  
  execute task  
  write artifacts (if any)  
  write audit\_log  
  write event\_log  
  mark task completed  
COMMIT

If anything fails → rollback → task stays pending.

\---

9\. Mapping your original AGENT ALGORITHM to this system

Original step → New implementation

“Generate tasks.md”

→ LLM proposes EPHEMERAL tasks in SQL

“Strictly follow TODOs”

→ System only executes ENFORCED tasks in order

“Perform audit logging”

→ Mandatory MCP audit wrapper

“Save campaign data”

→ ENFORCED DB tasks only

“LLM remembers steps”

→ Tasks remember steps

\---

10\. Lead discovery flow (Playwright)

1\. Enforced task: Authenticate LinkedIn

2\. Enforced task: Search LinkedIn profiles

3\. Enforced task: Extract leads

4\. Enforced task: Persist leads to DB

Each step:

has its own task

has its own audit logs

cannot be skipped

\---

11\. Scoring flow (hybrid)

1\. Ephemeral task: Design scoring heuristics

2\. LLM outputs weights

3\. Enforced task: Apply scoring algorithm

4\. Enforced task: Persist scores

\---

12\. Conversation model (simple, safe)

Conversation:

stored in DB (last N messages)

used ONLY to improve UX

NEVER used to drive execution

The assistant may:

explain task progress

explain why a task is blocked

ask clarifying questions

\---

13\. Memory model (final, practical)

Type	Where	Authority

Tasks	SQL	Absolute  
Leads	SQL	Absolute  
Logs	SQL	Absolute  
Artifacts	FS	Absolute  
Conversation	SQL	None  
LLM	Stateless	None

\---

14\. Tech stack (aligned with your preference)

Backend: ASP.NET Core (.NET 10\)

DB: PostgreSQL (per campaign if you insist)

LLM: cheap model (e.g. GPT-4o-mini)

Agent framework: optional (MAF only as wrapper)

UI: React

Automation: Playwright via MCP

\---

15\. Why this works when your Copilot prompt didn’t

Problem	This spec fixes it by

LLM forgets TODOs	Tasks are stored  
Skipped logging	Logging enforced in code  
Context loss	State is external  
Token explosion	Stateless LLM calls  
Too complex	Single task engine

\---

16\. What you should NOT build (again)

❌ Autonomous agent loops  
❌ Conversation-based control  
❌ Dynamic workflow DSL  
❌ Predicate logic  
❌ Multi-agent systems

\---

Final statement (important)

This system gives you:

Copilot-like UX

strict execution

full auditability

low cost

solo-developer scope

It is exactly the minimal system that satisfies your original prompt without collapsing.

\---

If you want next, I can:

give exact C\# interfaces

give MCP wrapper code

give React UI state model

or walk through one campaign end-to-end

Say which.