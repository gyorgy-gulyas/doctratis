import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import { ProjectIF } from 'docratis.ts.api'
import './App.css'

function App() {
    const [count, setCount] = useState(0)
    const [name, setName] = useState('')
    const [description, setDescription] = useState('')
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [success, setSuccess] = useState<string | null>(null)

    // Új projekt létrehozása
    const addProject = async () => {
        if (!name) return alert('Adj meg egy nevet!')
        try {
            setLoading(true)
            setError(null)
            setSuccess(null)
            await ProjectIF.V1.createProject(name, description)
            setName('')
            setDescription('')
            setSuccess('Projekt sikeresen létrehozva!')
        } catch (err) {
            console.error(err)
            setError('Hiba a projekt létrehozásakor!')
        } finally {
            setLoading(false)
        }
    }

    return (
        <>
            <div>
                <a href="https://vite.dev" target="_blank">
                    <img src={viteLogo} className="logo" alt="Vite logo" />
                </a>
                <a href="https://react.dev" target="_blank">
                    <img src={reactLogo} className="logo react" alt="React logo" />
                </a>
            </div>
            <h1>Vite + React + Projects222</h1>

            <div className="card">
                <button onClick={() => setCount((count) => count + 1)}>
                    count is {count}
                </button>
                <p>
                    Edit <code>src/App.tsx</code> and save to test HMR
                </p>
            </div>

            <div className="card">
                <h2>Új projekt</h2>
                <input
                    type="text"
                    placeholder="Név"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                />
                <input
                    type="text"
                    placeholder="Leírás"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                />
                <button onClick={addProject} disabled={loading}>
                    Hozzáadás
                </button>
                {loading && <p>Projekt létrehozása...</p>}
                {error && <p style={{ color: 'red' }}>{error}</p>}
                {success && <p style={{ color: 'green' }}>{success}</p>}
            </div>
        </>
    )
}

export default App
