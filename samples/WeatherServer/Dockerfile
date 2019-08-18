FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["samples/WeatherServer/WeatherServer.csproj", "samples/WeatherServer/"]
RUN dotnet restore "samples/WeatherServer/WeatherServer.csproj"
COPY . .
WORKDIR "/src/samples/WeatherServer"
RUN dotnet build "WeatherServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeatherServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherServer.dll"]