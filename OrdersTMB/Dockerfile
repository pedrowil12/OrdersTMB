FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OrdersTMB/OrdersTMB.csproj", "OrdersTMB/"]
COPY ["OrdersTMB.Shared/OrdersTMB.Shared.csproj", "OrdersTMB.Shared/"]
RUN dotnet restore "./OrdersTMB/OrdersTMB.csproj"
COPY . .
WORKDIR "/src/OrdersTMB"
RUN dotnet build "./OrdersTMB.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrdersTMB.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrdersTMB.dll"]
