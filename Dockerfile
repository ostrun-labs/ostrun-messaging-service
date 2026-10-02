FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OstrunNotificationService.sln .
COPY src/OstrunNotificationService.Domain/*.csproj src/OstrunNotificationService.Domain/
COPY src/OstrunNotificationService.Application/*.csproj src/OstrunNotificationService.Application/
COPY src/OstrunNotificationService.Infrastructure/*.csproj src/OstrunNotificationService.Infrastructure/
COPY src/OstrunNotificationService.Api/*.csproj src/OstrunNotificationService.Api/
RUN dotnet restore src/OstrunNotificationService.Api/OstrunNotificationService.Api.csproj

COPY src/ src/
RUN dotnet publish src/OstrunNotificationService.Api/OstrunNotificationService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN useradd --uid 5678 --user-group --no-create-home appuser
COPY --from=build /app/publish .
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OstrunNotificationService.Api.dll"]
