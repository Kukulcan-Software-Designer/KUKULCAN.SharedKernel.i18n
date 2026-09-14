# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props KUKULCAN.SharedKernel.i18n.slnx ./
COPY Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj Source/KUKULCAN.SharedKernel.i18n.API/
COPY Source/KUKULCAN.SharedKernel.i18n.Application/KUKULCAN.SharedKernel.i18n.Application.csproj Source/KUKULCAN.SharedKernel.i18n.Application/
COPY Source/KUKULCAN.SharedKernel.i18n.Domain/KUKULCAN.SharedKernel.i18n.Domain.csproj Source/KUKULCAN.SharedKernel.i18n.Domain/
COPY Source/KUKULCAN.SharedKernel.i18n.Infrastructure/KUKULCAN.SharedKernel.i18n.Infrastructure.csproj Source/KUKULCAN.SharedKernel.i18n.Infrastructure/
COPY Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/
COPY Source/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer.csproj Source/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer/
COPY Source/KUKULCAN.SharedKernel.i18n.Migrations.MySql/KUKULCAN.SharedKernel.i18n.Migrations.MySql.csproj Source/KUKULCAN.SharedKernel.i18n.Migrations.MySql/

RUN dotnet restore Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj

COPY Source/ Source/

RUN dotnet publish Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false \
    -p:GeneratePackageOnBuild=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "KUKULCAN.SharedKernel.i18n.dll"]
