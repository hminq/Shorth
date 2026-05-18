import { useState, type FormEvent } from 'react'
import { forgotPassword } from '../lib/api'
import { Button } from './Button'
import { Footer } from './Footer'
import { Header } from './Header'

type ForgotPasswordState =
  | { status: 'idle' }
  | { status: 'loading'; message: string }
  | { status: 'success'; message: string }
  | { status: 'error'; message: string }

type ForgotPasswordFieldErrors = Partial<Record<'email', string>>

const pendingPasswordResetEmailKey = 'shorth.pendingPasswordResetEmail'
const resetCodeAvailableAtKey = 'shorth.passwordResetCodeAvailableAt'
const resetCodeCooldownSeconds = 60
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function ForgotPasswordPage() {
  const [state, setState] = useState<ForgotPasswordState>({ status: 'idle' })
  const [fieldErrors, setFieldErrors] = useState<ForgotPasswordFieldErrors>({})
  const [email, setEmail] = useState(() => readInitialEmail())

  async function handleForgotPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const trimmedEmail = email.trim()
    const nextFieldErrors: ForgotPasswordFieldErrors = {}

    if (!trimmedEmail) {
      nextFieldErrors.email = 'Email is required.'
    } else if (!emailPattern.test(trimmedEmail)) {
      nextFieldErrors.email = 'Enter a valid email address.'
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors)
      setState({ status: 'idle' })
      return
    }

    setFieldErrors({})
    setState({ status: 'loading', message: 'Sending reset code...' })

    try {
      const result = await forgotPassword(trimmedEmail)
      localStorage.setItem(pendingPasswordResetEmailKey, trimmedEmail)
      localStorage.setItem(
        resetCodeAvailableAtKey,
        String(Date.now() + resetCodeCooldownSeconds * 1000)
      )
      setState({ status: 'success', message: result.message })
      window.setTimeout(() => {
        window.location.href = `/password-reset?email=${encodeURIComponent(trimmedEmail)}`
      }, 900)
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not send a reset code.'
      })
    }
  }

  return (
    <main className="page-shell">
      <Header />

      <section className="auth-page">
        <div className="auth-copy">
          <p className="eyebrow">Account recovery</p>
          <h1>Reset.</h1>
        </div>

        <section className="auth-card" aria-label="Forgot password form">
          <form className="auth-form" onSubmit={handleForgotPassword} noValidate>
            <label className="field-label" htmlFor="email">
              Email <span>*</span>
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              value={email}
              aria-invalid={Boolean(fieldErrors.email)}
              aria-describedby={fieldErrors.email ? 'email-error' : undefined}
              onChange={event => setEmail(event.target.value)}
            />
            {fieldErrors.email && (
              <p className="field-error" id="email-error">{fieldErrors.email}</p>
            )}

            <Button type="submit" disabled={state.status === 'loading'}>
              Send reset code
            </Button>
          </form>

          {state.status !== 'idle' && (
            <p className={`auth-message ${state.status === 'error' ? 'is-error' : ''}`}>
              {state.message}
            </p>
          )}

          <p className="auth-helper">
            Already have a code? <a href={buildResetHref(email)}>Reset password.</a>
          </p>
          <p className="auth-helper">
            Remembered it? <a href="/login">Sign in.</a>
          </p>
        </section>
      </section>

      <Footer />
    </main>
  )
}

function buildResetHref(email: string) {
  const trimmedEmail = email.trim()
  if (!trimmedEmail) {
    return '/password-reset'
  }

  return `/password-reset?email=${encodeURIComponent(trimmedEmail)}`
}

function readInitialEmail() {
  const queryEmail = new URLSearchParams(window.location.search).get('email')?.trim()
  if (queryEmail) {
    return queryEmail
  }

  return localStorage.getItem(pendingPasswordResetEmailKey) ?? ''
}
