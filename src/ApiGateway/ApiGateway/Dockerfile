# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Central package versions (если есть)
COPY Directory.Packages.props ./

# Копируем только csproj для restore (кэшируемо)
COPY src/ApiGateway/ApiGateway/ApiGateway.csproj src/ApiGateway/ApiGateway/

RUN dotnet restore src/ApiGateway/ApiGateway/ApiGateway.csproj

# Теперь копируем всё
COPY . .

WORKDIR /src/src/ApiGateway/ApiGateway
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
