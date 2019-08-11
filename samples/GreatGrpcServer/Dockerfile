FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["samples/GreatGrpcServer/GreatGrpcServer.csproj", "samples/GreatGrpcServer/"]
COPY ["samples/GrpcShared/GrpcShared.csproj", "samples/GrpcShared/"]
RUN dotnet restore "samples/GreatGrpcServer/GreatGrpcServer.csproj"
COPY . .
WORKDIR "/src/samples/GreatGrpcServer"
RUN dotnet build "GreatGrpcServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "GreatGrpcServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GreatGrpcServer.dll"]