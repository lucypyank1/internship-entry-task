FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Копируем проекты
COPY ["krestiki_noliki_api/krestiki_noliki_api.csproj", "krestiki_noliki_api/"]
COPY ["krestiki_noliki_api.Tests/krestiki_noliki_api.Tests.csproj", "krestiki_noliki_api.Tests/"]

# Восстанавливаем зависимости
RUN dotnet restore "krestiki_noliki_api/krestiki_noliki_api.csproj"

# Копируем исходники
COPY krestiki_noliki_api/. krestiki_noliki_api/
COPY krestiki_noliki_api.Tests/. krestiki_noliki_api.Tests/

# Запускаем тесты
RUN dotnet test krestiki_noliki_api.Tests/krestiki_noliki_api.Tests.csproj 

# Строим и публикуем приложение
RUN dotnet publish "krestiki_noliki_api/krestiki_noliki_api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "krestiki_noliki_api.dll"]
