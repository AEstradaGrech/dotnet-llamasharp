FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY DotnetLlamaSharp/DotnetLlamaSharp.csproj ./DotnetLlamaSharp/
COPY DotnetLlamaSharp.Domain/DotnetLlamaSharp.Domain.csproj ./DotnetLlamaSharp.Domain/
COPY DotnetLlamaSharp.Infrastructure/DotnetLlamaSharp.Infrastructure.csproj ./DotnetLlamaSharp.Infrastructure/

RUN dotnet restore DotnetLlamaSharp/DotnetLlamaSharp.csproj

COPY . ./

RUN dotnet publish DotnetLlamaSharp/DotnetLlamaSharp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=5150
EXPOSE 5150
ENTRYPOINT ["dotnet", "DotnetLlamaSharp.dll"]
