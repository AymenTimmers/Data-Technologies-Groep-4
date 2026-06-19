# WebShop API

## .NET Versions
- Webshop.Api = 8.0  
- Webshop.Api.Tests = 8.0  
- WebShop.Contracts = 8.0  

---

## Dependencies

### WebShop.Api

#### NuGet packages
- Dapper — 2.1.72  
- Microsoft.Data.Sqlite — 10.0.3  
- Neo4j.Driver — 6.1.2  
- StackExchange.Redis — 2.12.14  
- Swashbuckle.AspNetCore — 10.2.1  
- BCrypt.Net-Next — 4.0.3  
- System.IdentityModel.Tokens.Jwt — 7.4.0  
- Microsoft.AspNetCore.Authentication.JwtBearer — 8.0.0  

#### Project references
- WebShop.Contracts → `..\WebShop.Contracts\WebShop.Contracts.csproj`

---

### WebShop.Api.Tests

#### NuGet packages
- coverlet.collector — 6.0.0  
- Microsoft.NET.Test.Sdk — 17.8.0  
- xUnit — 2.5.3  
- xUnit Runner Visual Studio — 2.5.3  

#### Project references
- WebShop.Api → `..\WebShop.Api\WebShop.Api.csproj`

#### Global usings
- Xunit

---

## Database setup

- There are no separate scripts for running the databases.  
- The helper `DbBootstrapper.cs` handles initializing and loading data into the SQLite databases.  
- The `docker-compose.yml` creates containers for Redis, MongoDB, and Neo4j.  

---

## Configuration

- No `.env` file is needed.  
- Environment variables are configured in `appsettings.json`.  
- Dockerfile and `docker-compose.yml` are located in the root of the project.