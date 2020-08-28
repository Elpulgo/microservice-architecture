FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS base

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

WORKDIR /src
COPY "api-gateway.csproj" .
RUN dotnet restore "api-gateway.csproj"

COPY . .
RUN dotnet publish "api-gateway.csproj" --runtime linux-musl-x64 -c $BUILDCONFIG -o out /p:Version=$VERSION -p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine
WORKDIR /app
COPY --from=base /src/out ./

EXPOSE 80
ENTRYPOINT ["./api-gateway"]