FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build

WORKDIR /src
COPY ./shared/netcore-shared/shared/shared.csproj ./shared/netcore-shared/shared/shared.csproj 
COPY ./client/client.csproj ./client/client.csproj
RUN dotnet restore  ./client/client.csproj

COPY ./client ./client
COPY ./shared/netcore-shared/shared ./shared/netcore-shared/shared

FROM build AS publish
RUN dotnet publish ./client/client.csproj -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY ./client/nginx.conf /etc/nginx/nginx.conf