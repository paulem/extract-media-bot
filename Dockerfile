FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Telegram.ExtractMediaBot.csproj", "Telegram.ExtractMediaBot/"]
RUN dotnet restore "Telegram.ExtractMediaBot/Telegram.ExtractMediaBot.csproj"

COPY . "Telegram.ExtractMediaBot/"
WORKDIR "/src/Telegram.ExtractMediaBot/"
RUN dotnet build "Telegram.ExtractMediaBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Telegram.ExtractMediaBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Telegram.ExtractMediaBot.dll"]
