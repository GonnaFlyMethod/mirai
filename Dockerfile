# syntax=docker/dockerfile:1

FROM node:22-alpine AS frontend
WORKDIR /src/frontend

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY api/api/api.csproj api/api/
RUN dotnet restore api/api/api.csproj

COPY api/api/ api/api/
COPY --from=frontend /src/frontend/dist /src/api/api/wwwroot

RUN dotnet publish api/api/api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

# Refresh the package index. Acquire::Check-Date=false only relaxes apt's
# freshness-timing check, needed because the security mirror has been
# intermittently serving a Release file timestamped slightly in the future;
# GPG signature verification (the check that actually matters) is untouched,
# and every configured suite, security included, is still fetched normally.
# Install the real Leptonica/Tesseract shared libraries -- the Tesseract
# NuGet package only ships Windows natives, nothing for Linux.
# Add a compatibility symlink: glibc folded libdl into libc and kept only
# the versioned libdl.so.2, while .NET's P/Invoke looks for plain "libdl.so".
# Remove the downloaded package index afterwards -- it's only needed during
# install, not at runtime -- to keep this layer small.
RUN apt-get update -o Acquire::Check-Date=false \
    && apt-get install -y --no-install-recommends liblept5 libtesseract5 \
    && ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# InteropDotNet's Linux loader looks for native libs next to the app in an
# x64/ folder (mirroring the Windows x64/*.dll layout the NuGet package
# ships), not in system library paths, so the symlinks have to live there.
RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 x64/libleptonica-1.82.0.so \
    && ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 x64/libtesseract50.so

EXPOSE 8080

ENTRYPOINT ["dotnet", "api.dll"]
