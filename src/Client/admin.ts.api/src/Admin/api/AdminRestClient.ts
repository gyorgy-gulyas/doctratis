import axios from "axios";
import type { AxiosInstance, AxiosHeaderValue } from "axios";
import { v4 as uuidv4 } from "uuid"; // ha nincs, npm install uuid
import type { ApiError } from "../../ApiError";

export class AdminRestClient {
    private static instance: AdminRestClient;
    public axios: AxiosInstance;

    private constructor() {
        this.axios = axios.create({
            baseURL: "/", // Default
            timeout: 5000,
            headers: {
                "Content-Type": "application/json",
            },
        });
    }

    public static getInstance(): AdminRestClient {
        if (!AdminRestClient.instance) {
            AdminRestClient.instance = new AdminRestClient();
        }
        return AdminRestClient.instance;
    }

    /**
     * Átállítja a baseURL-t és a timeout-ot a meglévő Axios példányon
     */
    public init(baseURL: string, client_language: string, app_name: string, app_version: string): void {
        this.axios.defaults.baseURL = baseURL;
        console.log(`[AdminRestClient] baseURL set to: ${baseURL}`);

        this.axios.defaults.headers.common["client-language"] = client_language;
        this.axios.defaults.headers.common["client-application"] = app_name;
        this.axios.defaults.headers.common["client-version"] = app_version;
        this.axios.defaults.headers.common["client-tz-offset"] = new Date().getTimezoneOffset();
    }

    /**
     * Elérhetővé teszi a belső Axios példányt
     */
    public get apiClient(): AxiosInstance {
        return this.axios;
    }

    public getRequestHeaders(operation: string): Record<string, AxiosHeaderValue> {
        return {
            "request-id": uuidv4(), // egyedi ID minden híváshoz
            "call-stack": operation,
            // Ha kell, átadjuk a default headereket is
            ...this.axios.defaults.headers.common,
        };
    }

    public mapApiError(error: any, operation: string): ApiError {
        if (error.response?.data) {
            return {
                status: error.response.data.status,
                message: error.response.data.messageText,
                additionalInformation: error.response.data.additionalInformation,
            };
        } else if (error.response) {
            return {
                status: error.response.status,
                message: `API Error in ${operation}: ${error.message}`,
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
