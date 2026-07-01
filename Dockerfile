# syntax=docker/dockerfile:1

# Build the image (run from the repo root):
#   docker build -t couchpotato .

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so layers are cached when only source changes
COPY couchpotato.csproj ./
RUN dotnet restore couchpotato.csproj

# Copy the rest of the source and publish a framework-dependent build
COPY . ./
RUN dotnet publish couchpotato.csproj -c Release -o /app --no-restore

# ---- Runtime stage ----
# Console app (not a web app) so the plain runtime image is enough
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app ./

# The application is a one-off run: it processes the given settings and exits.
# Settings, plugins, output folders etc. are provided at runtime via volumes, e.g.:
#   docker run --rm \
#     -v /host/config:/config \
#     -v /host/plugins:/plugins \
#     -v /host/output:/output \
#     couchpotato /config/settings.json
ENTRYPOINT ["dotnet", "couchpotato.dll"]
