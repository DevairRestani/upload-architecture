# Upload Architecture

Uma implementação de referência para um fluxo de upload seguro: o cliente envia arquivos diretamente para o Blob Storage com URLs assinadas, enquanto a API mantém o estado e libera o download somente após a validação antivírus.

## Fluxo

1. O cliente solicita uma URL temporária em `POST /files/upload-url`.
2. O arquivo é enviado diretamente para um blob temporário.
3. O cliente confirma o envio em `POST /files/{id}/complete`.
4. Um worker verifica o arquivo com ClamAV e, quando limpo, promove-o ao blob definitivo.
5. A API gera uma URL temporária de download em `POST /files/{id}/download-url`.

O backend valida extensão, MIME type, tamanho e comprimento do blob antes de marcar o arquivo como enviado. O download só é liberado para arquivos validados e limpos.

## Stack

- ASP.NET Core (.NET 10) e Entity Framework Core
- PostgreSQL para metadados e estados do upload
- Azure Blob Storage, usando Azurite no ambiente local
- ClamAV para varredura antivírus
- Vue 3 + Vite para a interface de demonstração
- Docker Compose para o ambiente completo

## Executar localmente

Pré-requisito: Docker com Docker Compose.

```bash
docker compose up -d --build
```

Serviços locais:

| Serviço | Endereço |
| --- | --- |
| API | `http://localhost:8080` |
| PostgreSQL | `localhost:5433` |
| Azurite Blob | `http://localhost:10000` |
| ClamAV | `localhost:3310` |

As migrations são aplicadas automaticamente quando a API sobe.

Em desenvolvimento, a referência OpenAPI está disponível em `http://localhost:8080/scalar/v1`.

## Interface web

Com a API e os serviços em execução:

```bash
cd frontend
pnpm install
pnpm dev
```

A interface fica em `http://localhost:5173`. Ela cria a URL assinada, realiza o upload, confirma o arquivo e acompanha o status do processamento.

## Testar a API

Há uma collection no diretório [`postman/`](postman/). Importe a collection e o ambiente Docker, selecione **Upload Architecture - Docker** e execute as requisições na ordem.

Para validar o fluxo de antivírus, use [`test-files/eicar-test.txt`](test-files/eicar-test.txt), uma assinatura de teste segura do EICAR.

## Estrutura

```text
Controllers/  endpoints HTTP do fluxo de arquivos
Services/     Blob Storage, regras de tipos aceitos e ClamAV
Worker/       varredura e promoção de arquivos
Data/         contexto e migrations do PostgreSQL
frontend/     aplicação Vue 3
```

## Licença

[MIT](LICENSE)
