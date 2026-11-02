# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project file and restore
COPY ["src/Presentation/Web/web.csproj", "src/Presentation/Web/"]
RUN dotnet restore "src/Presentation/Web/web.csproj"

# Copy all source files and publish
COPY src/Presentation/Web/ src/Presentation/Web/
WORKDIR "/src/src/Presentation/Web"
RUN dotnet publish "web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "web.dll"]
