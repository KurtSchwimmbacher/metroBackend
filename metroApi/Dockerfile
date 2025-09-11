# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /publish .

# Expose port 8080 (Render expects this)
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "metroApi.dll"]
