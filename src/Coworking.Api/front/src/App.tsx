import { Navigate, Route, Routes } from 'react-router'
import { useEffect, useState } from 'react'
import { usuarioLogado } from './api/client'
import BarraNavegacao from './components/BarraNavegacao'
import Rodape from './components/Rodape'
import VitrineSalas from './pages/VitrineSalas'
import Login from './pages/Login'
import Cadastro from './pages/Cadastro'
import PainelCliente from './pages/PainelCliente'

export default function App() {
  const [logado, setLogado] = useState(usuarioLogado())
  const [papel, setPapel] = useState<string | null>(localStorage.getItem('papel'))

  useEffect(() => {
    const aoMudar = () => {
      setLogado(usuarioLogado())
      setPapel(localStorage.getItem('papel'))
    }
    window.addEventListener('auth-mudou', aoMudar)
    return () => window.removeEventListener('auth-mudou', aoMudar)
  }, [])

  return (
    <div className="aplicacao">
      <BarraNavegacao logado={logado} papel={papel} />

      <main className="conteudo">
        <Routes>
          <Route path="/" element={<VitrineSalas />} />
          <Route path="/login" element={<Login />} />
          <Route path="/cadastro" element={<Cadastro />} />
          <Route
            path="/painel"
            element={logado ? <PainelCliente /> : <Navigate to="/login" replace />}
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>

      <Rodape />
    </div>
  )
}
