FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Scheduler.Api/*.csproj Scheduler.Api/
RUN dotnet restore Scheduler.Api/Scheduler.Api.csproj
COPY Scheduler.Api/ Scheduler.Api/
RUN dotnet publish Scheduler.Api/Scheduler.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Scheduler.Api.dll"]
