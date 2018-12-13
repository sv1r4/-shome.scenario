#base for visual studio run only
FROM microsoft/dotnet:2.2.0-aspnetcore-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk-alpine AS build
WORKDIR /src
COPY src/shome.scene.processor/shome.scene.processor.csproj src/shome.scene.processor/
COPY src/shome.scene.provider.yml/shome.scene.provider.yml.csproj src/shome.scene.provider.yml/
COPY src/shome.scene.akka/shome.scene.akka.csproj src/shome.scene.akka/
COPY src/shome.scene.core/shome.scene.core.csproj src/shome.scene.core/
COPY src/shome.scene.mqtt.core/shome.scene.mqtt.core.csproj src/shome.scene.mqtt.core/
COPY src/shome.scene.mqtt.mqttnet/shome.scene.mqtt.mqttnet.csproj src/shome.scene.mqtt.mqttnet/
RUN dotnet restore src/shome.scene.processor/shome.scene.processor.csproj
COPY . .
WORKDIR /src/src/shome.scene.processor
RUN dotnet build shome.scene.processor.csproj -c Release -o /app
 

FROM build AS publish
RUN dotnet test /src/test/shome.provider.yml.unittest/shome.provider.yml.unittest.csproj -c Release && \
  dotnet test /src/test/shome.scene.akka.unittest/shome.scene.akka.unittest.csproj -c Release && \
  dotnet publish shome.scene.processor.csproj -c Release -o /app

#arm32v7 runtime for rpi
FROM microsoft/dotnet:2.2.0-aspnetcore-runtime-stretch-slim-arm32v7 AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "shome.scene.processor.dll"]
