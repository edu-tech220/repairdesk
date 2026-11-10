FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RepairDesk.Web/RepairDesk.Web.csproj RepairDesk.Web/
RUN dotnet restore RepairDesk.Web/RepairDesk.Web.csproj

COPY RepairDesk.Web/ RepairDesk.Web/
RUN dotnet publish RepairDesk.Web/RepairDesk.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RepairDesk.Web.dll"]
