# __PROJECT__ API image: the WebApi host from one build.
#
# Build context: the consumer root (the filled starter).
# Plain docker build, run from the consumer root (BuildKit, the Docker default):
#   docker build -f apps/api/Dockerfile --target api .
# The local-packages feed the gate populates carries LiteSpec.Analyzers;
# every other package comes from nuget.org.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY local-packages ./local-packages
COPY apps/api ./apps/api
WORKDIR /src/apps/api
RUN dotnet restore __NAMESPACE__.slnx
RUN dotnet publish src/Hosts/__NAMESPACE__.WebApi/__NAMESPACE__.WebApi.csproj --no-restore --configuration Release --output /out/api

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:8080
WORKDIR /app
COPY --from=build --chown=app:app /out/api ./
USER app
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "__NAMESPACE__.WebApi.dll"]
