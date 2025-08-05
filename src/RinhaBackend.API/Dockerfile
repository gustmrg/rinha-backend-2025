# Use a minimal runtime image for AOT applications
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage with full SDK and native build tools
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install native build tools required for AOT compilation
RUN apt-get update && apt-get install -y \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["RinhaBackend.API.csproj", "."]
RUN dotnet restore "RinhaBackend.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "RinhaBackend.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

# Publish with AOT compilation
RUN dotnet publish "RinhaBackend.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --self-contained true \
    -p:PublishAot=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# For AOT builds, the executable name matches your project name (without .dll)
ENTRYPOINT ["./RinhaBackend.API"]