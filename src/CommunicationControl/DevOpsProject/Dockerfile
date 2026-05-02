FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/CommunicationControl/DevOpsProject/DevOpsProject.CommunicationControl.API.csproj -c Release -o /app/published

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN groupadd --gid 1000 appuser && \
    useradd --uid 1000 --gid appuser --shell /bin/bash --create-home appuser
COPY --from=build --chown=appuser:appuser /app/published/ ./
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "DevOpsProject.CommunicationControl.API.dll"]
