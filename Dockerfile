# -----------------------------
# Build stage
# -----------------------------
# Use the full .NET SDK image to restore, build, and publish the application.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the source into the build container.
COPY . .

# Restore NuGet packages.
RUN dotnet restore "WarLeague.Discord/WarLeague.Discord.csproj"

# Publish a Release build to a folder we'll copy into the runtime image.
RUN dotnet publish "WarLeague.Discord/WarLeague.Discord.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# -----------------------------
# Runtime stage
# -----------------------------
# Use the smaller runtime image since this is a Discord bot, not an ASP.NET app.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

# SkiaSharp depends on native Linux libraries that are not included in the
# minimal .NET runtime image. Without these, image generation fails with
# errors such as:
#   libfontconfig.so.1: cannot open shared object file
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        libfontconfig1 \
        libfreetype6 && \
    rm -rf /var/lib/apt/lists/*

# Set the working directory for the application.
WORKDIR /app

# Copy the published application from the build stage.
COPY --from=build /app/publish .

# Start the Discord bot.
ENTRYPOINT ["dotnet", "WarLeague.Discord.dll"]