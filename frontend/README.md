# Upload Architecture Frontend

Frontend Vue 3 para operar o fluxo do backend:

- cria URL assinada em `POST /files/upload-url`;
- envia o arquivo para o Blob Storage com `PUT`;
- confirma o upload em `POST /files/{id}/complete`;
- lista status em `GET /files`;
- gera download em `POST /files/{id}/download-url`.

## Rodar localmente

```bash
pnpm install
pnpm dev
```

O Vite sobe em `http://localhost:5173`.

Por padrao, as chamadas `/files` sao proxied para `http://localhost:8080`, que e a porta do backend no Docker Compose. Para usar o backend via `dotnet run --launch-profile http` em `http://localhost:5203`, crie um `.env.local`:

```bash
VITE_API_BASE_URL=http://localhost:5203
```

O upload para Azurite local (`http://localhost:10000`) passa pelo proxy `/blob` no Vite para evitar CORS durante o desenvolvimento.
