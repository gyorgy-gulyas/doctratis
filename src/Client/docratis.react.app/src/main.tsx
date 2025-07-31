import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { BFFRestClient } from 'docratis.ts.api'

// Inicializálás az app indulásakor
BFFRestClient.getInstance().init(
    'http://localhost:52620/',
    'HU',
    'doctratis.react.app',
    '0.1'
)

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
