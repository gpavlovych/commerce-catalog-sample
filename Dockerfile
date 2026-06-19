# Multi-stage build. The runtime image starts the API in demo mode (SQLite + in-memory cache + in-process
# messaging) so "docker run" gives a working catalog with the UI, API, and live feed and no other services.
# Production overrides the Provider settings via environment variables to use Azure SQL, Redis, and Service Bus.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY Commerce.sln ./
COPY src ./src
COPY frontend ./frontend
RUN dotnet restore src/Commerce.Api/Commerce.Api.csproj
RUN dotnet publish src/Commerce.Api/Commerce.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Database__Provider=Sqlite \
    ConnectionStrings__Commerce="Data Source=/app/App_Data/commerce-demo.db" \
    Cache__Provider=InMemory \
    Messaging__Provider=InProcess

EXPOSE 8080
ENTRYPOINT ["dotnet", "Commerce.Api.dll"]
