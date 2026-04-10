# Stage 1: Build the Frontend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# Copy everything
COPY . .

# Build the Frontend (if it requires a specific build step, do it here)
# For example, if you need to move the index.js bundle:
RUN mkdir -p WebShop.Api/wwwroot && cp WebShop.Web/index.js WebShop.Api/wwwroot/

# Stage 2: Build the API
RUN dotnet restore "WebShop.Api/WebShop.Api.csproj"
RUN dotnet publish "WebShop.Api/WebShop.Api.csproj" -c Release -o /app/publish

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/publish .
ENTRYPOINT ["dotnet", "WebShop.Api.dll"]