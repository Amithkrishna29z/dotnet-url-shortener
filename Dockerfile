# Multi-stage build. PROJECT selects which app to publish (web or worker) so the
# same Dockerfile builds both images via docker-compose build args.
ARG PROJECT=SnipLink.Web

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT
WORKDIR /src

# Restore first (better layer caching) using the solution + project files.
COPY SnipLink.sln .
COPY src/ src/
RUN dotnet restore "src/${PROJECT}/${PROJECT}.csproj"

RUN dotnet publish "src/${PROJECT}/${PROJECT}.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
ARG PROJECT
WORKDIR /app
# curl is used by the compose health check; not in the base image by default.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV APP_DLL=${PROJECT}.dll
COPY --from=build /app/publish .
EXPOSE 8080
# Shell form so $APP_DLL is expanded at runtime (web vs worker).
ENTRYPOINT ["sh", "-c", "dotnet $APP_DLL"]
