FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["samples/OcelotGateway/OcelotGateway.csproj", "samples/OcelotGateway/"]
COPY ["samples/GrpcShared/GrpcShared.csproj", "samples/GrpcShared/"]
COPY ["src/GrpcJsonTranscoder/GrpcJsonTranscoder.csproj", "src/GrpcJsonTranscoder/"]
RUN dotnet restore "samples/OcelotGateway/OcelotGateway.csproj"
COPY . .
WORKDIR "/src/samples/OcelotGateway"
RUN dotnet build "OcelotGateway.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "OcelotGateway.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
#todo: override configuration.json to ocelot.json 
ENTRYPOINT ["dotnet", "OcelotGateway.dll"]