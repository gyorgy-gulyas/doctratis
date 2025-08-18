import { Routes, Route } from "react-router-dom";
import RequireAuth from "./auth/RequireAuth";
import LoginPage from "./pages/LoginPage";
import HomePage from "./pages/HomePage";
import './App.css'

function App() {
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
