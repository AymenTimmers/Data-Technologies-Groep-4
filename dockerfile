FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy EVERYTHING in the repo to the container
# This ensures the .sln finds all projects (Api, Contracts, Tests, etc.)
COPY . .

# 2. Restore using the solution file
RUN dotnet restore "WebShop.Api/WebShop.Api.csproj"

# 3. Build and Publish the Api project
WORKDIR "/src/WebShop.Api"
RUN dotnet publish "WebShop.Api.csproj" -c Release -o /app/publish --no-restore /-:UseAppHost=false

# 4. Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
# Copy the published output from the build stage
COPY --from=build /app/publish .

# The app will serve static files from wwwroot automatically if configured in Program.cs
ENTRYPOINT ["dotnet", "WebShop.Api.dll"]