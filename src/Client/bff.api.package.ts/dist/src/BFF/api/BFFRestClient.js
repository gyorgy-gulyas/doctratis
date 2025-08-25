import axios from "axios";
import { v4 as uuidv4 } from "uuid"; // ha nincs, npm install uuid
export class BFFRestClient {
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
        if (!BFFRestClient.instance) {
            BFFRestClient.instance = new BFFRestClient();
        }
        return BFFRestClient.instance;
    }
    /**
     * Átállítja a baseURL-t és a timeout-ot a meglévő Axios példányon
     */
    init(baseURL, client_language, app_name, app_version) {
        this.axios.defaults.baseURL = baseURL;
        console.log(`[BFFRestClient] baseURL set to: ${baseURL}`);
        this.axios.defaults.headers.common["client-language"] = client_language;
        this.axios.defaults.headers.common["client-application"] = app_name;
        this.axios.defaults.headers.common["client-version"] = app_version;
        this.axios.defaults.headers.common["client-tz-offset"] = new Date().getTimezoneOffset();
    }
    /**
     * - Authorization: Bearer <token>
     * - identity id/name headerek (alapértelmezett nevek testreszabhatók)
     */
    setAuthorization(bearerToken, userId, userName) {
        // Authorization
        this.axios.defaults.headers.common["Authorization"] = `Bearer ${bearerToken}`;
        this.axios.defaults.headers.common["identity-id"] = userId;
        this.axios.defaults.headers.common["identity-name"] = userName;
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
        var _a, _b;
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
                message: `API Error in ${operation}: ${((_b = error.response.data) === null || _b === void 0 ? void 0 : _b.message) || error.message}`,
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
//# sourceMappingURL=BFFRestClient.js.map