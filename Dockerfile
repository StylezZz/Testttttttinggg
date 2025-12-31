FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RiskListScraperAPI/RiskListScraperAPI.csproj", "RiskListScraperAPI/"]
RUN dotnet restore "RiskListScraperAPI/RiskListScraperAPI.csproj"
COPY src/RiskListScraperAPI/. RiskListScraperAPI/
WORKDIR "/src/RiskListScraperAPI"
RUN dotnet build "RiskListScraperAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RiskListScraperAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RiskListScraperAPI.dll"]
