import { Routes, Route } from "react-router-dom";
import { useEffect } from "react";
import "./App.css";

import { BFFRestClient } from "docratis.ts.api";
import { AdminRestClient } from "../../admin.ts.api/app";

import RequireAuth from "./auth/RequireAuth";
import HomePage from "./pages/HomePage";

import LoginMethodPage from "./pages/login/LoginMethodPage";
import EmailPasswordLoginPage from "./pages/login/EmailPasswordLoginPage";
import TwoFactorPage from "./pages/login/TwoFactorPage";
import PasswordChangePage from "./pages/login/PasswordChangePage";
import KAULoginPage from "./pages/login/KAULoginPage";
import ADLoginPage from "./pages/login/ADLoginPage";

function App() {
    const backendAddress_dockerLocal = "http://localhost/";

    // inicializálás csak egyszer
    useEffect(() => {
        BFFRestClient.getInstance().init(
            backendAddress_dockerLocal,
            "hu",
            "docratis",
            "v.0.1.0"
        );
        AdminRestClient.getInstance().init(
            backendAddress_dockerLocal,
            "hu",
            "docratis",
            "v.0.1.0"
        );
    }, []);

    return (
        <Routes>
            {/* Login flow gyökér */}
            <Route path="/login" element={<LoginMethodPage />} />
            <Route path="/login/email" element={<EmailPasswordLoginPage />} />
            <Route path="/login/kau" element={<KAULoginPage />} />
            <Route path="/login/ad" element={<ADLoginPage />} />
            <Route path="/login/2fa" element={<TwoFactorPage />} />
            <Route path="/login/password-change" element={<PasswordChangePage />} />

            {/* Védett útvonalak */}
            <Route element={<RequireAuth />}>
                <Route path="/" element={<HomePage />} />
                {/* ide jöhetnek további védett oldalak */}
            </Route>

            {/* Fallback */}
            <Route path="*" element={<LoginMethodPage />} />
        </Routes>
    )
}

export default App
