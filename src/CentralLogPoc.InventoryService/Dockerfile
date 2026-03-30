FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CentralLogPoc.InventoryService/CentralLogPoc.InventoryService.csproj", "src/CentralLogPoc.InventoryService/"]
COPY ["src/CentralLogPoc.Shared/CentralLogPoc.Shared.csproj",                     "src/CentralLogPoc.Shared/"]
RUN dotnet restore "src/CentralLogPoc.InventoryService/CentralLogPoc.InventoryService.csproj"
COPY . .
WORKDIR "/src/src/CentralLogPoc.InventoryService"
RUN dotnet build "CentralLogPoc.InventoryService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CentralLogPoc.InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CentralLogPoc.InventoryService.dll"]
