FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["APIGateway/APIGateway.csproj", "APIGateway/"]
COPY ["ProximityService/Protos/proximity.proto", "ProximityService/Protos/"]
COPY ["BusinessService/Protos/business.proto", "BusinessService/Protos/"]
RUN dotnet restore "APIGateway/APIGateway.csproj"
COPY . .
WORKDIR "/src/APIGateway"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "APIGateway.dll"]
