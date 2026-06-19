# Finish in Claude Code

This repository was authored end to end but not compiled in the authoring environment (no .NET SDK and no
NuGet access there). Everything below runs on your machine, where the compiler and your credentials are. Open
the folder in Claude Code and work top to bottom.

## 0. Prerequisites

* .NET 10 SDK
* Docker Desktop (integration tests and the full stack)
* Git and a GitHub account
* PowerShell (`pwsh`) for the Playwright browser install
* Azure CLI, only if you deploy to Azure

## 1. Build and reconcile package versions

A few third party versions are best guesses (Scalar.AspNetCore, the Azure Functions worker extensions, and a
couple of Testcontainers and Playwright versions). Let the compiler tell you what to bump.

```bash
dotnet restore Commerce.sln
dotnet build Commerce.sln -c Release
dotnet format Commerce.sln
```

Paste this into Claude Code if anything fails:

> Build Commerce.sln. If a package version does not resolve, pick the nearest stable version that supports
> net10.0 and update the csproj. If an API signature changed (for example Scalar.AspNetCore or a Functions
> worker extension), adjust the call site to the current API. Then run dotnet format and dotnet build until
> the solution is clean. Do not change the architecture or the public behaviour.

## 2. Run the tests

```bash
dotnet test tests/Commerce.UnitTests
dotnet test tests/Commerce.IntegrationTests        # Docker must be running

dotnet build -c Release
pwsh tests/Commerce.UiTests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/Commerce.Api &
DEMO_BASE_URL=http://localhost:5080 dotnet test tests/Commerce.UiTests
```

## 3. See it run

```bash
docker build -t commerce-catalog .
docker run -p 8080:8080 commerce-catalog
# http://localhost:8080
```

## 4. Push to GitHub

The badges and deploy docs assume the repository `gpavlovych/commerce-catalog-sample`. If you use a different
name, update the badge URLs in `README.md` and the image and resource names in the workflows and docs.

```bash
git init
git add .
git commit -m "Commerce Catalog: clean architecture .NET 10 + Azure sample"
git branch -M main
git remote add origin https://github.com/gpavlovych/commerce-catalog-sample.git
git push -u origin main
```

CI runs on push. Once green, the CI and coverage badges resolve.

## 5. Deploy a live URL

Pick one.

### Azure (matches the production stack)

```bash
az group create -n commerce-rg -l westeurope
az deployment group create -g commerce-rg -f infra/main.bicep \
  -p sqlAdminPassword='<strong-password>' containerImage='ghcr.io/gpavlovych/commerce-catalog-sample:latest'
```

Then add the GitHub secrets and variables listed in `docs/deployment.md` (OIDC client, tenant, subscription;
resource group and container app name) and push to `main`. The Deploy to Azure workflow builds the image,
pushes it to GHCR, and rolls it out. Make the GHCR package public so Azure can pull it.

### Render (free, fastest public URL)

Connect the repo in Render, point at `render.yaml`, deploy. You get a public URL running the demo image.

## 6. Wire the live URL in

Replace `https://YOUR-APP.azurecontainerapps.io` in `README.md` (the live demo badge and the link near the top)
with the URL from step 5. Add a screenshot of the console at `docs/console.png` and reference it in the README
if you want the repo to look complete at a glance.

## 7. Optional: Codecov

Add the repo on codecov.io and set `CODECOV_TOKEN` as a GitHub Actions secret. The coverage badge then fills in.
