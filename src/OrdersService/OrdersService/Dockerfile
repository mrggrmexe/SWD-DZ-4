# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY AsyncShopPlatform.sln ./

# Копируем csproj всех project references, нужных для restore
COPY src/Common/Common/Common.csproj src/Common/Common/
COPY src/Contracts/Contracts/Contracts.csproj src/Contracts/Contracts/
COPY src/OrdersService/OrdersService/OrdersService.csproj src/OrdersService/OrdersService/

RUN dotnet restore src/OrdersService/OrdersService/OrdersService.csproj

COPY . .

WORKDIR /src/src/OrdersService/OrdersService
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# В .NET контейнерах дефолт 8080; ASPNETCORE_HTTP_PORTS проще, чем ASPNETCORE_URLS
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish ./

# Non-root: APP_UID задаётся базовым образом
USER $APP_UID
ENTRYPOINT ["dotnet", "OrdersService.dll"]
