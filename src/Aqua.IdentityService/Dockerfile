FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props nuget.config global.json ./
COPY src/Aqua.Contracts/ src/Aqua.Contracts/
COPY src/Aqua.Data/ src/Aqua.Data/
COPY src/Aqua.IdentityService/ src/Aqua.IdentityService/
RUN dotnet restore src/Aqua.IdentityService/Aqua.IdentityService.csproj
RUN dotnet publish src/Aqua.IdentityService/Aqua.IdentityService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER 1000:1000
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Aqua.IdentityService.dll"]
