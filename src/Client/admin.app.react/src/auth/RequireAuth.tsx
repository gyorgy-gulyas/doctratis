import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./AuthContext";

export default function RequireAuth() {
    const { isAuth } = useAuth();
    return isAuth
        ? <Outlet />
        : <Navigate to="/login" replace />;
}