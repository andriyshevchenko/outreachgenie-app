# OutreachGenie Configuration Guide

## Azure OpenAI Setup

The application requires Azure OpenAI to power the AI agent. Follow these steps to configure it:

### 1. Create Azure OpenAI Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Create a new Azure OpenAI resource
3. Deploy a GPT-4o model (or another compatible model)
4. Note your endpoint URL (e.g., `https://YOUR-RESOURCE.openai.azure.com`)

### 2. Configure Authentication

The application uses Azure DefaultAzureCredential, which supports multiple authentication methods:

#### Option A: Azure CLI (Recommended for Development)
```bash
az login
```

#### Option B: Environment Variables
```bash
# Windows PowerShell
$env:AZURE_TENANT_ID="your-tenant-id"
$env:AZURE_CLIENT_ID="your-client-id"
$env:AZURE_CLIENT_SECRET="your-client-secret"

# Linux/macOS
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
```

#### Option C: Managed Identity (For Production/Azure Hosting)
No configuration needed - just assign the App Service Managed Identity to the Azure OpenAI resource.

### 3. Configure Endpoint

Edit `backend/OutreachGenie.Api/appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-AZURE-OPENAI-RESOURCE.openai.azure.com",
    "DeploymentName": "gpt-4o"
  }
}
```

Or set environment variable:
```bash
# Windows PowerShell
$env:AZURE_OPENAI_ENDPOINT="https://YOUR-RESOURCE.openai.azure.com"

# Linux/macOS
export AZURE_OPENAI_ENDPOINT="https://YOUR-RESOURCE.openai.azure.com"
```

### 4. Grant Permissions

Ensure your authenticated identity has the **Cognitive Services OpenAI User** role on the Azure OpenAI resource:

```bash
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee YOUR_USER_PRINCIPAL_ID \
  --scope /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/YOUR_RG/providers/Microsoft.CognitiveServices/accounts/YOUR_OPENAI_RESOURCE
```

## Running the Application

### Backend
```bash
cd backend/OutreachGenie.Api
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: http://localhost:5000/swagger
- AG-UI Endpoint: http://localhost:5000/api/agent

### Frontend
```bash
cd frontend
npm install  # First time only
npm run dev
```

The frontend will be available at http://localhost:5173

## Testing the Integration

1. Start the backend (dotnet run)
2. Start the frontend (npm run dev)
3. Open http://localhost:5173 in your browser
4. Send a message like: "Create a LinkedIn outreach campaign for SaaS companies"
5. Watch the agent stream its response token-by-token and use tools (CreateTask, DiscoverLeads, etc.)

## Troubleshooting

### "Azure OpenAI endpoint not configured"
- Check that appsettings.Development.json has a valid endpoint URL
- Or set AZURE_OPENAI_ENDPOINT environment variable
- Don't use an empty string - either provide a valid URL or remove the setting entirely

### "Authentication failed"
- Ensure you're logged in: `az login`
- Check that your user has the "Cognitive Services OpenAI User" role
- Verify the deployment name matches your Azure OpenAI model deployment

### "Deployment not found"
- Check that DeploymentName in appsettings matches your Azure OpenAI model deployment name exactly
- Or set AZURE_OPENAI_DEPLOYMENT environment variable

### Frontend can't connect to backend
- Ensure backend is running on port 5000
- Check browser console for CORS errors
- Verify AG-UI endpoint is accessible: http://localhost:5000/api/agent

### Database errors
- The SQLite database is created automatically on first run
- To reset: delete `backend/OutreachGenie.Api/outreachgenie.db` and restart
- Migrations are applied automatically on startup
