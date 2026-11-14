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

const BASE_URL = "/api/me/restaurant/ifood-config";

export const ifoodService = {
  getConfig: () => apiClientWithAuth<IFoodConfigDto>(BASE_URL),
  updateConfig: (dto: UpdateIFoodConfigDto) =>
    apiClientWithAuth<void>(BASE_URL, { method: "PUT", body: dto }),
  testConfig: () =>
    apiClientWithAuth<TestIFoodConfigResultDto>(`${BASE_URL}/test`, { method: "POST" })
};
