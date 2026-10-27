

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*


WORKDIR /app
COPY Release /app

EXPOSE 8080
ENTRYPOINT ["dotnet", "photoHolidayContainer.dll"]
