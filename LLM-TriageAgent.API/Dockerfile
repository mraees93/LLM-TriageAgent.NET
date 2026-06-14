# Stage 1: Build Environment
FROM mcr.microsoft.com AS build-env
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Final Production Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "LLM-TriageAgent.API.dll"]
