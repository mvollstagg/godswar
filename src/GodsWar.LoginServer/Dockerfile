FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY src/GodsWar.Database/GodsWar.Database.csproj src/GodsWar.Database/
COPY src/GodsWar.Protocol/GodsWar.Protocol.csproj src/GodsWar.Protocol/
COPY src/GodsWar.LoginServer/GodsWar.LoginServer.csproj src/GodsWar.LoginServer/
RUN dotnet restore src/GodsWar.LoginServer/GodsWar.LoginServer.csproj

COPY src/GodsWar.Database/ src/GodsWar.Database/
COPY src/GodsWar.Protocol/ src/GodsWar.Protocol/
COPY src/GodsWar.LoginServer/ src/GodsWar.LoginServer/
RUN dotnet publish src/GodsWar.LoginServer/GodsWar.LoginServer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 5999
ENTRYPOINT ["dotnet", "GodsWar.LoginServer.dll"]
