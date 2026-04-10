# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy the solution and project files first to cache the 'restore' layer
COPY ["Data-Technologies-Groep-4.sln", "./"]
COPY ["WebShop.Api/WebShop.Api.csproj", "WebShop.Api/"]
COPY ["WebShop.Contracts/WebShop.Contracts.csproj", "WebShop.Contracts/"]

RUN dotnet restore

# 2. Copy everything else and build
COPY . .
WORKDIR "/src/WebShop.Api"
RUN dotnet publish "WebShop.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Ensure the app knows to look for static files in wwwroot
ENTRYPOINT ["dotnet", "WebShop.Api.dll"]