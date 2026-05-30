import type {
  CompleteUploadResponse,
  DownloadUrlResponse,
  FileRecord,
  UploadUrlResponse
} from "./types";

const API_BASE_STORAGE_KEY = "upload-architecture-api-base-url";

export function getInitialApiBaseUrl(): string {
  return (
    localStorage.getItem(API_BASE_STORAGE_KEY) ??
    import.meta.env.VITE_API_BASE_URL ??
    ""
  ).replace(/\/$/, "");
}

export function saveApiBaseUrl(value: string) {
  localStorage.setItem(API_BASE_STORAGE_KEY, value.replace(/\/$/, ""));
}

export function createApiClient(apiBaseUrl: string) {
  const baseUrl = apiBaseUrl.replace(/\/$/, "");

  async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...init?.headers
      }
    });

    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || `HTTP ${response.status}`);
    }

    return response.json() as Promise<T>;
  }

  return {
    listFiles: () => request<FileRecord[]>("/files"),
    getFile: (id: string) => request<FileRecord>(`/files/${id}`),
    createUploadUrl: (file: File) =>
      request<UploadUrlResponse>("/files/upload-url", {
        method: "POST",
        body: JSON.stringify({
          fileName: file.name,
          contentType: file.type || "application/octet-stream",
          sizeBytes: file.size
        })
      }),
    completeUpload: (id: string) =>
      request<CompleteUploadResponse>(`/files/${id}/complete`, {
        method: "POST"
      }),
    createDownloadUrl: (id: string) =>
      request<DownloadUrlResponse>(`/files/${id}/download-url`, {
        method: "POST"
      })
  };
}

export function uploadBlob(
  uploadUrl: string,
  file: File,
  onProgress: (progress: number) => void
) {
  return new Promise<void>((resolve, reject) => {
    const xhr = new XMLHttpRequest();

    xhr.open("PUT", getBrowserUploadUrl(uploadUrl), true);
    xhr.setRequestHeader("x-ms-blob-type", "BlockBlob");
    xhr.setRequestHeader("Content-Type", file.type || "application/octet-stream");

    xhr.upload.onprogress = (event) => {
      if (!event.lengthComputable) return;
      onProgress(Math.round((event.loaded / event.total) * 100));
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        onProgress(100);
        resolve();
        return;
      }

      reject(new Error(xhr.responseText || `Falha no upload do Blob: HTTP ${xhr.status}`));
    };

    xhr.onerror = () => {
      reject(new Error("Falha de rede ao enviar o arquivo para o Blob Storage."));
    };

    xhr.send(file);
  });
}

function getBrowserUploadUrl(uploadUrl: string) {
  const url = new URL(uploadUrl);
  const isLocalAzurite =
    ["localhost", "127.0.0.1"].includes(url.hostname) && url.port === "10000";
  const isViteDevServer =
    ["localhost", "127.0.0.1"].includes(window.location.hostname) &&
    window.location.port === "5173";

  if (isLocalAzurite && isViteDevServer) {
    return `/blob${url.pathname}${url.search}`;
  }

  return uploadUrl;
}
