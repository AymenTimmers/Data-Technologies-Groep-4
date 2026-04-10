FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy EVERYTHING (IMPORTANT)
COPY . .

# Restore solution or API project
RUN dotnet restore WebShop.Api/WebShop.Api.csproj

RUN dotnet publish WebShop.Api/WebShop.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebShop.Api.dll"]