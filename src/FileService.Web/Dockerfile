FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8002
ENV ASPNETCORE_URLS=http://+:8002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["nuget.config", "."]

COPY ["src/FileService.Web/FileService.Web.csproj", "src/FileService.Web/"]
COPY ["src/FileService.Core/FileService.Core.csproj", "src/FileService.Core/"]
COPY ["src/FileService.Domain/FileService.Domain.csproj", "src/FileService.Domain/"]
COPY ["src/FileService.Infrastructure.Postgres/FileService.Infrastructure.Postgres.csproj", "src/FileService.Infrastructure.Postgres/"]
COPY ["src/FileService.Infrastructure.S3/FileService.Infrastructure.S3.csproj", "src/FileService.Infrastructure.S3/"]

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

ARG GITHUB_TOKEN

RUN dotnet restore "src/FileService.Web/FileService.Web.csproj"

COPY . .
WORKDIR "/src/src/FileService.Web"

RUN dotnet build "./FileService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FileService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileService.Web.dll"]
