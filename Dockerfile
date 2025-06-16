FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY MeCounter/*.csproj ./MeCounter/
COPY MeCounter.DataAccess.Postgres/*.csproj ./MeCounter.DataAccess.Postgres/
COPY MeCounter.sln ./
RUN dotnet restore
COPY MeCounter/ ./MeCounter/
COPY MeCounter.DataAccess.Postgres/ ./MeCounter.DataAccess.Postgres/
RUN dotnet publish MeCounter/MeCounter.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "MeCounter.dll"]