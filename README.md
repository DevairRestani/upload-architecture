# Upload Architecture

Projeto simples para testar uma arquitetura de upload de arquivos usando API, Blob Storage e banco de dados.

A API gera URLs assinadas para upload/download, salva os metadados no PostgreSQL e valida o arquivo enviado no Blob. Tambem tem um worker usando ClamAV para simular o fluxo de scan.

## Stack

- ASP.NET Core / .NET 10
- PostgreSQL
- Azurite
- ClamAV
- Vue 3 + Vite no frontend

## Como rodar

Suba tudo pelo Docker Compose:

```bash
docker compose up -d --build
```

A API fica em:

```text
http://localhost:8080
```

O PostgreSQL fica exposto em `localhost:5433` e o Azurite em `localhost:10000`.

Se o banco estiver vazio, aplique as migrations:

```bash
dotnet ef database update
```

## Frontend

Entre na pasta do frontend e rode:

```bash
cd frontend
npm install
npm run dev
```

O Vite sobe em:

```text
http://localhost:5173
```

Por padrao, o frontend chama a API em `http://localhost:8080`.

## Postman

Tem uma collection pronta em `postman/`.

Importe estes arquivos:

- `upload-architecture.postman_collection.json`
- `upload-architecture.docker.postman_environment.json`

Depois selecione o environment `Upload Architecture - Docker` e rode as requests na ordem.

## Observacoes

- O Docker Compose sobe API, Postgres, Azurite e ClamAV.
- As URLs de upload/download sao assinadas e temporarias.
- O arquivo `test-files/eicar-test.txt` pode ser usado para testar deteccao do ClamAV.
- Mais detalhes do fluxo estao em `docs/architecture-and-flows.md`.
