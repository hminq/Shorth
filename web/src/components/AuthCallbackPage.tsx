import { useEffect } from 'react'

export function AuthCallbackPage() {
  useEffect(() => {
    window.location.replace('/')
  }, [])

  return null
}
