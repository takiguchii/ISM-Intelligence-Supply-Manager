import { apiClientWithAuth } from "~/services/api/client";

// Espelha ISM.Application.DTOs.Restaurant.IFoodConfigDtos (backend)
export interface IFoodConfigDto {
  hasCredentials: boolean;
  merchantId: string | null;
  clientIdLast4: string | null;
}

export interface UpdateIFoodConfigDto {
  clientId?: string | null;
  clientSecret?: string | null;
  merchantId?: string | null;
  clearCredentials?: boolean;
}

export interface TestIFoodConfigResultDto {
  success: boolean;
  tokenObtained: boolean;
  errorMessage: string | null;
  latencyMs: number;
}

// Espelha ISM.Application.DTOs.Menu.IFoodCatalogDtos (backend)
export interface IFoodCatalogStatusDto {
  totalCategories: number;
  categoriesSynced: number;
  totalDishes: number;
  dishesSynced: number;
}

export interface IFoodCatalogSyncResultDto {
  categoriesSynced: number;
  itemsSynced: number;
}

const BASE_URL = "/api/me/restaurant/ifood-config";

export const ifoodService = {
  getConfig: () => apiClientWithAuth<IFoodConfigDto>(BASE_URL),
  updateConfig: (dto: UpdateIFoodConfigDto) =>
    apiClientWithAuth<void>(BASE_URL, { method: "PUT", body: dto }),
  testConfig: () =>
    apiClientWithAuth<TestIFoodConfigResultDto>(`${BASE_URL}/test`, { method: "POST" }),

  async syncCatalog(): Promise<IFoodCatalogSyncResultDto> {
    return apiClientWithAuth<IFoodCatalogSyncResultDto>("/api/menu/ifood-catalog/sync", { method: "POST" });
  },
  async getCatalogStatus(): Promise<IFoodCatalogStatusDto> {
    return apiClientWithAuth<IFoodCatalogStatusDto>("/api/menu/ifood-catalog/status");
  }
};
