import { useEffect, useState } from 'react'
import { listarSalas } from '../api/client'
import type { Sala } from '../api/client'

export default function VitrineSalas() {
  const [busca, setBusca] = useState('')
  const [pagina, setPagina] = useState(1)
  const [dados, setDados] = useState<{
    itens: Sala[]
    total: number
    totalPaginas: number
  } | null>(null)
  const [carregando, setCarregando] = useState(true)

  useEffect(() => {
    const timer = setTimeout(async () => {
      setCarregando(true)
      try {
        const lista = await listarSalas(busca, pagina)
        setDados(lista)
      } catch {
        setDados({ itens: [], total: 0, totalPaginas: 0 })
      } finally {
        setCarregando(false)
      }
    }, 300) // debounce da busca

    return () => clearTimeout(timer)
  }, [busca, pagina])

  return (
    <>
      <div className="cabecalho-pagina">
        <h1>Encontre a sala ideal para o seu dia</h1>
        <p>Reuniões, sprints, entrevistas e eventos — reserve em segundos.</p>
      </div>

      <div className="barra-busca">
        <input
          type="search"
          placeholder="Buscar sala por nome… (ex.: Aurora)"
          value={busca}
          onChange={(e) => {
            setBusca(e.target.value)
            setPagina(1)
          }}
        />
      </div>

      {carregando && <p className="vazio">Carregando salas…</p>}

      {!carregando && dados && dados.itens.length === 0 && (
        <p className="vazio">Nenhuma sala encontrada para “{busca}”.</p>
      )}

      <div className="grade-salas">
        {dados?.itens.map((sala) => (
          <article className="cartao-sala" key={sala.id}>
            <span className="etiqueta-capacidade">
              👥 até {sala.capacidade} pessoas
            </span>
            <h3>{sala.nome}</h3>
            <p className="descricao">{sala.descricao}</p>
            <p className="recursos">{sala.recursos}</p>
            <p className="preco">
              R$ {sala.precoHora.toFixed(2).replace('.', ',')}{' '}
              <small>/ hora</small>
            </p>
          </article>
        ))}
      </div>

      {!carregando && dados && dados.totalPaginas > 1 && (
        <div className="paginacao">
          <button
            disabled={pagina <= 1}
            onClick={() => setPagina((p) => p - 1)}
          >
            ← Anterior
          </button>
          <span>
            Página {pagina} de {dados.totalPaginas} · {dados.total} salas
          </span>
          <button
            disabled={pagina >= dados.totalPaginas}
            onClick={() => setPagina((p) => p + 1)}
          >
            Próxima →
          </button>
        </div>
      )}
    </>
  )
}
