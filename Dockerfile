FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OstrunMessagingService.sln .
COPY src/OstrunMessagingService.Domain/*.csproj src/OstrunMessagingService.Domain/
COPY src/OstrunMessagingService.Application/*.csproj src/OstrunMessagingService.Application/
COPY src/OstrunMessagingService.Infrastructure/*.csproj src/OstrunMessagingService.Infrastructure/
COPY src/OstrunMessagingService.Api/*.csproj src/OstrunMessagingService.Api/
RUN dotnet restore src/OstrunMessagingService.Api/OstrunMessagingService.Api.csproj

COPY src/ src/
RUN dotnet publish src/OstrunMessagingService.Api/OstrunMessagingService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN useradd --uid 5678 --user-group --no-create-home appuser
COPY --from=build /app/publish .
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OstrunMessagingService.Api.dll"]
