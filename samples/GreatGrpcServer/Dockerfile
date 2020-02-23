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

WORKDIR /src
COPY ["samples/GreatGrpcServer/GreatGrpcServer.csproj", "samples/GreatGrpcServer/"]
COPY ["samples/GrpcShared/GrpcShared.csproj", "samples/GrpcShared/"]
RUN dotnet restore "samples/GreatGrpcServer/GreatGrpcServer.csproj"
COPY . .
WORKDIR "/src/samples/GreatGrpcServer"
RUN dotnet build "GreatGrpcServer.csproj" -c Release -o /app

FROM builder AS publish
RUN dotnet publish "GreatGrpcServer.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.1-alpine3.10
WORKDIR /app
EXPOSE 80
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "GreatGrpcServer.dll"]