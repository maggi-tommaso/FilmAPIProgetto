FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/FilmAPI/FilmAPI.csproj backend/FilmAPI/
RUN dotnet restore backend/FilmAPI/FilmAPI.csproj

COPY . .
RUN dotnet publish backend/FilmAPI/FilmAPI.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

RUN mkdir -p /app/wwwroot

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "FilmAPI.dll"]
