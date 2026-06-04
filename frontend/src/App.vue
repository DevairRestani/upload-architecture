<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import {
  CheckCircle2,
  Clock3,
  Download,
  FileUp,
  RefreshCw,
  SearchCheck,
  Server,
  ShieldAlert,
  UploadCloud,
  XCircle
} from "@lucide/vue";
import {
  createApiClient,
  getInitialApiBaseUrl,
  saveApiBaseUrl,
  uploadBlob
} from "./api";
import type { FileRecord, UploadJob } from "./types";

const apiBaseUrl = ref(getInitialApiBaseUrl());
const selectedFiles = ref<File[]>([]);
const files = ref<FileRecord[]>([]);
const activeJobs = ref<UploadJob[]>([]);
const isDragging = ref(false);
const isRefreshing = ref(false);
const isUploading = ref(false);
const errorMessage = ref("");
const successMessage = ref("");
const fileInput = ref<HTMLInputElement | null>(null);

const api = computed(() => createApiClient(apiBaseUrl.value));
let refreshTimer: number | undefined;

const pendingUploads = computed(() =>
  files.value.some((file) =>
    ["Uploaded", "Validated"].includes(file.status) &&
    ["NotScanned", "Scanning"].includes(file.scanStatus)
  )
);

function handleApiUrlChange() {
  saveApiBaseUrl(apiBaseUrl.value);
  refreshFiles();
}

function openFilePicker() {
  fileInput.value?.click();
}

function onInputChange(event: Event) {
  const target = event.target as HTMLInputElement;
  selectedFiles.value = Array.from(target.files ?? []);
  target.value = "";
}

function onDrop(event: DragEvent) {
  isDragging.value = false;
  selectedFiles.value = Array.from(event.dataTransfer?.files ?? []);
}

async function refreshFiles(silent = false) {
  if (!silent) {
    isRefreshing.value = true;
    errorMessage.value = "";
  }

  try {
    files.value = await api.value.listFiles();
  } catch (error) {
    if (!silent) {
      errorMessage.value = getErrorMessage(error);
    }
  } finally {
    isRefreshing.value = false;
  }
}

async function uploadSelectedFiles() {
  if (selectedFiles.value.length === 0 || isUploading.value) return;

  isUploading.value = true;
  errorMessage.value = "";
  successMessage.value = "";

  for (const file of selectedFiles.value) {
    await uploadSingleFile(file);
  }

  selectedFiles.value = [];
  isUploading.value = false;
  await refreshFiles(true);
}

async function uploadSingleFile(file: File) {
  const job: UploadJob = {
    id: crypto.randomUUID(),
    fileName: file.name,
    sizeBytes: file.size,
    progress: 0,
    stage: "creating",
    message: "Solicitando URL assinada"
  };

  activeJobs.value = [job, ...activeJobs.value].slice(0, 5);

  try {
    const uploadUrl = await api.value.createUploadUrl(file);
    updateJob(job.id, {
      stage: "uploading",
      message: "Enviando para o Blob Storage"
    });

    await uploadBlob(uploadUrl.uploadUrl, file, (progress) => {
      updateJob(job.id, { progress });
    });

    updateJob(job.id, {
      stage: "completing",
      message: "Confirmando upload no backend"
    });

    await api.value.completeUpload(uploadUrl.id);
    updateJob(job.id, {
      stage: "done",
      progress: 100,
      message: "Upload concluido; aguardando scan"
    });
    successMessage.value = "Arquivo enviado. O worker de antivirus atualiza o status em seguida.";
  } catch (error) {
    updateJob(job.id, {
      stage: "failed",
      message: getErrorMessage(error)
    });
    errorMessage.value = getErrorMessage(error);
  }
}

function updateJob(id: string, patch: Partial<UploadJob>) {
  activeJobs.value = activeJobs.value.map((job) =>
    job.id === id ? { ...job, ...patch } : job
  );
}

async function downloadFile(file: FileRecord) {
  errorMessage.value = "";
  successMessage.value = "";

  try {
    const response = await api.value.createDownloadUrl(file.id);
    window.open(response.downloadUrl, "_blank", "noopener,noreferrer");
  } catch (error) {
    errorMessage.value = getErrorMessage(error);
  }
}

function canDownload(file: FileRecord) {
  return (
    ["Uploaded", "Validated"].includes(file.status) &&
    file.scanStatus === "Clean"
  );
}

function statusIcon(file: FileRecord) {
  if (file.scanStatus === "Clean") return CheckCircle2;
  if (file.scanStatus === "Infected") return ShieldAlert;
  if (file.scanStatus === "Failed" || file.status === "Failed") return XCircle;
  if (file.scanStatus === "Scanning") return SearchCheck;
  return Clock3;
}

function statusClass(file: FileRecord) {
  if (file.scanStatus === "Clean") return "status status--ok";
  if (file.scanStatus === "Infected" || file.status === "Failed") {
    return "status status--danger";
  }
  if (file.scanStatus === "Scanning") return "status status--scan";
  return "status status--wait";
}

function formatBytes(bytes: number) {
  if (bytes === 0) return "0 B";
  const units = ["B", "KB", "MB", "GB"];
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  const value = bytes / 1024 ** index;
  return `${value.toFixed(value >= 10 || index === 0 ? 0 : 1)} ${units[index]}`;
}

function formatDate(value: string | null) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short"
  }).format(new Date(value));
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : "Erro inesperado.";
}

onMounted(() => {
  refreshFiles();
  refreshTimer = window.setInterval(() => {
    if (
      pendingUploads.value ||
      activeJobs.value.some((job) =>
        ["creating", "uploading", "completing"].includes(job.stage)
      )
    ) {
      refreshFiles(true);
    }
  }, 4000);
});

onUnmounted(() => {
  if (refreshTimer) window.clearInterval(refreshTimer);
});
</script>

<template>
  <main class="shell">
    <header class="topbar">
      <div>
        <p class="eyebrow">Upload Architecture</p>
        <h1>Arquivos com URL assinada e scan</h1>
      </div>

      <label class="api-config">
        <Server :size="18" aria-hidden="true" />
        <span>API</span>
        <input
          v-model.trim="apiBaseUrl"
          placeholder="mesma origem ou http://localhost:8080"
          @change="handleApiUrlChange"
        />
      </label>
    </header>

    <section class="layout">
      <div class="upload-panel">
        <div
          class="dropzone"
          :class="{ 'dropzone--active': isDragging }"
          @dragover.prevent="isDragging = true"
          @dragleave.prevent="isDragging = false"
          @drop.prevent="onDrop"
        >
          <input
            ref="fileInput"
            class="file-input"
            type="file"
            multiple
            @change="onInputChange"
          />
          <UploadCloud :size="36" aria-hidden="true" />
          <h2>Enviar arquivos</h2>
          <p>Arraste arquivos para ca ou selecione no computador.</p>
          <button class="button button--secondary" type="button" @click="openFilePicker">
            <FileUp :size="18" aria-hidden="true" />
            Selecionar
          </button>
        </div>

        <div class="selected-list" v-if="selectedFiles.length">
          <div class="selected-header">
            <strong>{{ selectedFiles.length }} arquivo(s)</strong>
            <button
              class="button"
              type="button"
              :disabled="isUploading"
              @click="uploadSelectedFiles"
            >
              <UploadCloud :size="18" aria-hidden="true" />
              Enviar agora
            </button>
          </div>
          <ul>
            <li v-for="file in selectedFiles" :key="`${file.name}-${file.size}`">
              <span>{{ file.name }}</span>
              <small>{{ formatBytes(file.size) }}</small>
            </li>
          </ul>
        </div>

        <div class="jobs" v-if="activeJobs.length">
          <div class="job" v-for="job in activeJobs" :key="job.id">
            <div class="job-line">
              <strong>{{ job.fileName }}</strong>
              <span>{{ job.progress }}%</span>
            </div>
            <progress :value="job.progress" max="100"></progress>
            <small :class="{ danger: job.stage === 'failed' }">{{ job.message }}</small>
          </div>
        </div>
      </div>

      <section class="table-panel">
        <div class="panel-toolbar">
          <div>
            <p class="eyebrow">Historico</p>
            <h2>Arquivos processados</h2>
          </div>
          <button class="icon-button" type="button" :disabled="isRefreshing" @click="refreshFiles()">
            <RefreshCw :size="18" aria-label="Atualizar" />
          </button>
        </div>

        <div class="alert alert--error" v-if="errorMessage">{{ errorMessage }}</div>
        <div class="alert alert--success" v-if="successMessage">{{ successMessage }}</div>

        <div class="empty" v-if="!files.length && !isRefreshing && !errorMessage">
          Nenhum arquivo encontrado.
        </div>

        <div class="table-wrap" v-if="files.length">
          <table>
            <thead>
              <tr>
                <th>Arquivo</th>
                <th>Tamanho</th>
                <th>Status</th>
                <th>Criado em</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="file in files" :key="file.id">
                <td>
                  <strong>{{ file.originalName }}</strong>
                  <small>{{ file.contentType }}</small>
                </td>
                <td>{{ formatBytes(file.sizeBytes) }}</td>
                <td>
                  <span :class="statusClass(file)">
                    <component :is="statusIcon(file)" :size="16" aria-hidden="true" />
                    {{ file.status }} / {{ file.scanStatus }}
                  </span>
                </td>
                <td>{{ formatDate(file.createdAt) }}</td>
                <td class="actions">
                  <button
                    class="icon-button"
                    type="button"
                    :disabled="!canDownload(file)"
                    @click="downloadFile(file)"
                  >
                    <Download :size="18" aria-label="Baixar" />
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </section>
  </main>
</template>
