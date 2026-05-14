export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5050'

type CreateLinkResponse = {
  id: string
  slug: string
  destinationUrl: string
  createdAt: string
  expiresAt: string | null
}

type ProblemDetails = {
  title?: string
  detail?: string
  status?: number
}

export async function createAnonymousLink(destinationUrl: string): Promise<CreateLinkResponse> {
  const response = await fetch(`${API_BASE_URL}/api/links`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ destinationUrl })
  })

  if (!response.ok) {
    let problem: ProblemDetails | null = null

    try {
      problem = await response.json() as ProblemDetails
    } catch {
      problem = null
    }

    throw new Error(problem?.detail || problem?.title || `Request failed with status ${response.status}.`)
  }

  return await response.json() as CreateLinkResponse
}

export function shortUrl(slug: string) {
  return `${API_BASE_URL}/${slug}`
}
