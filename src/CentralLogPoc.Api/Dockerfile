# NOTE: .NET 10 henüz preview aşamasında olabilir.
# "10.0" tag bulunamazsa "10.0-preview" kullan.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CentralLogPoc.Api/CentralLogPoc.Api.csproj",       "src/CentralLogPoc.Api/"]
COPY ["src/CentralLogPoc.Shared/CentralLogPoc.Shared.csproj", "src/CentralLogPoc.Shared/"]
RUN dotnet restore "src/CentralLogPoc.Api/CentralLogPoc.Api.csproj"
COPY . .
WORKDIR "/src/src/CentralLogPoc.Api"
RUN dotnet build "CentralLogPoc.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CentralLogPoc.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CentralLogPoc.Api.dll"]
