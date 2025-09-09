import type { AxiosInstance, AxiosHeaderValue } from "axios";
import type { ApiError } from "../../ApiError";
export declare class AdminRestClient {
    private static instance;
    axios: AxiosInstance;
    private constructor();
    static getInstance(): AdminRestClient;
    /**
     * Átállítja a baseURL-t és a timeout-ot a meglévő Axios példányon
     */
    init(baseURL: string, client_language: string, app_name: string, app_version: string): void;
    /**
     * Elérhetővé teszi a belső Axios példányt
     */
    get apiClient(): AxiosInstance;
    getRequestHeaders(operation: string): Record<string, AxiosHeaderValue>;
    mapApiError(error: any, operation: string): ApiError;
}
//# sourceMappingURL=AdminRestClient.d.ts.map