FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . ./

RUN dotnet restore DevOpsProject.CommunicationControl.API.csproj
RUN dotnet publish DevOpsProject.CommunicationControl.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd --gid 1000 appuser && \
    useradd --uid 1000 --gid appuser --shell /bin/bash --create-home appuser

COPY --from=build --chown=appuser:appuser /app/publish ./

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "DevOpsProject.CommunicationControl.API.dll"]