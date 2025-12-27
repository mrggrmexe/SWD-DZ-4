# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY AsyncShopPlatform.sln ./

# ВАЖНО: двойные пути
COPY src/Contracts/Contracts/Contracts.csproj src/Contracts/Contracts/
COPY src/OrdersService/OrdersService/OrdersService.csproj src/OrdersService/OrdersService/
RUN dotnet restore src/OrdersService/OrdersService/OrdersService.csproj

COPY . .
WORKDIR /src/src/OrdersService/OrdersService
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "OrdersService.dll"]
