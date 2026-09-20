# syntax=docker/dockerfile:1
# Bab 1 — Application Host runtime: multi-stage image memisahkan SDK build dari runtime API.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["AspNetCoreDomainLab.csproj", "."]
RUN dotnet restore "AspNetCoreDomainLab.csproj"

COPY . .
RUN dotnet build "AspNetCoreDomainLab.csproj" \
    --no-restore \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AspNetCoreDomainLab.csproj" \
    --no-restore \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    /p:UseAppHost=false

# Bab 1 — final host hanya membawa artifact publish dan berjalan sebagai non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG APP_UID=1654
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=publish /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "AspNetCoreDomainLab.dll"]
