FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY Directory.Packages.props ./
COPY AsyncShopPlatform/AsyncShopPlatform.sln ./

# ВАЖНО: Contracts.csproj содержит Common
COPY src/Contracts/Contracts/Contracts.csproj src/Contracts/Contracts/
COPY src/OrdersService/OrdersService/OrdersService.csproj src/OrdersService/OrdersService/

RUN dotnet restore src/OrdersService/OrdersService/OrdersService.csproj

COPY . .

WORKDIR /src/src/OrdersService/OrdersService
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "OrdersService.dll"]
