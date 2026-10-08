import { Link } from 'react-router'
import { sair } from '../api/client'

interface Props {
  logado: boolean
  papel: string | null
}

export default function BarraNavegacao({ logado, papel }: Props) {
  return (
    <nav className="navbar">
      <div className="navbar-interna">
        <Link to="/" className="logotipo">
          Espaços Bertuloso <span>| Coworking</span>
        </Link>

        <div className="navbar-links">
          <Link to="/">Salas</Link>
          {logado && <Link to="/painel">Minhas Reservas</Link>}

          {!logado ? (
            <>
              <Link to="/login">Entrar</Link>
              <Link to="/cadastro" className="botao">
                Criar conta
              </Link>
            </>
          ) : (
            <>
              <span style={{ color: 'var(--cor-texto-suave)', fontSize: '0.9rem' }}>
                {localStorage.getItem('nome')}
                {papel === 'admin' ? ' (admin)' : ''}
              </span>
              <button className="botao secundario" onClick={sair}>
                Sair
              </button>
            </>
          )}
        </div>
      </div>
    </nav>
  )
}
