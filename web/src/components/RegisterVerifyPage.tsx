import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { PaperPlaneTilt } from '@phosphor-icons/react'
import { resendVerificationOtp, verifyEmailOtp } from '../lib/api'
import { Button } from './Button'
import { Footer } from './Footer'
import { Header } from './Header'

type VerifyState =
  | { status: 'idle' }
  | { status: 'loading'; message: string }
  | { status: 'success'; message: string }
  | { status: 'error'; message: string }

type VerifyFieldErrors = Partial<Record<'otpCode', string>>

const pendingVerificationEmailKey = 'shorth.pendingVerificationEmail'
const resendAvailableAtKey = 'shorth.verificationResendAvailableAt'
const resendCooldownSeconds = 60
const otpPattern = /^\d{6}$/

export function RegisterVerifyPage() {
  const [state, setState] = useState<VerifyState>({ status: 'idle' })
  const [fieldErrors, setFieldErrors] = useState<VerifyFieldErrors>({})
  const [email] = useState(() => readPendingEmail())
  const [secondsUntilResend, setSecondsUntilResend] = useState(() => readSecondsUntilResend())
  const isResendDisabled = state.status === 'loading' || secondsUntilResend > 0

  const maskedEmail = useMemo(() => {
    if (!email) {
      return ''
    }

    const [name, domain] = email.split('@')
    if (!name || !domain) {
      return email
    }

    return `${name.slice(0, 2)}${name.length > 2 ? '***' : ''}@${domain}`
  }, [email])

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setSecondsUntilResend(readSecondsUntilResend())
    }, 1000)

    return () => window.clearInterval(intervalId)
  }, [])

  useEffect(() => {
    if (state.status !== 'success' || state.message !== 'New code sent. Check your email.') {
      return
    }

    const timeoutId = window.setTimeout(() => {
      setState({ status: 'idle' })
    }, 1500)

    return () => window.clearTimeout(timeoutId)
  }, [state])

  async function handleVerify(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!email) {
      return
    }

    const formData = new FormData(event.currentTarget)
    const otpCode = String(formData.get('otpCode') ?? '').trim()

    if (!otpPattern.test(otpCode)) {
      setFieldErrors({ otpCode: 'Enter the 6-digit code.' })
      setState({ status: 'idle' })
      return
    }

    setFieldErrors({})
    setState({ status: 'loading', message: 'Verifying email...' })

    try {
      await verifyEmailOtp(email, otpCode)
      localStorage.removeItem(pendingVerificationEmailKey)
      localStorage.removeItem(resendAvailableAtKey)
      setState({ status: 'success', message: 'Email verified. You can sign in now.' })
      window.setTimeout(() => {
        window.location.href = '/login'
      }, 900)
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Verification failed.'
      })
    }
  }

  async function handleResend() {
    if (!email || isResendDisabled) {
      return
    }

    setState({ status: 'loading', message: 'Sending a new code...' })

    try {
      await resendVerificationOtp(email)
      startResendCooldown()
      setSecondsUntilResend(resendCooldownSeconds)
      setState({ status: 'success', message: 'New code sent. Check your email.' })
    } catch (error) {
      startResendCooldown()
      setSecondsUntilResend(resendCooldownSeconds)
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not send a new code.'
      })
    }
  }

  return (
    <main className="page-shell">
      <Header />

      <section className="auth-page">
        <div className="auth-copy">
          <p className="eyebrow">Email verification</p>
          <h1>Verify.</h1>
        </div>

        <section className="auth-card" aria-label="Verify email form">
          {email ? (
            <form className="auth-form" onSubmit={handleVerify} noValidate>
              <p className="auth-context">
                Code sent to <strong>{maskedEmail}</strong>
              </p>

              <label className="field-label" htmlFor="otpCode">
                Verification code <span>*</span>
              </label>
              <input
                id="otpCode"
                name="otpCode"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                aria-invalid={Boolean(fieldErrors.otpCode)}
                aria-describedby={fieldErrors.otpCode ? 'otpCode-error' : undefined}
              />
              {fieldErrors.otpCode && (
                <p className="field-error" id="otpCode-error">{fieldErrors.otpCode}</p>
              )}

              <Button type="submit" disabled={state.status === 'loading'}>
                Verify email
              </Button>
              <p className="auth-resend">
                <PaperPlaneTilt weight="bold" aria-hidden="true" />
                {secondsUntilResend > 0 ? (
                  <span>Send a new code in {secondsUntilResend}s</span>
                ) : (
                  <a
                    href="#resend"
                    aria-disabled={isResendDisabled}
                    onClick={event => {
                      event.preventDefault()
                      void handleResend()
                    }}
                  >
                    Send a new code
                  </a>
                )}
              </p>
            </form>
          ) : (
            <div className="auth-empty-state">
              <p>No email is waiting for verification.</p>
              <a className="action-button action-button-dark" href="/register">
                Sign up
              </a>
            </div>
          )}

          {state.status !== 'idle' && (
            <p className={`auth-message ${state.status === 'error' ? 'is-error' : ''}`}>
              {state.message}
            </p>
          )}

          <p className="auth-helper">
            Already have an account? <a href="/login">Sign in.</a>
          </p>
        </section>
      </section>

      <Footer />
    </main>
  )
}

function readPendingEmail() {
  const queryEmail = new URLSearchParams(window.location.search).get('email')?.trim()
  if (queryEmail) {
    localStorage.setItem(pendingVerificationEmailKey, queryEmail)
    return queryEmail
  }

  return localStorage.getItem(pendingVerificationEmailKey)
}

function readSecondsUntilResend() {
  const availableAt = Number(localStorage.getItem(resendAvailableAtKey) ?? '0')
  if (!Number.isFinite(availableAt) || availableAt <= Date.now()) {
    return 0
  }

  return Math.ceil((availableAt - Date.now()) / 1000)
}

function startResendCooldown() {
  localStorage.setItem(
    resendAvailableAtKey,
    String(Date.now() + resendCooldownSeconds * 1000)
  )
}
