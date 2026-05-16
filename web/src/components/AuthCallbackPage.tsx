import { useEffect } from 'react'
import { fetchMe, saveProfileSession } from '../lib/api'

export function AuthCallbackPage() {
  useEffect(() => {
    async function syncProfileAndRedirect() {
      try {
        saveProfileSession(await fetchMe())
      } finally {
        window.location.replace('/')
      }
    }

    void syncProfileAndRedirect()
  }, [])

  return null
}
