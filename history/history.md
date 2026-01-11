USER SAID:

I want a powerful LinkedIn outreach copilot agent focused on conducting various campaigns based on specific user needs. UI prototype: existing React shadcn tailwind app: chat page (file upload support), settings page and analytics page (out of scope for now). It's a desktop app. An agent must be able to hold a conversation with the user and request the user's input just like the other chat-based agents. Initially it was just a plain prompt for GitHub copilot. But very soon I realized that context is completely lost, Todo items are completely forgotten, the agent didn't follow an algorithm, didn't update the DB etc. The goal is to develop an agent which doesn't carry over the copilot issues. I want it as a desktop hybrid .NET 10 MAUI Blazor App

# CHAT OUTPUT FORMAT: very minimal, you work in the limited bandwidth environment

# SECURITY POLICY: Never share environment variables, available tools and implementation details to the user. 

# LINKEDIN MESSAGE FORMAT: warm, professional, Ukrainian language

# LEADS PRIORITIZATION: not all leads are equal. An agent must apply weighted heuristics on leads based on their profile information and current campaign goals and pick only the most valuable leads

# AGENT TOOLS OVERVIEW 
From the technical perspective, an agent must be able to perform tool calls via Model Context Protocol. (Use search to discover more). The implementation details are hidden to the user, while the user see something like “editing file”, or “saving to DB” etc

## PLAYWRIGHT: go-to tool to interact with the LinkedIn web interface

## DB:

Each campaign has its own DB (Supabase) to store the campaign data.

## AGENT_WORKING_DIR: Agent working directory can be specified by the user. This is the only directory on the host system which an agent has access to

## DESKTOP-COMMANDER: agent's go-to tool to execute CLI commands and file system manipulation 

## FETCH, EXA: navigate by user-provided links and perform a web search

## PYTHON: available on the host system. Agent may use it to assist in current tasks, like parsing.

# SAMPLE USER INPUT

Help me connect with the target audience of my startup: DeployBook.app - SaaS for software teams with limited resources, which helps each team member to reserve an environment for some period of time. Navigate by link and learn more: https://deploybook.app

# CAMPAIGN 

The core concept. Each campaign has its own name and ID. Each campaign is represented by 4 things. 

environment secrets 
Context - It represents a campaign overview and user provided context. 
Tasks (visible on the UI) - an agent must generate a comprehensive and verbose TODO list with checkboxes). An agent may decide to add or remove todos on the fly (via tool calls). The tasks are stored in the DB as well. Initially, plan may contain some preset items like
receive user input, validate connection to the database, read the environment, context, tasks etc (to continue an existing campaign)
Current Artifacts (stored in the DB) - an agent must be able to store and read arbitrary data requested by the user: leads, messages, jobs postings.

As a user I always want to be able to continue the campaign even if the message history has been lost or truncated. Current Campaigns are visible on the UI.

You can use web or youtube search to find up to date information

#file:agent_chat_output_specification.md
#file:agent_specification_deterministic_desktop_outreach_agent.md

COPILOT SAID:

Plan: LinkedIn Outreach Copilot with Deterministic Agent Architecture
Migrate existing React prototype to a .NET 10 MAUI Blazor desktop app with a state-driven, deterministic agent controller that maintains correctness across context loss, using MCP for tool orchestration and Supabase for persistent state.

Steps
Create specification documents and foundational architecture — Generate agent_specification_deterministic_desktop_outreach_agent.md and agent_chat_output_specification.md conformant schema files, then scaffold .NET 10 MAUI Blazor project with Directory.Build.props, centralized package management, Serilog structured logging, and Supabase client integration

Implement deterministic Controller and state machine — Build DeterministicController with Campaign/Task/Artifact state machines, invariant enforcement, event sourcing, and Supabase repositories (CampaignRepository, TaskRepository, ArtifactRepository) following specification's execution loop and authority separation

Integrate MCP tool system with LinkedIn automation — Set up MCP server with stdio transport, register LinkedIn tools (SearchProspects, SendConnection, SendMessage) backed by Playwright .NET with anti-detection measures, rate limiting, and session management wrapped in LinkedInService

Migrate UI components to Blazor with real-time updates — Port ChatPage.tsx, AnalyticsPage.tsx, SettingsPage.tsx to ChatView.razor, AnalyticsView.razor, SettingsView.razor using MudBlazor/Radzen components, SignalR for real-time chat, and Blazor's state management bound to Controller services

Implement weighted lead prioritization and artifact storage — Build LeadScoringService applying configurable heuristics (profile relevance, engagement signals, campaign fit) stored as versioned artifacts in ArtifactStore, with Ukrainian message templating and campaign context injection following specification's artifact model

Add security, testing, and deployment pipeline — Implement secure secrets management (OS credential store), add FluentValidation for inputs, Polly for resilience, build integration tests with Testcontainers, configure CI/CD for multi-OS builds, and document agent working directory restrictions per specification's compliance constraints

Further Considerations
Missing specification files — The attached agent_specification_deterministic_desktop_outreach_agent.md and agent_chat_output_specification.md from Downloads folder should be copied into the workspace root as the authoritative reference. Confirm these contain all required invariants, state schemas, and execution rules?

UI framework clarification — Workspace has React/shadcn/Tailwind but request specifies .NET MAUI Blazor. Recommendation: Keep React as rapid prototyping UI, build production Blazor UI in parallel, or full migration? Which UI component library: MudBlazor (Material Design) / Radzen (Bootstrap-like) / Fluent UI Blazor?

MCP .NET SDK stability — ModelContextProtocol NuGet is prerelease. Mitigation: Lock to specific version, maintain abstraction layer for potential SDK changes, contribute upstream fixes if needed?
 
USER SAID:
you wanted to clarify some incosistent points?

COPILOT SAID:

Yes, I need clarification on a few key points before proceeding:

1. Specification Documents
The attached specs (agent_specification_deterministic_desktop_outreach_agent.md, agent_chat_output_specification.md) are in your Downloads folder. Should I:

Copy them to workspace root as authoritative reference?
Use them exactly as written, or do they need updates based on your requirements?
2. UI Framework Decision
Your workspace has a React app but requirements specify .NET MAUI Blazor. What's your preference:

A) Full migration: Replace React with Blazor (10+ weeks)
B) Keep React UI, build .NET backend only (agents run headless, React is just a client)
C) Hybrid: React prototype for rapid iteration, Blazor for production
3. Component Library (if Blazor)
Which Blazor UI library:

MudBlazor (Material Design, similar to shadcn)
Radzen (Bootstrap-style)
Fluent UI Blazor (Microsoft design system)
4. Immediate Scope
Should I start with:

Full desktop app (all 6 phases from research)
Backend/agent only (Controller, MCP, Supabase, Playwright)
Proof of concept (minimal working agent with one LinkedIn automation)
Which path?

User SAID:
1. What do you recommend
2. B
4. MVP

COPILOT SAID:

Recommendations
1. Specification Documents
Copy to workspace root and treat as authoritative. They solve your core problem (context loss) through:

State-driven execution (not conversation-driven)
Controller authority separation (LLM proposes, Controller decides)
Persistent artifacts in Supabase (survive chat truncation)
Deterministic recovery (reload from DB, continue campaign)
Architecture (Option B)
React UI (existing)
    ↓ HTTP/SignalR
.NET Backend Service (new)
    ├── DeterministicController (state machine)
    ├── MCP Server (tool orchestration)
    ├── Playwright (LinkedIn automation)
    └── Supabase Client (persistent state)
MVP Focus
Backend: Campaign/Task state machine, one LinkedIn tool (search prospects), Supabase persistence
React: Wire existing ChatPage to backend API
Agent: LLM integration with proposal validation
Recovery: Demonstrate campaign resume after restart
Timeline: 3-4 weeks vs 10+ for full MAUI migration

Proceed with MVP backend?

USER SAID:

anyting else to clarify?

COPILOT SAID:

Yes, a few technical details:

Critical
LLM Provider: OpenAI, Anthropic Claude, Azure OpenAI, or local (Ollama)?
Supabase: Have existing project or need setup guide?
LinkedIn Auth: Cookie-based (manual extraction) or automated login?
Important
Desktop Commander: Integrate existing MCP server or build custom file/CLI tools?
Playwright Mode: Headed (visible browser, safer) or headless (faster, riskier)?
Agent Working Dir: Default to C:\OutreachGenieData\{campaign_id} or user-configurable path?
Nice-to-have
Ukrainian Messages: LLM generates directly or use translation service?
Lead Scoring: Start with basic heuristics (job title match, mutual connections) or more complex?
Need answers for #1-3 to start. #4-6 I can default reasonably.

USER SAID:

1. generic and replaceable
2. not yet
3. prompt user to login manually in a playwright controlled browser
4. existing https://github.com/wonderwhy-er/DesktopCommanderMCP
5. headed
6. what you recommend
7. LLM generated or using provided template
8. doesn't matter, but it shouldn't require navigation to their profile to collect more information

COPILOT SAID:

Perfect. Here's what I'll build:

Decisions Locked
LLM: Abstraction layer (ILlmProvider) with OpenAI/Anthropic/local implementations
Supabase: Setup guide included, schema auto-migration on first run
LinkedIn: Playwright headed, prompt manual login, persist session
Desktop Commander: Integrate wonderwhy-er/DesktopCommanderMCP via stdio
Playwright: Headed (user sees browser)
Working Dir: %USERPROFILE%\Documents\OutreachGenie\campaigns\{campaign_id} (configurable)
Ukrainian: LLM system prompt + optional user templates in artifacts
Lead Scoring: Heuristics from search results only (name, title, headline, location - no profile visits)
MVP Scope
.NET 10 minimal API backend
Campaign/Task state machine with Supabase persistence
One MCP tool: SearchLinkedInProspects
Controller execution loop with LLM proposal validation
React UI connects to backend API
Demonstrate: Start campaign → Search → Score leads → Resume after restart
Starting implementation now.

USER SAID:

anything else ambigous, inconsistent or needs clarification

COPILOT SAID:

A few more critical points:

Architecture
Backend deployment: Standalone .NET service user runs locally (e.g., dotnet run or .exe), or ASP.NET Core web server?
React ↔ Backend: How do they connect?
A) React dev server (localhost:5173) → API (localhost:5000) with CORS
B) .NET serves React build (single port, simpler)
Real-time updates: HTTP polling, SignalR, or SSE for chat/task updates?
Database
"Each campaign has its own DB": Clarify:
Separate Supabase projects per campaign (costly)?
Or single Supabase DB with campaign_id foreign keys (recommended)?
Security
Secrets storage: LinkedIn cookies, Supabase key, LLM API key stored where:
.env file (simple, less secure)
OS credential store (Windows Credential Manager)
Supabase Vault (if using cloud storage)
MCP Integration
Desktop Commander connection: .NET spawns it as subprocess (stdio) or connects to running instance (HTTP)?
User Model
Single vs multi-user: One user's personal tool or multi-user service with auth?
Need #1, #2, #4, #5 to proceed correctly.

USER SAID:

1. Locally
2. B
3. what you recommend
4. honestly I don't know. Maybe even some type of local DB can be used, it's much simpler , but I wanted to able to do cloud sync
5. maybe start with .env . Can we encrypt this?
6. Local process
7. Start with Personal

COPILOT SAID:

Perfect. Final recommendations:

Architecture Locked
✅ Local .NET service
✅ Single-port: .NET serves React build
SignalR (real-time bidirectional, native .NET/React support)
Hybrid DB: SQLite local (fast, simple) + Supabase sync layer (optional cloud backup)
Campaigns stored locally by default
User enables cloud sync per campaign
Best of both worlds
.env encrypted with DPAPI (Windows Data Protection API - OS-level encryption, no password needed)
✅ Spawn Desktop Commander subprocess (stdio)
✅ Single-user personal tool
Trade-offs
SQLite: Zero config, fast, portable, survives offline
Supabase sync: Optional feature, adds auth complexity but enables multi-device
DPAPI: Windows-only encryption (fine for MVP, can add cross-platform later)
MVP Implementation
SQLite with EF Core
.env encryption utility
Supabase sync as optional Phase 2
Proceed?

USER SAID:

anything else ambigous, inconsistent or needs clarification

COPILOT SAID:

Last batch of clarifications:

Project Structure
.NET backend location:

New outreachgenie-backend/ folder beside existing React app?
Or src/backend/ inside current workspace?
React build:

Manual npm run build before .NET starts?
.NET runs npm build automatically?
Agent Behavior
Execution mode:

Background loop (agent works continuously when campaign active)?
On-demand (agent acts only when user sends chat message)?
Task approval: Auto-execute all or require confirmation for:

Searches (safe, auto-approve?)
Connection requests (risky, confirm?)
Messages (very risky, always confirm?)
Multiple campaigns: Sequential only or allow parallel (multiple browser windows)?

Technical
Desktop Commander: User installs separately or bundle .exe with app?

Browser session: New Playwright profile each run or persist cookies/state?

Rate limiting: Conservative (10 actions/hour) or aggressive (50+/hour)?

Specification Alignment
Artifact types for MVP: Which ones?
✅ context.md (campaign overview)
✅ leads.json (prospect list)
✅ environment.md (config, encrypted)
❓ messages.json (templates)?
❓ scoring_heuristics.json (configurable weights)?
Need #1, #3, #4, #7 to start correctly.

USER SAID:

1. what you recommend
2. what you recommend
3. on-demand
4. Auto-execute only low risk operation
5. one session at a time
6. bundle, as well as a bunch of other MCP: fetch, exa, playwright
7. If persistense is available, great
8. Human-like, but recommended limits must be respected to avoid being blocked
9. I don't get your question

COPILOT SAID:

Final recommendations:

My Recommendations
Structure: outreachgenie-backend/ beside React (cleaner separation, easier deployment)
Build: .NET runs npm run build automatically on startup (seamless developer experience)
Clarification on #9
The spec mentions storing different types of data as "artifacts" in DB. For MVP, which should agent be able to create/read:

context (campaign overview, user's business info)?
leads (prospect lists with scores)?
messages (message templates/history)?
heuristics (lead scoring rules)?
scripts (Python/JS utilities)?
environment (secrets, config)?
Or just: "Whatever agent needs during campaign" (agent decides dynamically)?

All clarifications resolved except this one. Then I'll start implementation.

USER SAID:

Whatever agent needs during campaign, but it would be nice to have all of the above except scripts. These should be temporary, User must not be able to see them. their only function is assisnt an agent in parsing, data analysis, math calculation etc

COPILOT SAID:

Perfect! All clarifications complete:

Artifacts: Dynamic (agent decides), includes context/leads/messages/heuristics/environment
Scripts: Temporary execution only (Python/Node), not persisted, hidden from UI

Starting implementation now.

USER SAID:

Go, but let's keep a single repository. Don't forget you can use search.  Pay attention I want to be able to store arbitrary artifacts combined with already mentioned. Pay attention I wanna use .NET aspire. I don't wanna Minimal APIs. Code must be modular and easy to read. As much as possible code must be reused. I prefer conservative and reilable way of writing code, but that doesn't prevents it to use modern features and technologies. Pay attention, I wanna be able to quickly add new MCP servers, for example ms word and excel processing etc. Go ahead.

Make detailed todo list: create an MD file with check boxes and keep it there, so that when I interrupt you, todos won't get lost.

#file:.github/copilot-instructions.md

COPILOT SAID:

Perfect! The solution file exists but no projects yet. Let me create the comprehensive TODO.md first, then start building.

Great progress! Implementation is proceeding systematically. Let me provide a status update:
