# syntax=docker/dockerfile:1.6
ARG BUILD_CONFIGURATION=Release
ARG PROJECT=src/CallWellbeing.Api/CallWellbeing.Api.csproj

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION
ARG PROJECT
WORKDIR /src
COPY CallWellbeing.sln ./
COPY Directory.Build.props ./
COPY src ./src
COPY tests ./tests
COPY data ./data
RUN dotnet restore "$PROJECT"
RUN dotnet publish "$PROJECT" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
CMD ["dotnet", "CallWellbeing.Api.dll"]
