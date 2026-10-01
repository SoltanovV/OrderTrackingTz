FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrderTracking.csproj", "NuGet.Config", "./"]
COPY Domain/OrderTracking.Domain.csproj Domain/
COPY Application/OrderTracking.Application.csproj Application/
COPY Infrastructure/OrderTracking.Infrastructure.csproj Infrastructure/
RUN dotnet restore "OrderTracking.csproj" --configfile NuGet.Config
COPY . .
RUN dotnet publish "OrderTracking.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "OrderTracking.dll"]
