FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TOSAi.Api/TOSAi.Api.csproj TOSAi.Api/
RUN dotnet restore TOSAi.Api/TOSAi.Api.csproj

COPY . .
RUN dotnet publish TOSAi.Api/TOSAi.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} exec dotnet TOSAi.Api.dll"]