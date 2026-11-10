# syntax=docker/dockerfile:1

# Build on the runner's native architecture. The output is portable IL, so it runs on any
# architecture the runtime image supports without emulating the build.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until dependencies change.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/RomaniaPulse.Web/RomaniaPulse.Web.csproj src/RomaniaPulse.Web/packages.lock.json src/RomaniaPulse.Web/
RUN dotnet restore src/RomaniaPulse.Web --locked-mode

COPY src/RomaniaPulse.Web/ src/RomaniaPulse.Web/
RUN dotnet publish src/RomaniaPulse.Web -c Release --no-restore -p:UseAppHost=false -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Runs as the image's built-in non-root user; only the snapshot cache is writable.
RUN mkdir -p /app/App_Data && chown $APP_UID /app/App_Data
VOLUME /app/App_Data
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RomaniaPulse.Web.dll"]
