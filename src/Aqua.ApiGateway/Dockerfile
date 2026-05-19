FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props nuget.config global.json ./
COPY src/Aqua.Contracts/ src/Aqua.Contracts/
COPY src/Aqua.ApiGateway/ src/Aqua.ApiGateway/
RUN dotnet restore src/Aqua.ApiGateway/Aqua.ApiGateway.csproj
RUN dotnet publish src/Aqua.ApiGateway/Aqua.ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER 1000:1000
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Aqua.ApiGateway.dll"]
