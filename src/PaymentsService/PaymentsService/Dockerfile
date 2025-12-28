# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY AsyncShopPlatform.sln ./

# ВАЖНО: копируем csproj всех project references, которые могут участвовать в restore
COPY src/Common/Common/Common.csproj src/Common/Common/
COPY src/Contracts/Contracts/Contracts.csproj src/Contracts/Contracts/
COPY src/PaymentsService/PaymentsService/PaymentsService.csproj src/PaymentsService/PaymentsService/

RUN dotnet restore src/PaymentsService/PaymentsService/PaymentsService.csproj

COPY . .

WORKDIR /src/src/PaymentsService/PaymentsService
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Для контейнеров проще/надёжнее, чем ASPNETCORE_URLS
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish ./

# Non-root user через APP_UID (стандарт для .NET images)
USER $APP_UID
ENTRYPOINT ["dotnet", "PaymentsService.dll"]
