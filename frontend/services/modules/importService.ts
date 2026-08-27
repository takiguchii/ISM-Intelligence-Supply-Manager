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

export const importService = {
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