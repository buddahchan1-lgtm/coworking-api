import type { FormEvent } from 'react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { ErroApi, registrar, salvarSessao } from '../api/client'

export default function Cadastro() {
  const navegar = useNavigate()
  const [nome, setNome] = useState('')
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function aoEnviar(evento: FormEvent) {
    evento.preventDefault()
    setErro(null)
    setEnviando(true)
    try {
      const resposta = await registrar(nome, email, senha)
      salvarSessao(resposta)
      navegar('/painel')
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : 'Falha no cadastro. Tente novamente.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <form className="formulario" onSubmit={aoEnviar}>
      <h1>Criar conta</h1>

      {erro && <div className="erro">{erro}</div>}

      <div className="campo">
        <label htmlFor="nome">Nome completo</label>
        <input
          id="nome"
          required
          minLength={3}
          value={nome}
          onChange={(e) => setNome(e.target.value)}
          placeholder="Maria Souza"
        />
      </div>

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
        <label htmlFor="senha">Senha (mínimo 6 caracteres)</label>
        <input
          id="senha"
          type="password"
          required
          minLength={6}
          value={senha}
          onChange={(e) => setSenha(e.target.value)}
          placeholder="••••••••"
        />
      </div>

      <button className="botao" disabled={enviando}>
        {enviando ? 'Criando conta…' : 'Criar conta'}
      </button>

      <p className="texto-rodape-form">
        Já tem conta? <Link to="/login">Entrar</Link>
      </p>
    </form>
  )
}
