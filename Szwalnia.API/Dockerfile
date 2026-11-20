FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Szwalnia.API/Szwalnia.API.csproj", "Szwalnia.API/"]

RUN dotnet restore "Szwalnia.API/Szwalnia.API.csproj"

COPY Szwalnia.API/ Szwalnia.API/

RUN dotnet publish "Szwalnia.API/Szwalnia.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false 

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir /app/keys && chown "$APP_UID" /app/keys

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Szwalnia.API.dll"]
