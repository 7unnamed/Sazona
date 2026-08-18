FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Planner.Api/Planner.Api.csproj", "src/Services/Planner.Api/"]
RUN dotnet restore "src/Services/Planner.Api/Planner.Api.csproj"
COPY src/Services/Planner.Api/ src/Services/Planner.Api/
WORKDIR /src/src/Services/Planner.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Planner.Api.dll"]
