FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file from current folder (metroApi)
COPY ["metroApi.csproj", "."]

# Restore dependencies
RUN dotnet restore "./metroApi.csproj"

# Copy everything in the metroApi folder
COPY . .

WORKDIR /src
RUN dotnet build "metroApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "metroApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "metroApi.dll"]
