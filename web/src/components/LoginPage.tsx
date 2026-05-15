import { useState, type FormEvent } from 'react'
import { getGoogleLoginUrl, loginLocal, saveAuthSession } from '../lib/api'
import { Button } from './Button'
import { Footer } from './Footer'
import { Header } from './Header'

type LoginState =
  | { status: 'idle' }
  | { status: 'loading'; message: string }
  | { status: 'success'; message: string }
  | { status: 'error'; message: string }

export function LoginPage() {
  const [state, setState] = useState<LoginState>({ status: 'idle' })

  async function handleLocalLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const formData = new FormData(event.currentTarget)
    const email = String(formData.get('email') ?? '').trim()
    const password = String(formData.get('password') ?? '')

    if (!email || !password) {
      setState({ status: 'error', message: 'Enter your email and password.' })
      return
    }

    setState({ status: 'loading', message: 'Signing in...' })

    try {
      const session = await loginLocal(email, password)
      saveAuthSession(session)
      window.location.href = '/'
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not sign in.'
      })
    }
  }

  async function handleGoogleLogin() {
    setState({ status: 'loading', message: 'Opening Google sign in...' })

    try {
      window.location.href = await getGoogleLoginUrl()
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not start Google sign in.'
      })
    }
  }

  return (
    <main className="page-shell">
      <Header />

      <section className="auth-page">
        <div className="auth-copy">
          <p className="eyebrow">Account access</p>
          <h1>Sign in.</h1>
        </div>

        <section className="auth-card" aria-label="Sign in form">
          <form className="auth-form" onSubmit={handleLocalLogin}>
            <label className="field-label" htmlFor="email">
              Email <span>*</span>
            </label>
            <input id="email" name="email" type="email" autoComplete="email" required />

            <label className="field-label" htmlFor="password">
              Password <span>*</span>
            </label>
            <input id="password" name="password" type="password" autoComplete="current-password" required />

            <Button type="submit" disabled={state.status === 'loading'}>
              Sign in
            </Button>
          </form>

          <div className="auth-divider" aria-hidden="true">
            Or
          </div>

          <button className="action-button auth-google-button" type="button" onClick={() => void handleGoogleLogin()}>
            <GoogleLogo />
            Continue with Google
          </button>

          {state.status !== 'idle' && (
            <p className={`auth-message ${state.status === 'error' ? 'is-error' : ''}`}>
              {state.message}
            </p>
          )}

          <p className="auth-helper">
            New here? <a href="/register">Create an account.</a>
          </p>
        </section>
      </section>

      <Footer />
    </main>
  )
}

function GoogleLogo() {
  return (
    <svg className="google-logo" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path fill="#4285F4" d="M21.6 12.23c0-.79-.07-1.55-.2-2.23H12v4.22h5.38a4.6 4.6 0 0 1-1.99 3.02v2.51h3.22c1.89-1.74 2.99-4.3 2.99-7.52Z" />
      <path fill="#34A853" d="M12 22c2.7 0 4.96-.89 6.61-2.25l-3.22-2.51c-.9.6-2.04.95-3.39.95-2.6 0-4.8-1.76-5.59-4.12H3.08v2.59A9.99 9.99 0 0 0 12 22Z" />
      <path fill="#FBBC05" d="M6.41 14.07A6 6 0 0 1 6.09 12c0-.72.12-1.42.32-2.07V7.34H3.08A9.99 9.99 0 0 0 2 12c0 1.61.39 3.14 1.08 4.49l3.33-2.42Z" />
      <path fill="#EA4335" d="M12 5.81c1.47 0 2.79.51 3.83 1.5l2.86-2.86C16.95 2.83 14.7 2 12 2a9.99 9.99 0 0 0-8.92 5.34l3.33 2.59C7.2 7.57 9.4 5.81 12 5.81Z" />
    </svg>
  )
}
