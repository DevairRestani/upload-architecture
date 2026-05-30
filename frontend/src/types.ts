export interface UploadUrlResponse {
  id: string;
  blobPath: string;
  uploadUrl: string;
  expiresAt: string;
}

export interface CompleteUploadResponse {
  fileId: string;
  status: string;
}

export interface DownloadUrlResponse {
  fileId: string;
  downloadUrl: string;
  expiresAt: string;
}

export interface FileRecord {
  id: string;
  originalName: string;
  blobPath: string;
  contentType: string;
  sizeBytes: number;
  status: "Pending" | "Uploaded" | "Validated" | "Failed" | "Expired" | string;
  scanStatus: "NotScanned" | "Clean" | "Scanning" | "Infected" | "Failed" | string;
  uploadUrlExpiresAt: string | null;
  uploadedAt: string | null;
  scannedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface UploadJob {
  id: string;
  fileName: string;
  sizeBytes: number;
  progress: number;
  stage: "creating" | "uploading" | "completing" | "done" | "failed";
  message: string;
}
