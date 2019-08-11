FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["samples/ProductCatalogGrpcServer/ProductCatalogGrpcServer.csproj", "samples/ProductCatalogGrpcServer/"]
COPY ["samples/GrpcShared/GrpcShared.csproj", "samples/GrpcShared/"]
RUN dotnet restore "samples/ProductCatalogGrpcServer/ProductCatalogGrpcServer.csproj"
COPY . .
WORKDIR "/src/samples/ProductCatalogGrpcServer"
RUN dotnet build "ProductCatalogGrpcServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ProductCatalogGrpcServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ProductCatalogGrpcServer.dll"]