# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a .csproj changes.
COPY Portfolio.sln ./
COPY src/Portfolio.Api/Portfolio.Api.csproj src/Portfolio.Api/
COPY tests/Portfolio.Api.Tests/Portfolio.Api.Tests.csproj tests/Portfolio.Api.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Portfolio.Api/Portfolio.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the non-root user that ships with the .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
