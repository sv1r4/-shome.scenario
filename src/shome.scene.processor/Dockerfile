FROM microsoft/dotnet:2.1.6-aspnetcore-runtime-stretch-slim-arm32v7 AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/shome.scene.processor/shome.scene.processor.csproj src/shome.scene.processor/
COPY src/shome.provider.yml/shome.provider.yml.csproj src/shome.provider.yml/
COPY src/shome.scene.provider/shome.scene.provider.csproj src/shome.scene.provider/
COPY src/shome.scene.core/shome.scene.core.csproj src/shome.scene.core/
RUN dotnet restore src/shome.scene.processor/shome.scene.processor.csproj
COPY . .
WORKDIR /src/src/shome.scene.processor
RUN dotnet build shome.scene.processor.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish shome.scene.processor.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "shome.scene.processor.dll"]
