// =====================================================================
// Camada de comunicação com a API (fetch nativo + interceptor manual).
// - Injeta o Bearer token do localStorage em toda requisição autenticada.
// - Converte respostas ProblemDetails (401/403/404/409...) em exceções
//   com mensagens limpas para exibir na tela.
// =====================================================================

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export class ErroApi extends Error {
  readonly status: number
  constructor(status: number, mensagem: string) {
    super(mensagem)
    this.status = status
  }
}

async function extrairErro(resposta: Response): Promise<string> {
  try {
    const corpo = await resposta.json()
    // ProblemDetails (RFC 7807): { title, detail, ... }
    if (corpo && typeof corpo === 'object') {
      const mensagem = (corpo.detail ?? corpo.title) as string | undefined
      if (mensagem) return mensagem
    }
  } catch {
    /* corpo sem JSON — cai para a mensagem padrão */
  }
  return `Erro ${resposta.status}: ${resposta.statusText}`
}

/** Interceptor manual: injeta o token e normaliza erros ProblemDetails. */
async function requisitar<T>(caminho: string, opcoes: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('token')

  const cabecalhos: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((opcoes.headers as Record<string, string>) ?? {}),
  }
  if (token) cabecalhos['Authorization'] = `Bearer ${token}`

  const resposta = await fetch(`${BASE_URL}${caminho}`, {
    ...opcoes,
    headers: cabecalhos,
  })

  if (!resposta.ok) {
    const mensagem = await extrairErro(resposta)

    if (resposta.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('nome')
      localStorage.removeItem('papel')
      window.dispatchEvent(new Event('auth-mudou'))
    }

    throw new ErroApi(resposta.status, mensagem)
  }

  if (resposta.status === 204) return undefined as T
  return resposta.json() as Promise<T>
}

// ------------------------------ Tipos ------------------------------
export interface Sala {
  id: number
  nome: string
  descricao: string | null
  capacidade: number
  precoHora: number
  recursos: string
}

export interface ListaSalas {
  pagina: number
  tamanho: number
  total: number
  totalPaginas: number
  itens: Sala[]
}

export interface Reserva {
  id: number
  salaId: number
  salaNome: string
  inicio: string
  fim: string
  criadoEm: string
}

export interface RespostaAuth {
  token: string
  nome: string
  email: string
  papel: string
}

// ------------------------------ Endpoints ------------------------------
export function listarSalas(busca: string, pagina: number, tamanho = 9): Promise<ListaSalas> {
  const params = new URLSearchParams({
    pagina: String(pagina),
    tamanho: String(tamanho),
  })
  if (busca.trim()) params.set('busca', busca.trim())
  return requisitar<ListaSalas>(`/api/salas?${params}`)
}

export function obterSala(id: number): Promise<Sala> {
  return requisitar<Sala>(`/api/salas/${id}`)
}

export function login(email: string, senha: string): Promise<RespostaAuth> {
  return requisitar<RespostaAuth>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, senha }),
  })
}

export function registrar(nome: string, email: string, senha: string): Promise<RespostaAuth> {
  return requisitar<RespostaAuth>('/api/auth/registro', {
    method: 'POST',
    body: JSON.stringify({ nome, email, senha }),
  })
}

export function minhasReservas(): Promise<Reserva[]> {
  return requisitar<Reserva[]>('/api/reservas')
}

export function criarReserva(salaId: number, inicio: string, fim: string): Promise<Reserva> {
  return requisitar<Reserva>('/api/reservas', {
    method: 'POST',
    body: JSON.stringify({ salaId, inicio, fim }),
  })
}

export function excluirReserva(id: number): Promise<void> {
  return requisitar<void>(`/api/reservas/${id}`, { method: 'DELETE' })
}

// ------------------------------ Sessão ------------------------------
export function usuarioLogado(): boolean {
  return localStorage.getItem('token') !== null
}

export function sair(): void {
  localStorage.removeItem('token')
  localStorage.removeItem('nome')
  localStorage.removeItem('papel')
  window.dispatchEvent(new Event('auth-mudou'))
}

export function salvarSessao(auth: RespostaAuth): void {
  localStorage.setItem('token', auth.token)
  localStorage.setItem('nome', auth.nome)
  localStorage.setItem('papel', auth.papel)
  window.dispatchEvent(new Event('auth-mudou'))
}
