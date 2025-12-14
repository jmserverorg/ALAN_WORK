# Docker Deployment Guide

## Prerequisites

1. Docker and Docker Compose installed
2. Azure CLI installed and authenticated (`az login`)
3. Copy `.env.example` to `.env` and configure:
   - `AZURE_OPENAI_ENDPOINT` - Your Azure OpenAI endpoint
   - `AZURE_OPENAI_DEPLOYMENT` - Your deployment name (default: gpt-4o-mini)
   - `GITHUB_MCP_PAT` - (Optional) GitHub PAT for MCP integration

**Note**: API keys are NOT required. The containers use your Azure CLI credentials from `az login`.

## Quick Start

```bash
# Ensure you're logged in with Azure CLI
az login

# Build and start all services
docker compose up --build

# Start in detached mode
docker compose up -d

# View logs
docker compose logs -f

# Stop all services
docker compose down
```

## Services

| Service     | Port | Description                          |
|-------------|------|--------------------------------------|
| azurite     | 10000-10002 | Local Azure Storage emulator  |
| chatapi     | 5041 | ASP.NET Core Web API backend        |
| alanagent   | -    | Background autonomous agent          |
| alan-web    | 5269 | Next.js frontend                     |

## Access URLs

- **Web Interface**: http://localhost:5269
- **ChatApi**: http://localhost:5041
- **Azurite Blob**: http://localhost:10000

## Architecture

```
┌─────────────┐
│  alan-web   │ :5269 (Next.js Frontend)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  chatapi    │ :5041 (ASP.NET Core API)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  azurite    │ :10000-10002 (Storage)
└──────┬──────┘
       ▲
       │
┌──────┴──────┐
│ alanagent   │ (Background Agent)
└─────────────┘
```

## Troubleshooting

### Azure Authentication Issues

The containers use your local Azure CLI credentials. Ensure:
```bash
# Check you're logged in
az account show

# Re-login if needed
az login

# Verify OpenAI access
az cognitiveservices account show --name <your-openai-resource> --resource-group <your-rg>
```

### Azurite Connection Issues

If services cannot connect to Azurite:
```bash
# Check Azurite is healthy
docker compose ps

# View Azurite logs
docker compose logs azurite
```

### MCP Server Issues

The agent requires `mcp-config.yaml` which is mounted from `src/ALAN.Agent/mcp-config.yaml`. Ensure this file exists.

### Port Conflicts

If ports are already in use:
```bash
# Stop any existing services
docker compose down

# Check what's using the ports
lsof -i :5041
lsof -i :5269
lsof -i :10000
```

## Data Persistence

Azurite data is persisted in a Docker volume `azurite-data`. To reset:
```bash
docker compose down -v
```

## Production Considerations

For production deployment:
1. Replace Azurite with Azure Storage Account
2. Use managed identities instead of API keys
3. Configure proper CORS and security headers
4. Enable HTTPS
5. Set up monitoring and health checks
6. Consider container orchestration (Kubernetes, Azure Container Apps)
