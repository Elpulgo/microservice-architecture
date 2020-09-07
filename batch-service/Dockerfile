FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS base

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

WORKDIR /src
COPY ./shared/netcore-shared/shared/shared.csproj ./shared/netcore-shared/shared/shared.csproj 
COPY ./batch-service/batch-webservice.csproj ./batch-service/batch-webservice.csproj
RUN dotnet restore ./batch-service/batch-webservice.csproj

COPY ./batch-service ./batch-service
COPY ./shared/netcore-shared/shared ./shared/netcore-shared/shared
RUN dotnet publish ./batch-service/batch-webservice.csproj --runtime linux-musl-x64 -c $BUILDCONFIG -o out /p:Version=$VERSION -p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine
WORKDIR /app
COPY --from=base /src/out ./

EXPOSE 8040
ENTRYPOINT ["./batch-webservice"]