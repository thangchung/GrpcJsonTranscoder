FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["samples/AggregationRestApi/AggregationRestApi.csproj", "samples/AggregationRestApi/"]
COPY ["src/GrpcJsonTranscoder/GrpcJsonTranscoder.csproj", "src/GrpcJsonTranscoder/"]
COPY ["samples/GrpcShared/GrpcShared.csproj", "samples/GrpcShared/"]
RUN dotnet restore "samples/AggregationRestApi/AggregationRestApi.csproj"
COPY . .
WORKDIR "samples/AggregationRestApi/"
RUN dotnet build "AggregationRestApi.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AggregationRestApi.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AggregationRestApi.dll"]