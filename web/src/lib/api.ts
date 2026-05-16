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
  avatarUrl: string | null
  hasPassword: boolean
}

export type UserProfile = AuthSession

type MeResponse = UserProfile

export type UpdateProfilePayload = {
  displayName?: string
  avatarUrl?: string
  currentPassword?: string
  newPassword?: string
}

export type CreateUploadPayload = {
  fileType: 'profile_image'
  fileName: string
  contentType: string
  fileSizeBytes: number
}

export type CreateUploadResponse = {
  uploadUrl: string
  fields: Record<string, string>
  objectKey: string
  publicUrl: string
  maxSizeBytes: number
  expiresAt: string
}

export type UserLinkSummary = {
  id: string
  slug: string
  destinationUrl: string
  clickCount: number
  lastClickedAt: string | null
  createdAt: string
  expiresAt: string | null
  isDisabled: boolean
}

export type UserLinksResponse = {
  page: number
  pageSize: number
  hasNextPage: boolean
  items: UserLinkSummary[]
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
    if (response.status === 401) {
      throw new Error('Email or password is incorrect.')
    }

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

export async function fetchMe(): Promise<AuthSession> {
  const response = await safeFetch('/api/profile', {
    credentials: 'include'
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  return await response.json() as MeResponse
}

export async function updateProfile(payload: UpdateProfilePayload): Promise<UserProfile> {
  const response = await safeFetch('/api/profile', {
    method: 'PATCH',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  return await response.json() as UserProfile
}

export async function createUpload(payload: CreateUploadPayload): Promise<CreateUploadResponse> {
  const response = await safeFetch('/api/upload', {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  return await response.json() as CreateUploadResponse
}

export async function uploadFileToS3(upload: CreateUploadResponse, file: File): Promise<void> {
  const form = new FormData()

  for (const [key, value] of Object.entries(upload.fields)) {
    form.append(key, value)
  }

  form.append('file', file)

  const response = await fetch(upload.uploadUrl, {
    method: 'POST',
    body: form
  })

  if (!response.ok) {
    throw new Error('Could not upload your profile image. Please try again.')
  }
}

export async function fetchUserLinks(page = 1): Promise<UserLinksResponse> {
  const response = await safeFetch(`/api/links?page=${page}`, {
    credentials: 'include'
  })

  if (!response.ok) {
    throw new Error(await readProblemMessage(response))
  }

  return await response.json() as UserLinksResponse
}

export function saveAuthSession(session: LoginResponse) {
  const safeSession: AuthSession = {
    userId: session.userId,
    email: session.email,
    displayName: session.displayName,
    avatarUrl: null,
    hasPassword: true
  }

  localStorage.setItem('shorth.auth', JSON.stringify(safeSession))
}

export function saveProfileSession(session: AuthSession) {
  localStorage.setItem('shorth.auth', JSON.stringify(session))
  window.dispatchEvent(new CustomEvent<AuthSession>('shorth:auth-session-updated', { detail: session }))
}

export function getAuthSession(): AuthSession | null {
  const rawSession = localStorage.getItem('shorth.auth')
  if (!rawSession) {
    return null
  }

  try {
    const parsed = JSON.parse(rawSession) as Partial<AuthSession>
    if (!parsed.userId || !parsed.email || !parsed.displayName) {
      localStorage.removeItem('shorth.auth')
      return null
    }

    return {
      userId: parsed.userId,
      email: parsed.email,
      displayName: parsed.displayName,
      avatarUrl: parsed.avatarUrl ?? null,
      hasPassword: parsed.hasPassword ?? false
    }
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
  if (response.status === 401) {
    return 'Please sign in again to continue.'
  }

  if (response.status === 403) {
    return 'You do not have permission to do that.'
  }

  if (response.status === 404) {
    return 'We could not find what you requested.'
  }

  if (response.status >= 500) {
    return 'Something went wrong on our side. Please try again in a moment.'
  }

  let problem: ProblemDetails | null = null

  try {
    problem = await response.json() as ProblemDetails
  } catch {
    problem = null
  }

  return problem?.detail || problem?.title || 'Something went wrong. Please try again.'
}

async function safeFetch(path: string, init?: RequestInit) {
  try {
    return await fetch(`${API_BASE_URL}${path}`, init)
  } catch {
    throw new Error('Could not reach Shorth right now. Please check your connection and try again.')
  }
}
