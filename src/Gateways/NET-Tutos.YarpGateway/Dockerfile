# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/Infrastructure/Common/NET-Tutos.BuildingBlocks.Common.csproj", "src/Infrastructure/Common/"]
COPY ["src/Gateways/NET-Tutos.YarpGateway/NET-Tutos.YarpGateway.csproj", "src/Gateways/NET-Tutos.YarpGateway/"]

RUN dotnet restore "src/Gateways/NET-Tutos.YarpGateway/NET-Tutos.YarpGateway.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/Gateways/NET-Tutos.YarpGateway"
RUN dotnet publish "NET-Tutos.YarpGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NET-Tutos.YarpGateway.dll"]
