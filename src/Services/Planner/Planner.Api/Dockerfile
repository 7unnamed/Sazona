FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BuildingBlocks/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj", "src/BuildingBlocks/BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj", "src/BuildingBlocks/BuildingBlocks.Application/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj", "src/BuildingBlocks/BuildingBlocks.Infrastructure/"]
COPY ["src/Services/Planner/Planner.Domain/Planner.Domain.csproj", "src/Services/Planner/Planner.Domain/"]
COPY ["src/Services/Planner/Planner.Application/Planner.Application.csproj", "src/Services/Planner/Planner.Application/"]
COPY ["src/Services/Planner/Planner.Infrastructure/Planner.Infrastructure.csproj", "src/Services/Planner/Planner.Infrastructure/"]
COPY ["src/Services/Planner/Planner.Api/Planner.Api.csproj", "src/Services/Planner/Planner.Api/"]
RUN dotnet restore "src/Services/Planner/Planner.Api/Planner.Api.csproj"
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Planner/ src/Services/Planner/
WORKDIR /src/src/Services/Planner/Planner.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Planner.Api.dll"]
