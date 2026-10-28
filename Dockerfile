FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=5150
EXPOSE 5150

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY DotnetLlamaSharp/DotnetLlamaSharp.csproj ./DotnetLlamaSharp/
COPY DotnetLlamaSharp.Domain/DotnetLlamaSharp.Domain.csproj ./DotnetLlamaSharp.Domain/
COPY DotnetLlamaSharp.Infrastructure/DotnetLlamaSharp.Infrastructure.csproj ./DotnetLlamaSharp.Infrastructure/

RUN dotnet restore DotnetLlamaSharp/DotnetLlamaSharp.csproj

COPY . ./

RUN dotnet publish DotnetLlamaSharp/DotnetLlamaSharp.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DotnetLlamaSharp.dll"]
