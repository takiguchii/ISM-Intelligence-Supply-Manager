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
