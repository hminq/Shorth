export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5050'

type CreateLinkResponse = {
  id: string
  slug: string
  destinationUrl: string
  createdAt: string
  expiresAt: string | null
}

export type LoginResponse = {
  accessToken: string
  userId: string
  email: string
  displayName: string
}

export type AuthSession = {
  userId: string
  email: string
  displayName: string
}

type GoogleLoginUrlResponse = {
  authorizationUrl: string
}

type ProblemDetails = {
  title?: string
  detail?: string
  status?: number
}

export async function createAnonymousLink(
  destinationUrl: string,
  captchaToken?: string
): Promise<CreateLinkResponse> {
  const response = await safeFetch('/api/links', {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ destinationUrl, captchaToken })
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  return await response.json() as CreateLinkResponse
}

export async function loginLocal(email: string, password: string): Promise<LoginResponse> {
  const response = await safeFetch('/api/login/local', {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ email, password })
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  return await response.json() as LoginResponse
}

export async function getGoogleLoginUrl(): Promise<string> {
  const response = await safeFetch('/api/login/google', {
    credentials: 'include'
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  const payload = await response.json() as GoogleLoginUrlResponse
  return payload.authorizationUrl
}

export function saveAuthSession(session: LoginResponse) {
  const safeSession: AuthSession = {
    userId: session.userId,
    email: session.email,
    displayName: session.displayName
  }

  localStorage.setItem('shorth.auth', JSON.stringify(safeSession))
}

export function getAuthSession(): AuthSession | null {
  const rawSession = localStorage.getItem('shorth.auth')
  if (!rawSession) {
    return null
  }

  try {
    return JSON.parse(rawSession) as AuthSession
  } catch {
    localStorage.removeItem('shorth.auth')
    return null
  }
}

export async function logout() {
  const response = await safeFetch('/api/logout', {
    method: 'POST',
    credentials: 'include'
  })

  localStorage.removeItem('shorth.auth')

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }
}

export function shortUrl(slug: string) {
  return `${API_BASE_URL}/${slug}`
}

async function readProblemMessage(response: Response) {
  if (response.status >= 500) {
    return 'Something went wrong on our side. Please try again in a moment.'
  }

  let problem: ProblemDetails | null = null

  try {
    problem = await response.json() as ProblemDetails
  } catch {
    problem = null
  }

  return problem?.detail || problem?.title || `Request failed with status ${response.status}.`
}

async function safeFetch(path: string, init?: RequestInit) {
  try {
    return await fetch(`${API_BASE_URL}${path}`, init)
  } catch {
    throw new Error('Could not reach Shorth right now. Please check your connection and try again.')
  }
}
