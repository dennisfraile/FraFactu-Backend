# syntax=docker/dockerfile:1

# ---------- Etapa de build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para aprovechar la cache de capas en el restore
COPY FraFactu.sln ./
COPY src/FraFactu.Domain/FraFactu.Domain.csproj          src/FraFactu.Domain/
COPY src/FraFactu.Application/FraFactu.Application.csproj src/FraFactu.Application/
COPY src/FraFactu.Infrastructure/FraFactu.Infrastructure.csproj src/FraFactu.Infrastructure/
COPY src/FraFactu.API/FraFactu.API.csproj                src/FraFactu.API/
COPY tests/FraFactu.Tests/FraFactu.Tests.csproj          tests/FraFactu.Tests/
RUN dotnet restore FraFactu.sln

# Copiar el resto del código y publicar el API
COPY . .
RUN dotnet publish src/FraFactu.API/FraFactu.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- Etapa de runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# libfontconfig1: requerido por SkiaSharp/QuestPDF (generación de PDFs) en Linux
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

COPY --from=build /app/publish .

# Usuario no-root (endurecimiento; se completa en F9)
RUN useradd -m appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "FraFactu.API.dll"]
