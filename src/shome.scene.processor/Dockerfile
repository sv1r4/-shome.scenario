#vase for visual studio run only
FROM microsoft/dotnet:2.2.0-aspnetcore-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk-alpine AS build
WORKDIR /src
COPY src/shome.scene.processor/shome.scene.processor.csproj src/shome.scene.processor/
COPY src/shome.provider.yml/shome.provider.yml.csproj src/shome.provider.yml/
COPY src/shome.scene.provider/shome.scene.provider.csproj src/shome.scene.provider/
COPY src/shome.scene.core/shome.scene.core.csproj src/shome.scene.core/
RUN dotnet restore src/shome.scene.processor/shome.scene.processor.csproj
COPY . .
WORKDIR /src/src/shome.scene.processor
RUN dotnet build shome.scene.processor.csproj -c Release -o /app && \
 dotnet test /src/test/shome.provider.yml.unittest/shome.provider.yml.unittest.csproj -c Release

FROM build AS publish
RUN dotnet publish shome.scene.processor.csproj -c Release -o /app

#arm32v7 runtime for rpi
FROM microsoft/dotnet:2.2.0-aspnetcore-runtime-stretch-slim-arm32v7 AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "shome.scene.processor.dll"]
