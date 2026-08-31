FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/Fluxora.Domain/Fluxora.Domain.csproj src/Fluxora.Domain/
COPY src/Fluxora.Application/Fluxora.Application.csproj src/Fluxora.Application/
COPY src/Fluxora.Infrastructure/Fluxora.Infrastructure.csproj src/Fluxora.Infrastructure/
COPY src/Fluxora.Api/Fluxora.Api.csproj src/Fluxora.Api/
RUN dotnet restore src/Fluxora.Api/Fluxora.Api.csproj

COPY src/ src/
RUN dotnet publish src/Fluxora.Api/Fluxora.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system fluxora && useradd --system --gid fluxora --no-create-home fluxora
COPY --from=build /app .
USER fluxora

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fluxora.Api.dll"]
