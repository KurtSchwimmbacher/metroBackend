FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app from local publish folder into the image
COPY ./publish .

# Expose port 80
EXPOSE 80

# Start the app
ENTRYPOINT ["dotnet", "metroApi.dll"]
