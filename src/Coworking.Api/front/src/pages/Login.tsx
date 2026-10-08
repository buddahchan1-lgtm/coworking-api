import type { FormEvent } from 'react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { ErroApi, login, salvarSessao } from '../api/client'

export default function Login() {
  const navegar = useNavigate()
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function aoEnviar(evento: FormEvent) {
    evento.preventDefault()
    setErro(null)
    setEnviando(true)
    try {
      const resposta = await login(email, senha)
      salvarSessao(resposta)
      navegar('/painel')
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : 'Falha ao entrar. Tente novamente.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <form className="formulario" onSubmit={aoEnviar}>
      <h1>Entrar</h1>

      {erro && <div className="erro">{erro}</div>}

      <div className="campo">
        <label htmlFor="email">E-mail</label>
        <input
          id="email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="voce@exemplo.com"
        />
      </div>

      <div className="campo">
        <label htmlFor="senha">Senha</label>
        <input
          id="senha"
          type="password"
          required
          value={senha}
          onChange={(e) => setSenha(e.target.value)}
          placeholder="••••••••"
        />
      </div>

      <button className="botao" disabled={enviando}>
        {enviando ? 'Entrando…' : 'Entrar'}
      </button>

      <p className="texto-rodape-form">
        Ainda não tem conta? <Link to="/cadastro">Cadastre-se</Link>
      </p>
    </form>
  )
}
