# syntax=docker/dockerfile:1

# --------------------
# Build
# --------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Для кэша restore (учитываем Central Package Management)
COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY AsyncShopPlatform.sln ./

COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
RUN dotnet restore src/ApiGateway/ApiGateway.csproj

COPY . .
WORKDIR /src/src/ApiGateway
RUN dotnet publish -c Release -o /app/publish --no-restore

# --------------------
# Runtime
# --------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

# Начиная с .NET 8 образы содержат non-root пользователя `app`,
# UID доступен в переменной $APP_UID — можно включить одной строкой.
USER $APP_UID

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
