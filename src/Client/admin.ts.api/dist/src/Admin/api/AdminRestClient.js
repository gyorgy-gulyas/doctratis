"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.AdminRestClient = void 0;
const axios_1 = __importDefault(require("axios"));
const uuid_1 = require("uuid"); // ha nincs, npm install uuid
class AdminRestClient {
    constructor() {
        this.axios = axios_1.default.create({
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
        return Object.assign({ "request-id": (0, uuid_1.v4)(), "call-stack": operation }, this.axios.defaults.headers.common);
    }
    mapApiError(error, operation) {
        var _a;
        if (error.response) {
            return {
                status: error.response.status,
                message: `API Error in ${operation}: ${((_a = error.response.data) === null || _a === void 0 ? void 0 : _a.message) || error.message}`,
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
exports.AdminRestClient = AdminRestClient;
//# sourceMappingURL=AdminRestClient.js.map