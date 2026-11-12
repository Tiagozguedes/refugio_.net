# Build da Refúgio API. Rode a partir da raiz do repositório:
#   docker build -f dockerfiles/Dockerfile -t refugio-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj, para aproveitar o cache das camadas.
COPY src/Refugio.Api/Refugio.Api.csproj src/Refugio.Api/
COPY src/Refugio.Application/Refugio.Application.csproj src/Refugio.Application/
COPY src/Refugio.Domain/Refugio.Domain.csproj src/Refugio.Domain/
COPY src/Refugio.Infrastructure/Refugio.Infrastructure.csproj src/Refugio.Infrastructure/
RUN dotnet restore src/Refugio.Api/Refugio.Api.csproj

COPY src/ src/
RUN dotnet publish src/Refugio.Api/Refugio.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# Usuário sem privilégios que já vem na imagem oficial.
USER $APP_UID

ENTRYPOINT ["dotnet", "Refugio.Api.dll"]
