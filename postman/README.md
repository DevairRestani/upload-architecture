# Postman Docker

Importe estes dois arquivos no Postman:

- `upload-architecture.postman_collection.json`
- `upload-architecture.docker.postman_environment.json`

Selecione o environment `Upload Architecture - Docker`.

## Como rodar

1. Suba a API e as dependencias no Docker:

   ```bash
   docker compose up -d --build
   ```

2. Aplique as migrations, se o banco estiver vazio:

   ```bash
   dotnet ef database update
   ```

3. No Postman, execute em ordem:

   1. `Files / 1. Create Upload URL`
   2. `Files / 2. Upload Blob With Signed URL`
   3. `Files / 3. Complete Upload`
   4. `Files / 4. Create Download URL`
   5. `Files / 5. Download Blob With Signed URL`

As requests salvam automaticamente `fileId`, `uploadUrl`, `blobPath`, `expiresAt`, `downloadUrl` e `downloadExpiresAt` no environment.

## Variaveis principais

- `baseUrl`: `http://localhost:8080`
- `fileName`: nome do arquivo usado no teste
- `contentType`: tipo MIME enviado para a API e para o Blob
- `sampleFileContent`: conteudo enviado no PUT do Blob
- `sizeBytes`: tamanho do conteudo de teste em bytes

Se alterar `sampleFileContent`, atualize tambem `sizeBytes`.
