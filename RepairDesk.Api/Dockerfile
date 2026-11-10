FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RepairDesk.Api/RepairDesk.Api.csproj RepairDesk.Api/
RUN dotnet restore RepairDesk.Api/RepairDesk.Api.csproj

COPY RepairDesk.Api/ RepairDesk.Api/
RUN dotnet publish RepairDesk.Api/RepairDesk.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RepairDesk.Api.dll"]
