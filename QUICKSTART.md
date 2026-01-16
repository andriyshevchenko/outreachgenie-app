# Quick Start Guide

## Choose Your AI Provider

You **do NOT need Azure**. Pick the easiest option for you:

### Option 1: OpenAI API (Recommended - Simplest!)

1. **Get API Key**: Visit https://platform.openai.com/api-keys
2. **Set it via environment variable**:
   ```powershell
   # Windows PowerShell
   $env:OPENAI_API_KEY="sk-proj-..."
   ```
   
   Or edit `backend/OutreachGenie.Api/appsettings.Development.json`:
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-proj-...",
       "Model": "gpt-4o"
     }
   }
   ```

3. **Done!** No authentication setup, no Azure account needed.

**Cost**: ~$0.01-0.05 per conversation with GPT-4o

### Option 2: Azure OpenAI

See [CONFIGURATION.md](CONFIGURATION.md) for Azure setup instructions.

### Option 3: Local Models (Free but slower)

Install Ollama and use models like Llama 3.1:
```bash
# Install Ollama from https://ollama.ai
ollama pull llama3.1

# Add package to backend
dotnet add package Microsoft.Extensions.AI.Ollama

# Update Program.cs to use Ollama client
```

## Running the App

### 1. Start Backend
```bash
cd backend/OutreachGenie.Api
dotnet run
```
✅ API at http://localhost:5000 (Swagger: `/swagger`)

### 2. Start Frontend  
```bash
cd frontend
npm install  # First time only
npm run dev
```
✅ UI at http://localhost:5173

### 3. Test It!
Open http://localhost:5173 and send:
> "Create a LinkedIn outreach campaign for SaaS companies"

Watch the agent stream responses and use tools in real-time! 🚀

## What You'll See

- **Token-by-token streaming**: Watch the AI think in real-time
- **Tool calls**: See when the agent uses `CreateTask`, `DiscoverLeads`, etc.
- **Task enforcement**: Agent cannot skip required tasks (enforced by middleware)
- **Database persistence**: All campaigns saved to SQLite

## Troubleshooting

### "No AI provider configured"
Set either `OPENAI_API_KEY` or `AZURE_OPENAI_ENDPOINT` environment variable.

### Frontend can't connect
Ensure backend is running on port 5000. Check http://localhost:5000/swagger

### Database errors
Delete `backend/OutreachGenie.Api/outreachgenie.db` and restart to reset.
