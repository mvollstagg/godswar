FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY src/GodsWar.Database/GodsWar.Database.csproj src/GodsWar.Database/
COPY src/GodsWar.Protocol/GodsWar.Protocol.csproj src/GodsWar.Protocol/
COPY src/GodsWar.GameServer/GodsWar.GameServer.csproj src/GodsWar.GameServer/
RUN dotnet restore src/GodsWar.GameServer/GodsWar.GameServer.csproj

COPY src/GodsWar.Database/ src/GodsWar.Database/
COPY src/GodsWar.Protocol/ src/GodsWar.Protocol/
COPY src/GodsWar.GameServer/ src/GodsWar.GameServer/
RUN dotnet publish src/GodsWar.GameServer/GodsWar.GameServer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 6001
ENTRYPOINT ["dotnet", "GodsWar.GameServer.dll"]
