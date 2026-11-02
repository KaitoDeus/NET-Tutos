# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/Infrastructure/Common/NET-Tutos.BuildingBlocks.Common.csproj", "src/Infrastructure/Common/"]
COPY ["src/Services/CodeRunner/NET-Tutos.CodeRunner.Domain/NET-Tutos.CodeRunner.Domain.csproj", "src/Services/CodeRunner/NET-Tutos.CodeRunner.Domain/"]
COPY ["src/Services/CodeRunner/NET-Tutos.CodeRunner.Application/NET-Tutos.CodeRunner.Application.csproj", "src/Services/CodeRunner/NET-Tutos.CodeRunner.Application/"]
COPY ["src/Services/CodeRunner/NET-Tutos.CodeRunner.Infrastructure/NET-Tutos.CodeRunner.Infrastructure.csproj", "src/Services/CodeRunner/NET-Tutos.CodeRunner.Infrastructure/"]
COPY ["src/Services/CodeRunner/NET-Tutos.CodeRunner.API/NET-Tutos.CodeRunner.API.csproj", "src/Services/CodeRunner/NET-Tutos.CodeRunner.API/"]

RUN dotnet restore "src/Services/CodeRunner/NET-Tutos.CodeRunner.API/NET-Tutos.CodeRunner.API.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/Services/CodeRunner/NET-Tutos.CodeRunner.API"
RUN dotnet publish "NET-Tutos.CodeRunner.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NET-Tutos.CodeRunner.API.dll"]
