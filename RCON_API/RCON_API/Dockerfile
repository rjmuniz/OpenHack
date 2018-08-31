FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["RCON_API/RCON_API.csproj", "RCON_API/"]
RUN dotnet restore "RCON_API/RCON_API.csproj"
COPY . .
WORKDIR "/src/RCON_API"
RUN dotnet build "RCON_API.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RCON_API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RCON_API.dll"]