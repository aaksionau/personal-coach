# --- assets: Tailwind CSS (standalone CLI, no Node.js) + vendored Alpine.js ---
FROM debian:bookworm-slim AS assets
WORKDIR /assets
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Pinned versions -- bump deliberately, not via a floating "latest" tag.
ARG TAILWIND_VERSION=v4.3.3
ARG ALPINE_JS_VERSION=3.16.3

RUN curl -sSLo tailwindcss "https://github.com/tailwindlabs/tailwindcss/releases/download/${TAILWIND_VERSION}/tailwindcss-linux-x64" \
    && chmod +x tailwindcss
RUN curl -sSLo alpine.min.js "https://cdn.jsdelivr.net/npm/alpinejs@${ALPINE_JS_VERSION}/dist/cdn.min.js"

COPY Styles/ Styles/
COPY Components/ Components/
RUN ./tailwindcss -i Styles/tailwind.css -o app.css --minify

# --- build: dotnet publish (picks up the generated assets into wwwroot before publish) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Coach.Web.csproj .
RUN dotnet restore Coach.Web.csproj

COPY . .
COPY --from=assets /assets/app.css wwwroot/css/app.css
COPY --from=assets /assets/alpine.min.js wwwroot/js/alpine.min.js
RUN dotnet publish Coach.Web.csproj -c Release -o /app/publish

# --- final: aspnet runtime only ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Coach.Web.dll"]
