FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/backend/exam-project-backend-NazarMikhin.csproj ./src/backend/
RUN dotnet restore ./src/backend/exam-project-backend-NazarMikhin.csproj

COPY src/backend ./src/backend
RUN dotnet publish ./src/backend/exam-project-backend-NazarMikhin.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "exam-project-backend-NazarMikhin.dll"]
