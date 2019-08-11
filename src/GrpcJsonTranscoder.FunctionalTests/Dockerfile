FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["src/GrpcJsonTranscoder.FunctionalTests/GrpcJsonTranscoder.FunctionalTests.csproj", "src/GrpcJsonTranscoder.FunctionalTests/"]
RUN dotnet restore "src/GrpcJsonTranscoder.FunctionalTests/GrpcJsonTranscoder.FunctionalTests.csproj"
COPY . .
WORKDIR "/src/src/GrpcJsonTranscoder.FunctionalTests"
RUN dotnet build "GrpcJsonTranscoder.FunctionalTests.csproj" -c Release -o /app

FROM build as functional_tests
