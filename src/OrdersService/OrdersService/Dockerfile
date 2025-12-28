# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./

# Contracts (внутри него Common/)
COPY src/Contracts/Contracts/Contracts.csproj src/Contracts/Contracts/
COPY src/OrdersService/OrdersService/OrdersService.csproj src/OrdersService/OrdersService/

RUN dotnet restore src/OrdersService/OrdersService/OrdersService.csproj

COPY . .

WORKDIR /src/src/OrdersService/OrdersService
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "OrdersService.dll"]
