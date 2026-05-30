# syntax=docker/dockerfile:1

# Runtime image: menor superfície para execução em produção.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Build image: contém SDK apenas durante restore/build/publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia primeiro somente o projeto para aproveitar cache do restore.
COPY ["upload-architecture.csproj", "./"]
RUN dotnet restore "upload-architecture.csproj"

# Copia o restante do código após o restore.
COPY . .
# Publica a aplicação sem apphost para reduzir artefatos finais.
FROM build AS publish
RUN dotnet publish "upload-architecture.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# Imagem final: somente runtime + binários publicados.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Usuário não-root fornecido pelas imagens oficiais .NET.
USER $APP_UID
ENTRYPOINT ["dotnet", "upload-architecture.dll"]
