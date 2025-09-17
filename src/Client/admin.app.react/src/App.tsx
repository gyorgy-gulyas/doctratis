import { Routes, Route } from "react-router-dom";
import { useEffect } from "react";
import "./App.css";

import { BFFRestClient } from "@docratis/bff.api.package.ts";
import { AdminRestClient } from "@docratis/admin.api.package.ts";

import RequireAuth from "./auth/RequireAuth";
import HomePage from "./pages/HomePage";

import AuthLayout from "./layouts/AuthLayout";
import MainLayout from "./layouts/MainLayout";
import LoginMethodPage from "./pages/login/LoginMethodPage";
import EmailPasswordLoginPage from "./pages/login/EmailPasswordLoginPage";
import TwoFactorPage from "./pages/login/TwoFactorPage";
import PasswordChangePage from "./pages/login/PasswordChangePage";
import KAULoginPage from "./pages/login/KAULoginPage";
import ADLoginPage from "./pages/login/ADLoginPage";
import ForgotPasswordPage from "./pages/login/ForgotPassword";
import UserList from "./pages/admin/UserList";

function App() {
    const backendAddress_dockerLocal = "http://localhost:31000/";

    // Init only once
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
            {/* Login flow  */}
            <Route element={<AuthLayout />}>
                <Route path="/login" element={<LoginMethodPage />} />
                <Route path="/login/email" element={<EmailPasswordLoginPage />} />
                <Route path="/login/kau" element={<KAULoginPage />} />
                <Route path="/login/ad" element={<ADLoginPage />} />
                <Route path="/login/2fa" element={<TwoFactorPage />} />
                <Route path="/login/password-change" element={<PasswordChangePage />} />
                <Route path="/login/forgot-password" element={<ForgotPasswordPage />} />
            </Route>
            
            {/* Protected paths */}
            <Route element={<RequireAuth />}>
                <Route element={<MainLayout />}>
                    <Route path="/" element={<HomePage />} />
                    <Route path="/admin/users" element={<UserList />} />
                </Route>
            </Route>

            {/* Fallback */}
            <Route element={<AuthLayout />}>
                <Route path="*" element={<LoginMethodPage />} />
            </Route>
        </Routes>
    )
}

export default App
