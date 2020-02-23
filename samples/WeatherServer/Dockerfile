FROM mcr.microsoft.com/dotnet/core/sdk:3.1.101-alpine3.10 AS builder

ENV GLIBC_REPO=https://github.com/sgerrand/alpine-pkg-glibc
ENV GLIBC_VERSION=2.30-r0

RUN set -ex && \
    apk --update add libstdc++ curl ca-certificates && \
    for pkg in glibc-${GLIBC_VERSION} glibc-bin-${GLIBC_VERSION}; \
        do curl -sSL ${GLIBC_REPO}/releases/download/${GLIBC_VERSION}/${pkg}.apk -o /tmp/${pkg}.apk; done && \
    apk add --allow-untrusted /tmp/*.apk && \
    rm -v /tmp/*.apk && \
    /usr/glibc-compat/sbin/ldconfig /lib /usr/glibc-compat/lib

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS builder
WORKDIR /src
COPY ["samples/WeatherServer/WeatherServer.csproj", "samples/WeatherServer/"]
RUN dotnet restore "samples/WeatherServer/WeatherServer.csproj"
COPY . .
WORKDIR "/src/samples/WeatherServer"
RUN dotnet build "WeatherServer.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish "WeatherServer.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.1-alpine3.10
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherServer.dll"]