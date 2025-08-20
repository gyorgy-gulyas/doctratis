export enum AuthModes {
    EmailPassword = "email",
    KAU = "kau",
    AD = "ad",
}

// login utáni következő lépés
export type LoginNext =
    | { kind: "ok"; accessToken: string }
    | { kind: "twofactor"; accessToken: string }
    | { kind: "passwordChange"; }
    | { kind: "redirect"; url: string }
    | { kind: "error"; message: string };

export type EmailPasswordPayload = { email: string; password: string };
export type ADPayload = { username: string; password: string };
export type KAUPayload = { frontendUrl: string };

export type LoginPayload =
    | { provider: AuthModes.EmailPassword; data: EmailPasswordPayload }
    | { provider: AuthModes.KAU; data: KAUPayload }
    | { provider: AuthModes.AD; data: ADPayload };