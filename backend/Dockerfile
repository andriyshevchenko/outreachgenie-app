FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy centralized package management files
COPY Directory.Build.props Directory.Build.props
COPY Directory.Packages.props Directory.Packages.props
COPY stylecop.json stylecop.json

# Copy csproj and restore dependencies
COPY backend/OutreachGenie.Api/OutreachGenie.Api.csproj OutreachGenie.Api/
RUN dotnet restore OutreachGenie.Api/OutreachGenie.Api.csproj

# Copy everything else and build
COPY backend/ .
WORKDIR /src/OutreachGenie.Api
RUN dotnet build -c Release -o /app/build -p:TreatWarningsAsErrors=false -p:CodeAnalysisTreatWarningsAsErrors=false

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false -p:TreatWarningsAsErrors=false -p:CodeAnalysisTreatWarningsAsErrors=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5063
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OutreachGenie.Api.dll"]
