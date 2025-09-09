import axios from "axios";
import { v4 as uuidv4 } from "uuid"; // ha nincs, npm install uuid
export class AdminRestClient {
    constructor() {
        this.axios = axios.create({
            baseURL: "/", // Default
            timeout: 5000,
            headers: {
                "Content-Type": "application/json",
            },
        });
    }
    static getInstance() {
        if (!AdminRestClient.instance) {
            AdminRestClient.instance = new AdminRestClient();
        }
        return AdminRestClient.instance;
    }
    /**
     * Átállítja a baseURL-t és a timeout-ot a meglévő Axios példányon
     */
    init(baseURL, client_language, app_name, app_version) {
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
    get apiClient() {
        return this.axios;
    }
    getRequestHeaders(operation) {
        return Object.assign({ "request-id": uuidv4(), "call-stack": operation }, this.axios.defaults.headers.common);
    }
    mapApiError(error, operation) {
        var _a;
        if ((_a = error.response) === null || _a === void 0 ? void 0 : _a.data) {
            return {
                status: error.response.data.status,
                message: error.response.data.messageText,
                additionalInformation: error.response.data.additionalInformation,
            };
        }
        else if (error.response) {
            return {
                status: error.response.status,
                message: `API Error in ${operation}: ${error.message}`,
                additionalInformation: JSON.stringify(error.response.data),
            };
        }
        else if (error.request) {
            return {
                status: 500,
                message: `No response received in ${operation}`,
                additionalInformation: error.message,
            };
        }
        else {
            return {
                status: 500,
                message: `Unexpected error in ${operation}: ${error.message}`,
            };
        }
    }
}
//# sourceMappingURL=AdminRestClient.js.map