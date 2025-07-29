FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RinhaBackend.sln", "."]
COPY ["src/RinhaBackend.API/RinhaBackend.API.csproj", "src/RinhaBackend.API/"]
RUN dotnet restore "src/RinhaBackend.API/RinhaBackend.API.csproj"
COPY . .
WORKDIR "/src/src/RinhaBackend.API"
RUN dotnet build "RinhaBackend.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RinhaBackend.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaBackend.API.dll"]