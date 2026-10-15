

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY Release /app

EXPOSE 8080
ENTRYPOINT ["dotnet", "photoHolidayContainer.dll"]
