import type { FormEvent } from 'react'
import { useEffect, useState } from 'react'
import { criarReserva, ErroApi, excluirReserva, listarSalas, minhasReservas } from '../api/client'
import type { Reserva, Sala } from '../api/client'

/** Converte "2026-11-20T14:00" (datetime-local, horário local) para ISO com offset. */
function paraIso(valorLocal: string): string {
  return new Date(valorLocal).toISOString()
}

export default function PainelCliente() {
  const [salas, setSalas] = useState<Sala[]>([])
  const [reservas, setReservas] = useState<Reserva[]>([])
  const [salaId, setSalaId] = useState<number | ''>('')
  const [data, setData] = useState('')
  const [horaInicio, setHoraInicio] = useState('')
  const [horaFim, setHoraFim] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [sucesso, setSucesso] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function recarregar() {
    const [listaSalas, listaReservas] = await Promise.all([
      listarSalas('', 1, 50),
      minhasReservas(),
    ])
    setSalas(listaSalas.itens)
    setReservas(listaReservas)
  }

  useEffect(() => {
    recarregar().catch((e) =>
      setErro(e instanceof ErroApi ? e.message : 'Falha ao carregar o painel.'),
    )
  }, [])

  async function aoReservar(evento: FormEvent) {
    evento.preventDefault()
    setErro(null)
    setSucesso(null)
    setEnviando(true)
    try {
      await criarReserva(Number(salaId), paraIso(`${data}T${horaInicio}`), paraIso(`${data}T${horaFim}`))
      setSucesso('Reserva criada com sucesso!')
      setSalaId('')
      setData('')
      setHoraInicio('')
      setHoraFim('')
      await recarregar()
    } catch (e) {
      // O 409 chega aqui com a mensagem limpa do ProblemDetails.
      setErro(e instanceof ErroApi ? e.message : 'Falha ao criar a reserva.')
    } finally {
      setEnviando(false)
    }
  }

  async function aoExcluir(id: number) {
    setErro(null)
    setSucesso(null)
    try {
      await excluirReserva(id)
      await recarregar()
    } catch (e) {
      setErro(e instanceof ErroApi ? e.message : 'Falha ao excluir a reserva.')
    }
  }

  function formatar(iso: string): string {
    return new Date(iso).toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  return (
    <>
      <div className="cabecalho-pagina">
        <h1>Minhas Reservas</h1>
        <p>Crie novas reservas e acompanhe as suas salas ocupadas.</p>
      </div>

      {erro && <div className="erro" role="alert">{erro}</div>}
      {sucesso && <div className="aviso-sucesso">{sucesso}</div>}

      <form className="formulario-reserva" onSubmit={aoReservar}>
        <div className="campo">
          <label htmlFor="sala">Sala</label>
          <select
            id="sala"
            required
            value={salaId}
            onChange={(e) => setSalaId(e.target.value ? Number(e.target.value) : '')}
          >
            <option value="" disabled>
              Selecione…
            </option>
            {salas.map((s) => (
              <option key={s.id} value={s.id}>
                {s.nome} (até {s.capacidade} pessoas)
              </option>
            ))}
          </select>
        </div>

        <div className="campo">
          <label htmlFor="data">Data</label>
          <input
            id="data"
            type="date"
            required
            value={data}
            onChange={(e) => setData(e.target.value)}
          />
        </div>

        <div className="campo">
          <label htmlFor="inicio">Início</label>
          <input
            id="inicio"
            type="time"
            required
            value={horaInicio}
            onChange={(e) => setHoraInicio(e.target.value)}
          />
        </div>

        <div className="campo">
          <label htmlFor="fim">Fim</label>
          <input
            id="fim"
            type="time"
            required
            value={horaFim}
            onChange={(e) => setHoraFim(e.target.value)}
          />
        </div>

        <button className="botao" disabled={enviando}>
          {enviando ? 'Reservando…' : 'Reservar'}
        </button>
      </form>

      {reservas.length === 0 ? (
        <p className="vazio">Você ainda não possui reservas.</p>
      ) : (
        <div className="lista-reservas">
          {reservas.map((r) => (
            <div className="item-reserva" key={r.id}>
              <div className="info">
                <strong>{r.salaNome}</strong>
                <span>
                  {formatar(r.inicio)} → {formatar(r.fim)}
                </span>
              </div>
              <button className="botao perigo" onClick={() => aoExcluir(r.id)}>
                Cancelar
              </button>
            </div>
          ))}
        </div>
      )}
    </>
  )
}
