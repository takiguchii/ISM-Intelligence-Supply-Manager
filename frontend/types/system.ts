export interface EnabledModule {
  id: number;
  name: string;
  slug: string;
  description: string;
  sortOrder: number;
}

export interface SystemStatus {
  applicationName: string;
  environment: string;
  utcTimestamp: string;
  databaseConnected: boolean;
  enabledModuleCount: number;
  firstEnabledModule: EnabledModule | null;
  enabledModules: EnabledModule[];
}

export type AlertSeverity = "Info" | "Warning" | "Critical";

export interface SystemAlertResponse {
  id: number;
  restaurantId: number;
  alertType: string;
  severity: AlertSeverity;
  title: string;
  message: string;
  referenceEntityType?: string | null;
  referenceEntityId?: number | null;
  payloadSerializedJson?: string | null;
  isRead: boolean;
  isDismissed: boolean;
  generatedAtUtc: string;
  readAtUtc?: string | null;
  dismissedAtUtc?: string | null;
  dismissedByUserId?: number | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

