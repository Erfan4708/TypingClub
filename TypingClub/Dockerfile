# Use ASP.NET base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use SDK for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TypingClub/TypingClub.csproj", "TypingClub/"]
RUN dotnet restore "TypingClub/TypingClub.csproj"
COPY . . 
WORKDIR "/src/TypingClub"
RUN dotnet build "TypingClub.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "TypingClub.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Setup the base image for the final container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TypingClub.dll"]
