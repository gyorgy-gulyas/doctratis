import { Routes, Route } from "react-router-dom";
import RequireAuth from "./auth/RequireAuth";
import LoginPage from "./pages/LoginPage";
import HomePage from "./pages/HomePage";
import './App.css'
import { BFFRestClient } from "docratis.ts.api";
import { AdminRestClient } from "../../admin.ts.api/app";

function App() {
    const backendAddress_dockerLocal = "http://localhost/";

    BFFRestClient.getInstance().init(backendAddress_dockerLocal, "hu", "docratis", "v.0.1.0");
    AdminRestClient.getInstance().init(backendAddress_dockerLocal, "hu", "docratis", "v.0.1.0");

    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<RequireAuth />}>
                <Route path="/" element={<HomePage />} />
                {
                    /* ide jönnek a védett oldalak */
                }
            </Route>
            <Route path="*" element={<LoginPage />} />
        </Routes>
    )
}

export default App
