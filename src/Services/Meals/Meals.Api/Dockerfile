FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BuildingBlocks/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj", "src/BuildingBlocks/BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj", "src/BuildingBlocks/BuildingBlocks.Application/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj", "src/BuildingBlocks/BuildingBlocks.Infrastructure/"]
COPY ["src/Services/Meals/Meals.Domain/Meals.Domain.csproj", "src/Services/Meals/Meals.Domain/"]
COPY ["src/Services/Meals/Meals.Application/Meals.Application.csproj", "src/Services/Meals/Meals.Application/"]
COPY ["src/Services/Meals/Meals.Infrastructure/Meals.Infrastructure.csproj", "src/Services/Meals/Meals.Infrastructure/"]
COPY ["src/Services/Meals/Meals.Api/Meals.Api.csproj", "src/Services/Meals/Meals.Api/"]
RUN dotnet restore "src/Services/Meals/Meals.Api/Meals.Api.csproj"
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Meals/ src/Services/Meals/
WORKDIR /src/src/Services/Meals/Meals.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Meals.Api.dll"]
