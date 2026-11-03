# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Infrastructure/Common/NET-Tutos.BuildingBlocks.Common.csproj", "src/Infrastructure/Common/"]
COPY ["src/Services/Catalog/NET-Tutos.Catalog.Domain/NET-Tutos.Catalog.Domain.csproj", "src/Services/Catalog/NET-Tutos.Catalog.Domain/"]
COPY ["src/Services/Catalog/NET-Tutos.Catalog.Application/NET-Tutos.Catalog.Application.csproj", "src/Services/Catalog/NET-Tutos.Catalog.Application/"]
COPY ["src/Services/Catalog/NET-Tutos.Catalog.Infrastructure/NET-Tutos.Catalog.Infrastructure.csproj", "src/Services/Catalog/NET-Tutos.Catalog.Infrastructure/"]
COPY ["src/Services/Catalog/NET-Tutos.Catalog.API/NET-Tutos.Catalog.API.csproj", "src/Services/Catalog/NET-Tutos.Catalog.API/"]

RUN dotnet restore "src/Services/Catalog/NET-Tutos.Catalog.API/NET-Tutos.Catalog.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Catalog/NET-Tutos.Catalog.API"
RUN dotnet publish "NET-Tutos.Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NET-Tutos.Catalog.API.dll"]
