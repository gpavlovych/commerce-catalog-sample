# Deployment

The production target is Azure Container Apps, with Azure SQL, Azure Cache for Redis, Azure Service Bus, and
Azure SignalR Service. Two lighter alternatives are included: a free Render path for a quick public URL, and a
GitHub Pages path for the static console.

## 1. Provision Azure resources

```bash
az group create -n commerce-rg -l westeurope

az deployment group create \
  -g commerce-rg \
  -f infra/main.bicep \
  -p sqlAdminPassword='<a-strong-password>' \
     containerImage='ghcr.io/gpavlovych/commerce-catalog-sample:latest'
```

The Bicep template creates the Container App and its dependencies, wires connection strings in as secrets, and
enables duplicate detection on the Service Bus topic plus a `forecasting` subscription. It outputs the public
API URL.

RediSearch note: the index requires Azure Cache for Redis in the Enterprise tier or self hosted Redis Stack.
The Standard tier in the template covers the cache aside path; switch to Enterprise to run the index in Azure.

## 2. Configure GitHub for OIDC deploys

Create a federated credential on an Azure AD app registration for this repository, then add:

Secrets

* `AZURE_CLIENT_ID`
* `AZURE_TENANT_ID`
* `AZURE_SUBSCRIPTION_ID`
* `CODECOV_TOKEN` (optional, for coverage upload)

Variables

* `AZURE_RESOURCE_GROUP` (for example `commerce-rg`)
* `AZURE_CONTAINERAPP_NAME` (for example `commerce-api`)

## 3. Deploy

Push to `main`, or run the Deploy to Azure workflow manually. It builds the image, pushes it to GitHub
Container Registry, and updates the Container App to the new image. Make the GHCR package public, or configure
the Container App with registry credentials, so Azure can pull it.

Update the live demo badge and link in the README with the URL the deployment outputs.

## Alternatives

* Render: connect the repo, point at `render.yaml`, and you get a free public URL running the demo image in
  SQLite mode. Useful when you do not want to spend on Azure.
* GitHub Pages: the Deploy console workflow publishes `frontend/` to Pages. Because Pages cannot host the API,
  set the API base box on the page to your deployed API URL.
