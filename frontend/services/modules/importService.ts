import { apiClientWithAuth } from "~/services/api/client";

export interface ImportResultDto {
  importId: string;
  dataSourceName: string;
  targetEntity: string;
  totalRecordsInSource: number;
  recordsSucceeded: number;
  recordsFailed: number;
  receivedAtUtc: string;
  finishedAtUtc?: string;
  newOrUpdatedEntityIds: number[];
  errors: ImportErrorDto[];
}

export interface ImportErrorDto {
  sourceRowNumber: number;
  entityKeyValue?: string | null;
  errorMessage: string;
}

export interface ImportAuditDto {
  importId: string;
  dataSourceName: string;
  dataSourceType: string;
  targetEntity: string;
  totalRecordsInSource: number;
  recordsSucceeded: number;
  recordsFailed: number;
  receivedAtUtc: string;
  finishedAtUtc?: string;
}

export interface ImportAuditDetailDto extends ImportAuditDto {
  sourceOriginalFilename?: string | null;
  errors: ImportErrorDto[];
}

// ===== Dry-run / confirmação por categoria =====

export interface CategoryConfirmationDto {
  category: string;
  targetEntity: string;
  rowCount: number;
  canImport: boolean;
  mappedFields: Record<string, string | null>[];
  rowsWithErrors: number[];
  errors: string[];
}

export interface ImportPreviewDto {
  fileName: string;
  contentType?: string | null;
  totalRows: number;
  headers: string[];
  unmappedColumns: string[];
  categories: CategoryConfirmationDto[];
}

/** Tipos de arquivo aceitos no upload universal. */
export const PHOTO_CONTENT_PREFIXES = ["image/", "application/pdf"];

export async function postPreview(
  endpoint: string,
  file: File,
  restaurantId?: number | null
): Promise<ImportPreviewDto> {
  const runtimeConfig = useRuntimeConfig();
  const token = process.client ? localStorage.getItem("auth_token") : "";

  const formData = new FormData();
  formData.append("file", file);

  const params = new URLSearchParams();
  if (restaurantId && restaurantId > 0) params.set("restaurantId", String(restaurantId));

  const url = `${runtimeConfig.public.apiBase}${endpoint}?${params.toString()}`;
  return await $fetch<ImportPreviewDto>(url, {
    method: "POST",
    body: formData,
    headers: token ? { Authorization: `Bearer ${token}` } : {}
  });
}

export async function uploadFormDataFile(
  endpoint: string,
  file: File,
  strategy = "MergeByNameAndRestaurant",
  restaurantId?: number | null
): Promise<ImportResultDto> {
  const runtimeConfig = useRuntimeConfig();
  const token = process.client ? localStorage.getItem("auth_token") : "";

  const formData = new FormData();
  formData.append("file", file);

  const params = new URLSearchParams();
  params.set("strategy", strategy);
  if (restaurantId && restaurantId > 0) params.set("restaurantId", String(restaurantId));

  const url = `${runtimeConfig.public.apiBase}${endpoint}?${params.toString()}`;
  const resp = await $fetch<ImportResultDto>(url, {
    method: "POST",
    body: formData,
    headers: token
      ? {
          Authorization: `Bearer ${token}`
        }
      : {}
  });

  return resp;
}

/** Rota de importação definitiva para cada categoria detectada no preview. */
const CATEGORY_IMPORT_ENDPOINTS: Record<string, string> = {
  estoque: "/api/import/stock/products",
  fornecedores: "/api/import/suppliers"
};

/** Categorias ainda sem persistência no backend (exibidas como "em breve"). */
const CATEGORY_FUTURE = new Set(["financas"]);

export const importService = {
  /** Analisa uma planilha (dry-run) sem gravar nada. */
  previewSpreadsheet(file: File, restaurantId?: number | null) {
    return postPreview("/api/import/preview", file, restaurantId);
  },

  /** Extrai tabela de foto/PDF escaneado via visão computacional e gera confirmações (dry-run). */
  previewPhoto(file: File, restaurantId?: number | null) {
    return postPreview("/api/import/photo-preview", file, restaurantId);
  },

  /** Indica se a categoria pode ser importada agora (false = estrutura futura). */
  canConfirmCategory(category: string) {
    return !CATEGORY_FUTURE.has(category);
  },

  /** Executa a importação da categoria confirmada pelo usuário. */
  async confirmCategory(category: string, file: File, restaurantId?: number | null): Promise<ImportResultDto> {
    const endpoint = CATEGORY_IMPORT_ENDPOINTS[category];
    if (!endpoint) throw new Error(`Categoria '${category}' ainda não possui importação implementada.`);
    return uploadFormDataFile(endpoint, file, undefined, restaurantId);
  },

  uploadEstoqueCsv(file: File, strategy?: string, restaurantId?: number | null) {
    return uploadFormDataFile("/api/import/stock/products", file, strategy, restaurantId);
  },
  uploadFornecedoresCsv(file: File, strategy?: string, restaurantId?: number | null) {
    return uploadFormDataFile("/api/import/suppliers", file, strategy, restaurantId);
  },
  history(limit = 50, restaurantId?: number | null) {
    const params = new URLSearchParams();
    params.set("limit", String(limit));
    if (restaurantId && restaurantId > 0) params.set("restaurantId", String(restaurantId));
    return apiClientWithAuth<ImportAuditDto[]>(`/api/import/history?${params.toString()}`, {
      method: "GET"
    });
  },
  detail(importId: string) {
    return apiClientWithAuth<ImportAuditDetailDto>(`/api/import/history/${importId}`, {
      method: "GET"
    });
  }
};
