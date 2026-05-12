FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY frontend/CineBase.Web/CineBase.Web.csproj frontend/CineBase.Web/
RUN dotnet restore frontend/CineBase.Web/CineBase.Web.csproj

COPY . .
RUN dotnet publish frontend/CineBase.Web/CineBase.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "CineBase.Web.dll"]
