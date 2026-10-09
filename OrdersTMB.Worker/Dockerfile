FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OrdersTMB.Worker/OrdersTMB.Worker.csproj", "OrdersTMB.Worker/"]
COPY ["OrdersTMB.Shared/OrdersTMB.Shared.csproj", "OrdersTMB.Shared/"]
RUN dotnet restore "./OrdersTMB.Worker/OrdersTMB.Worker.csproj"
COPY . .
WORKDIR "/src/OrdersTMB.Worker"
RUN dotnet build "./OrdersTMB.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrdersTMB.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrdersTMB.Worker.dll"]
