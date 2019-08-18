FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["samples/ProductGrpcServer/ProductGrpcServer.csproj", "samples/ProductGrpcServer/"]
COPY ["samples/GrpcShared/GrpcShared.csproj", "samples/GrpcShared/"]
RUN dotnet restore "samples/ProductGrpcServer/ProductGrpcServer.csproj"
COPY . .
WORKDIR "/src/samples/ProductGrpcServer"
RUN dotnet build "ProductGrpcServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ProductGrpcServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ProductGrpcServer.dll"]