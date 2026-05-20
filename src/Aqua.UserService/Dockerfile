FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props Directory.Build.props ./
COPY src/Aqua.Contracts ./Aqua.Contracts
COPY src/Aqua.Data ./Aqua.Data
COPY src/Aqua.UserService ./Aqua.UserService
RUN dotnet publish Aqua.UserService -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "Aqua.UserService.dll"]
