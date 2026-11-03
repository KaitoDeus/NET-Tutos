# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/Infrastructure/Common/NET-Tutos.BuildingBlocks.Common.csproj", "src/Infrastructure/Common/"]
COPY ["src/Services/AiTutor/NET-Tutos.AiTutor.Domain/NET-Tutos.AiTutor.Domain.csproj", "src/Services/AiTutor/NET-Tutos.AiTutor.Domain/"]
COPY ["src/Services/AiTutor/NET-Tutos.AiTutor.Application/NET-Tutos.AiTutor.Application.csproj", "src/Services/AiTutor/NET-Tutos.AiTutor.Application/"]
COPY ["src/Services/AiTutor/NET-Tutos.AiTutor.Infrastructure/NET-Tutos.AiTutor.Infrastructure.csproj", "src/Services/AiTutor/NET-Tutos.AiTutor.Infrastructure/"]
COPY ["src/Services/AiTutor/NET-Tutos.AiTutor.API/NET-Tutos.AiTutor.API.csproj", "src/Services/AiTutor/NET-Tutos.AiTutor.API/"]

RUN dotnet restore "src/Services/AiTutor/NET-Tutos.AiTutor.API/NET-Tutos.AiTutor.API.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/Services/AiTutor/NET-Tutos.AiTutor.API"
RUN dotnet publish "NET-Tutos.AiTutor.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NET-Tutos.AiTutor.API.dll"]
