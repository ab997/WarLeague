FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WarLeague.Discord/WarLeague.Discord.csproj", "WarLeague.Discord/"]
RUN dotnet restore "WarLeague.Discord/WarLeague.Discord.csproj"

COPY . .

WORKDIR "/src/WarLeague.Discord"
RUN dotnet publish "WarLeague.Discord.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WarLeague.Discord.dll"]