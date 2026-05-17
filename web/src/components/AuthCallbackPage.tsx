import { useEffect, useState } from 'react'
import { fetchMe, saveProfileSession } from '../lib/api'
import { Footer } from './Footer'
import { Header } from './Header'

type CallbackState =
  | { status: 'loading'; message: string }
  | { status: 'error'; message: string }

export function AuthCallbackPage() {
  const [state, setState] = useState<CallbackState>({ status: 'loading', message: 'Finishing sign in...' })

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const errorMessage = params.get('message')
    if (errorMessage) {
      setState({ status: 'error', message: errorMessage })
      return
    }

    async function syncProfileAndRedirect() {
      try {
        saveProfileSession(await fetchMe())
        window.location.replace('/')
      } catch {
        setState({
          status: 'error',
          message: 'Could not finish sign in. Please try again.'
        })
      }
    }

    void syncProfileAndRedirect()
  }, [])

  return (
    <main className="page-shell">
      <Header />

      <section className="auth-page">
        <div className="auth-copy">
          <p className="eyebrow">Google account</p>
          <h1>Sign in.</h1>
        </div>

        <section className="auth-card" aria-label="Google sign in status">
          <p className={`auth-message ${state.status === 'error' ? 'is-error' : ''}`}>
            {state.message}
          </p>

          {state.status === 'error' && (
            <div className="auth-callback-actions">
              <a className="action-button action-button-dark" href="/login">
                Try again
              </a>
              <a className="action-button" href="/register">
                Sign up with email
              </a>
            </div>
          )}
        </section>
      </section>

      <Footer />
    </main>
  )
}
