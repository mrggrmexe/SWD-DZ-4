# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY AsyncShopPlatform.sln ./

# Contracts здесь НЕ обязателен, если gateway не ссылается на него,
# но оставляем на будущее/унификацию (без вреда)
COPY src/Contracts/Contracts/Contracts.csproj src/Contracts/Contracts/
COPY src/ApiGateway/ApiGateway/ApiGateway.csproj src/ApiGateway/ApiGateway/

RUN dotnet restore src/ApiGateway/ApiGateway/ApiGateway.csproj

COPY . .
WORKDIR /src/src/ApiGateway/ApiGateway
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
