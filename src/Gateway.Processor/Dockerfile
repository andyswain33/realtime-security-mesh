# 1. BUILD STAGE: Use the heavy .NET 10 SDK to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to cache the NuGet restore layer
COPY ["Gateway.Processor/Gateway.Processor.csproj", "Gateway.Processor/"]
COPY ["Gateway.Core/Gateway.Core.csproj", "Gateway.Core/"]

# Restore dependencies
RUN dotnet restore "Gateway.Processor/Gateway.Processor.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the application in Release mode
WORKDIR "/src/Gateway.Processor"
RUN dotnet publish "Gateway.Processor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. FINAL STAGE: Use the lightweight .NET 10 Runtime to run the app
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Copy the compiled binaries from the build stage
COPY --from=build /app/publish .

# Enterprise Security: Run the container as a non-root user (Standard in .NET 8+)
USER $APP_UID

# Start the worker
ENTRYPOINT ["dotnet", "Gateway.Processor.dll"]