import axios, { AxiosInstance } from "axios";

export class BFFRestClient {
  private static instance: BFFRestClient;
  private _apiClient: AxiosInstance;

  private constructor() {
    this._apiClient = axios.create({
      baseURL: "/", // Default
      timeout: 5000,
      headers: {
        "Content-Type": "application/json",
      },
    });
  }

  public static getInstance(): BFFRestClient {
    if (!BFFRestClient.instance) {
      BFFRestClient.instance = new BFFRestClient();
    }
    return BFFRestClient.instance;
  }

  /**
   * Átállítja a baseURL-t és a timeout-ot a meglévő Axios példányon
   */
  public init(baseURL: string, client_language:string, app_name:string, app_version:string): void {
    this._apiClient.defaults.baseURL = baseURL;
    console.log(`[BFFRestClient] baseURL set to: ${baseURL}`);

    this._apiClient.defaults.headers.common["client-language"] = client_language;
    this._apiClient.defaults.headers.common["client-application"] = app_name;
    this._apiClient.defaults.headers.common["client-version"] = app_version;
    this._apiClient.defaults.headers.common["client-tz-offset"] = new Date().getTimezoneOffset();
  }

  /**
   * Elérhetővé teszi a belső Axios példányt
   */
  public get apiClient(): AxiosInstance {
    return this._apiClient;
  }

  public mapApiError(error: any, operation: string): ApiError {
    if (error.response) {
        return {
            status: error.response.status,
            message: `API Error in ${operation}: ${error.response.data?.message || error.message}`,
            additionalInformation: JSON.stringify(error.response.data),
        };
    } else if (error.request) {
        return {
            status: 500,
            message: `No response received in ${operation}`,
            additionalInformation: error.message,
        };
    } else {
        return {
            status: 500,
            message: `Unexpected error in ${operation}: ${error.message}`,
        };
    }
}
}
