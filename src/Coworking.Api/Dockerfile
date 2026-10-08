# ============================ ESTÁGIO 1: BUILD ============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro (aproveita cache de camadas do Docker)
COPY ["src/Coworking.Api/Coworking.Api.csproj", "src/Coworking.Api/"]
RUN dotnet restore "src/Coworking.Api/Coworking.Api.csproj"

COPY . .
RUN dotnet publish "src/Coworking.Api/Coworking.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ========================== ESTÁGIO 2: RUNTIME ===========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Coworking.Api.dll"]
