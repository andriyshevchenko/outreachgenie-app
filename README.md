# OutreachGenie - LinkedIn Outreach Automation Agent

> **Status**: Infrastructure 100% Complete, Agent Processing Ready for Implementation  
> **Last Updated**: January 11, 2026

## 🎯 Project Overview

OutreachGenie is a deterministic, state-driven desktop application for LinkedIn outreach automation. It survives context loss, operates locally on user machines, and uses Model Context Protocol (MCP) for tool orchestration.

**Core Philosophy**: The agent is a proposer, not an actor. All state, side effects, and guarantees are owned by the Controller.

## 🏗️ Architecture

### Technology Stack

**Backend**:
- .NET 10 (ASP.NET Core Web API)
- SQLite with EF Core (local-first database)
- SignalR (real-time UI updates)
- Serilog (structured logging)

**Frontend**:
- React 18 + TypeScript
- Vite (dev server & build tool)
- shadcn/ui components
- TanStack Query (server state)
- Tailwind CSS

**Agent Integration**:
- Model Context Protocol (MCP) - Tool orchestration
- OpenAI API (LLM provider, abstracted via ILlmProvider)
- Playwright MCP (LinkedIn browser automation)
- Desktop Commander MCP (CLI/filesystem operations)
- Fetch & Exa MCP (web search and fetching)

### Key Architectural Decisions

1. **State-Driven, Not Conversation-Driven**
   - Campaigns execute based on database state, not chat history
   - System must work even if LLM forgets everything or app restarts
   - All state externalized and reloadable

2. **Controller Authority Separation**
   - LLM proposes actions (non-authoritative)
   - Controller validates and executes (authoritative)
   - No state mutations by LLM

3. **Local-First Desktop App**
   - Runs on user's machine (not cloud SaaS)
   - SQLite database in user's AppData folder
   - Optional cloud sync for multi-device (post-MVP)

## 🚀 Setup & Development

### Prerequisites

- Node.js 18+ (for frontend)
- .NET 10 SDK (for backend)
- npx (for MCP servers)

### Installation

```bash
# Clone repository
git clone <YOUR_GIT_URL>
cd outreachgenie-app

# Install frontend dependencies
npm install

# Restore backend dependencies
cd server
dotnet restore
cd ..

# Run database migrations
cd server/OutreachGenie.Api
dotnet ef database update
cd ../..
```

### Environment Configuration

**Development Mode**: Create `.env` file in project root:

```env
# LLM Provider (OpenAI) - DEVELOPMENT ONLY
OPENAI_API_KEY=your_key_here

# Web Search (Exa) - DEVELOPMENT ONLY
EXA_API_KEY=your_key_here

# Agent Configuration
POLLING_INTERVAL_MS=60000
MAX_CONCURRENT_CAMPAIGNS=1
```

**Production Mode (End Users)**: All secrets stored securely in database:
- Users enter API keys through Settings UI
- Keys encrypted via OS-level security:
  - **Windows**: DPAPI (Data Protection API) - tied to user account
  - **macOS**: Keychain API - secure credential storage
- No plaintext secrets in files or database
- LinkedIn session cookies also encrypted and stored in database

**Secure Storage Architecture**:
- `UserSecret` entity stores encrypted credentials
- `LinkedInSession` entity stores encrypted cookies
- Encryption/decryption transparent to application
- Keys isolated per-user, per-machine

### MCP Server Configuration

MCP servers are called via `npx` for built-in servers:

```json
// mcp.json (auto-discovered)
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-playwright"]
    },
    "desktop-commander": {
      "command": "npx",
      "args": ["-y", "@wonderwhy-er/desktop-commander"]
    },
    "fetch": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch"]
    },
    "exa": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-exa"]
    }
  }
}
```

Custom MCP servers can be added with absolute paths.

**Desktop Commander Key Features** (End-User Focused):
- **Excel Operations**: Native .xlsx/.xls/.xlsm support - read, write, edit cells, search content
- **PDF Operations**: Read PDFs, create from markdown, modify existing PDFs
- **Python Execution**: Run Python scripts in-memory for instant data analysis (pandas, numpy, matplotlib)
- **File System**: Full file/directory operations with advanced search (vscode-ripgrep)
- **Terminal**: Execute commands, manage processes, interactive sessions (SSH, databases)
- **Data Analysis**: Analyze CSV/JSON/Excel files without saving temporary files

### Running the Application

**Development Mode (separate terminals)**:

```bash
# Terminal 1: Backend API
cd server/OutreachGenie.Api
dotnet run

# Terminal 2: Frontend dev server
npm run dev
```

**Production Mode (.NET serves React)**:

```bash
# Build frontend
npm run build

# Run backend (serves built React app)
cd server/OutreachGenie.Api
dotnet run --configuration Release
```

Access at: `http://localhost:5000`

## 🧪 Testing

```bash
# Backend tests
cd server
dotnet test

# Frontend tests
npm test

# E2E tests
npm run test:e2e

# Run all CI checks
npm run ci
```

**Test Coverage**:
- Backend: 96.4% (142 tests)
- Frontend: 89.4% (46 tests)
- E2E: 10 Playwright tests

## 📊 Code Metrics

```bash
# Calculate SLOC (Source Lines of Code)
npm run sloc

# Show detailed breakdown by file
npm run sloc -- --by-file

# Show breakdown by language and file
npm run sloc -- --by-lang
```

**Current SLOC**: ~14,000 lines
- TypeScript: ~6,700 lines (Frontend)
- C#: ~6,600 lines (Backend)
- Other: ~700 lines (Config, styles, etc.)

## 📋 Implementation Decisions (Ready to Code)

### MVP-0: Agent Background Service

**Decision Summary**:
- **Polling**: Every 60 seconds for Active campaigns
- **Concurrency**: 1 campaign at a time (MaxConcurrentCampaigns=1)
- **Error Handling**: 3 retries with exponential backoff
- **SignalR**: Emit CampaignStateChanged, TaskStatusChanged events

**LinkedIn Authentication**:
- **Flow**: User logs in manually in headed Playwright browser
- **Cookie Storage**: `%APPDATA%/OutreachGenie/browser-sessions/{campaign-id}/cookies.json`
- **Expiration**: Detect expired cookies, emit SignalR notification for re-login
- **Format**: JSON array of browser cookies with expiry timestamps

### MVP-4: Chat-to-LLM Integration

**Decision Summary**:
- **Storage**: Chat messages stored as Artifacts with `type="chat"`
- **Context**: Load campaign + last 10 chat artifacts for LLM context
- **Rate Limiting**: 10 messages per minute per user
- **Provider**: OpenAI only (no fallback for MVP)

### MCP Lifecycle

**Decision Summary**:
- **Singleton**: MCP servers shared across all campaigns
- **Discovery**: Built-in servers via `npx`, custom servers via `mcp.json`
- **Transport**: StdioMcpTransport with JSON-RPC 2.0

## 📁 Project Structure

```
outreachgenie-app/
├── src/                          # React frontend
│   ├── components/               # UI components
│   ├── pages/                    # Route pages
│   ├── lib/                      # Utilities
│   └── types/                    # TypeScript types
├── server/                       # .NET backend
│   ├── OutreachGenie.Api/        # Web API & SignalR
│   ├── OutreachGenie.Application/# Business logic & services
│   ├── OutreachGenie.Domain/     # Entities & interfaces
│   ├── OutreachGenie.Infrastructure/ # MCP, DB, repositories
│   └── OutreachGenie.Tests/      # Unit & integration tests
├── e2e/                          # Playwright E2E tests
├── history/                      # Design specs
│   ├── agent_specification_deterministic_desktop_outreach_agent.md
│   ├── agent_chat_output_specification.md
│   └── history.md                # Conversation log
├── TODO.md                       # Implementation roadmap
└── README.md                     # This file
```

## 🎯 Current Status

### ✅ Infrastructure Complete (100%)
- Domain models, repositories, state machine
- REST API with 49 passing tests
- SignalR real-time messaging
- MCP integration (4 servers)
- LLM abstraction layer
- React UI with all pages
- 198 tests with 90%+ coverage

### 🔴 Agent Processing (Ready for Implementation)
See [TODO.md](TODO.md) for detailed implementation plan:
- MVP-0: AgentHostedService (background campaign processor)
- MVP-4: Chat-to-LLM wiring (real AI conversations)
- Estimated: 10-15 hours to complete MVP

## 📖 Documentation

- **[TODO.md](TODO.md)** - Complete task list with priorities and estimates
- **[history/agent_specification_deterministic_desktop_outreach_agent.md](history/agent_specification_deterministic_desktop_outreach_agent.md)** - Deterministic agent architecture
- **[history/agent_chat_output_specification.md](history/agent_chat_output_specification.md)** - Chat system design
- **[history/history.md](history/history.md)** - Conversation log with all decisions
- **[.github/copilot-instructions.md](.github/copilot-instructions.md)** - Code quality standards

## 🔐 Security & Privacy

- **Local-First**: All data stored on user's machine
- **No Telemetry**: No analytics or tracking
- **User API Keys**: Users provide their own OpenAI/Exa keys
- **Secure Storage** (Post-MVP): Windows Credential Manager / macOS Keychain integration
- **Session Cookies**: LinkedIn cookies encrypted and stored locally

## 🛠️ Contributing

Follow coding standards in `.github/copilot-instructions.md`:
- Elegant Objects principles (immutability, no utility classes)
- DDD architecture (Domain/Application/Infrastructure)
- Test-first development (TDD)
- ≥90% test coverage required

## 📝 License

[Add your license here]
