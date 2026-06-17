# syntax=docker/dockerfile:1

FROM node:22-alpine AS frontend
WORKDIR /src/frontend

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY api/api/api.csproj api/api/
RUN dotnet restore api/api/api.csproj

COPY api/api/ api/api/
COPY --from=frontend /src/frontend/dist /src/api/api/wwwroot

RUN dotnet publish api/api/api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "api.dll"]
