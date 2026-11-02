# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/Infrastructure/Common/NET-Tutos.BuildingBlocks.Common.csproj", "src/Infrastructure/Common/"]
COPY ["src/Infrastructure/EventBus/NET-Tutos.BuildingBlocks.EventBus.csproj", "src/Infrastructure/EventBus/"]
COPY ["src/Services/Gamification/NET-Tutos.Gamification.Domain/NET-Tutos.Gamification.Domain.csproj", "src/Services/Gamification/NET-Tutos.Gamification.Domain/"]
COPY ["src/Services/Gamification/NET-Tutos.Gamification.Application/NET-Tutos.Gamification.Application.csproj", "src/Services/Gamification/NET-Tutos.Gamification.Application/"]
COPY ["src/Services/Gamification/NET-Tutos.Gamification.Infrastructure/NET-Tutos.Gamification.Infrastructure.csproj", "src/Services/Gamification/NET-Tutos.Gamification.Infrastructure/"]
COPY ["src/Services/Gamification/NET-Tutos.Gamification.API/NET-Tutos.Gamification.API.csproj", "src/Services/Gamification/NET-Tutos.Gamification.API/"]

RUN dotnet restore "src/Services/Gamification/NET-Tutos.Gamification.API/NET-Tutos.Gamification.API.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/Services/Gamification/NET-Tutos.Gamification.API"
RUN dotnet publish "NET-Tutos.Gamification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NET-Tutos.Gamification.API.dll"]
