# Build context is the repo root (this project references sibling projects
# under src/) -- invoke as `docker build -f src/Coach.GarminIngestion/Dockerfile .`
# from the repo root.

# --- build: dotnet publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY personal-coach.slnx .
COPY src/Coach.Domain/Coach.Domain.csproj src/Coach.Domain/
COPY src/Coach.Application/Coach.Application.csproj src/Coach.Application/
COPY src/Coach.Infrastructure/Coach.Infrastructure.csproj src/Coach.Infrastructure/
COPY src/Coach.Infrastructure.Garmin/Coach.Infrastructure.Garmin.csproj src/Coach.Infrastructure.Garmin/
COPY src/Coach.GarminIngestion/Coach.GarminIngestion.csproj src/Coach.GarminIngestion/
RUN dotnet restore src/Coach.GarminIngestion/Coach.GarminIngestion.csproj

COPY src/ src/
RUN dotnet publish src/Coach.GarminIngestion/Coach.GarminIngestion.csproj -c Release -o /app/publish

# --- final: runtime only (no ASP.NET -- this is a batch job, not a web server) ---
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Coach.GarminIngestion.dll"]
