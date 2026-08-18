FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Meals.Api/Meals.Api.csproj", "src/Services/Meals.Api/"]
RUN dotnet restore "src/Services/Meals.Api/Meals.Api.csproj"
COPY src/Services/Meals.Api/ src/Services/Meals.Api/
WORKDIR /src/src/Services/Meals.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Meals.Api.dll"]
