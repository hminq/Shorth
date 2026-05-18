import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Eye, EyeSlash, PaperPlaneTilt } from '@phosphor-icons/react'
import { completePasswordReset, forgotPassword, verifyPasswordReset } from '../lib/api'
import { Button } from './Button'
import { Footer } from './Footer'
import { Header } from './Header'

type PasswordResetState =
  | { status: 'idle' }
  | { status: 'loading'; message: string }
  | { status: 'success'; message: string }
  | { status: 'error'; message: string }

type PasswordResetField = 'email' | 'otpCode' | 'newPassword' | 'confirmPassword'
type PasswordResetFieldErrors = Partial<Record<PasswordResetField, string>>
type PasswordResetStep = 'code' | 'password'

const pendingPasswordResetEmailKey = 'shorth.pendingPasswordResetEmail'
const resetCodeAvailableAtKey = 'shorth.passwordResetCodeAvailableAt'
const resetCodeCooldownSeconds = 60
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const otpPattern = /^\d{6}$/
const strongPasswordPattern = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$/

export function PasswordResetPage() {
  const [state, setState] = useState<PasswordResetState>({ status: 'idle' })
  const [fieldErrors, setFieldErrors] = useState<PasswordResetFieldErrors>({})
  const [step, setStep] = useState<PasswordResetStep>('code')
  const [resetToken, setResetToken] = useState('')
  const [secondsUntilResend, setSecondsUntilResend] = useState(() => readSecondsUntilResend())
  const [formValues, setFormValues] = useState(() => ({
    email: readPendingEmail(),
    otpCode: '',
    newPassword: '',
    confirmPassword: ''
  }))
  const [isNewPasswordVisible, setIsNewPasswordVisible] = useState(false)
  const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false)
  const isResendDisabled = state.status === 'loading' || secondsUntilResend > 0

  const maskedEmail = useMemo(() => {
    const [name, domain] = formValues.email.split('@')
    if (!name || !domain) {
      return ''
    }

    return `${name.slice(0, 2)}${name.length > 2 ? '***' : ''}@${domain}`
  }, [formValues.email])

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setSecondsUntilResend(readSecondsUntilResend())
    }, 1000)

    return () => window.clearInterval(intervalId)
  }, [])

  useEffect(() => {
    if (state.status !== 'success' || state.message !== 'New reset code sent. Check your email.') {
      return
    }

    const timeoutId = window.setTimeout(() => {
      setState({ status: 'idle' })
    }, 1500)

    return () => window.clearTimeout(timeoutId)
  }, [state])

  async function handleCodeVerify(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const email = formValues.email.trim()
    const otpCode = formValues.otpCode.trim()
    const nextFieldErrors: PasswordResetFieldErrors = {}

    if (!email) {
      nextFieldErrors.email = 'Email is required.'
    } else if (!emailPattern.test(email)) {
      nextFieldErrors.email = 'Enter a valid email address.'
    }

    if (!otpCode) {
      nextFieldErrors.otpCode = 'Reset code is required.'
    } else if (!otpPattern.test(otpCode)) {
      nextFieldErrors.otpCode = 'Enter the 6-digit code.'
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors)
      setState({ status: 'idle' })
      return
    }

    setFieldErrors({})
    setState({ status: 'loading', message: 'Checking reset code...' })

    try {
      const result = await verifyPasswordReset(email, otpCode)
      setResetToken(result.resetToken)
      setStep('password')
      setState({ status: 'idle' })
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not verify your reset code.'
      })
    }
  }

  async function handlePasswordReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const newPassword = formValues.newPassword
    const confirmPassword = formValues.confirmPassword
    const nextFieldErrors: PasswordResetFieldErrors = {}

    if (!resetToken) {
      setStep('code')
      setState({ status: 'error', message: 'Reset session expired. Enter the code again.' })
      return
    }

    if (!newPassword) {
      nextFieldErrors.newPassword = 'New password is required.'
    } else if (newPassword.length < 8 || newPassword.length > 72) {
      nextFieldErrors.newPassword = 'Password must be between 8 and 72 characters.'
    } else if (!strongPasswordPattern.test(newPassword)) {
      nextFieldErrors.newPassword = 'Use uppercase, lowercase, a number, and a special character. No spaces.'
    }

    if (!confirmPassword) {
      nextFieldErrors.confirmPassword = 'Confirm your new password.'
    } else if (confirmPassword !== newPassword) {
      nextFieldErrors.confirmPassword = 'Passwords do not match.'
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors)
      setState({ status: 'idle' })
      return
    }

    setFieldErrors({})
    setState({ status: 'loading', message: 'Resetting password...' })

    try {
      await completePasswordReset(resetToken, newPassword)
      localStorage.removeItem(pendingPasswordResetEmailKey)
      setState({ status: 'success', message: 'Password reset. You can sign in now.' })
      window.setTimeout(() => {
        window.location.href = '/login'
      }, 900)
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not reset your password.'
      })
    }
  }

  async function handleResendCode() {
    const email = formValues.email.trim()

    if (isResendDisabled) {
      return
    }

    if (!email) {
      window.location.href = '/forgot-password'
      return
    }

    if (!emailPattern.test(email)) {
      setFieldErrors({ email: 'Enter a valid email address.' })
      setState({ status: 'idle' })
      return
    }

    setFieldErrors({})
    setResetToken('')
    setStep('code')
    setState({ status: 'loading', message: 'Sending a new reset code...' })

    try {
      await forgotPassword(email)
      localStorage.setItem(pendingPasswordResetEmailKey, email)
      startResendCooldown()
      setSecondsUntilResend(resetCodeCooldownSeconds)
      setState({ status: 'success', message: 'New reset code sent. Check your email.' })
    } catch (error) {
      startResendCooldown()
      setSecondsUntilResend(resetCodeCooldownSeconds)
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not send a new reset code.'
      })
    }
  }

  return (
    <main className="page-shell">
      <Header />

      <section className="auth-page">
        <div className="auth-copy">
          <p className="eyebrow">Password reset</p>
          <h1>Reset.</h1>
        </div>

        <section className="auth-card" aria-label="Password reset form">
          {step === 'code' ? (
            <form className="auth-form" onSubmit={handleCodeVerify} noValidate>
              {maskedEmail && (
                <p className="auth-context">
                  Code sent to <strong>{maskedEmail}</strong>
                </p>
              )}

              <label className="field-label" htmlFor="email">
                Email <span>*</span>
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                value={formValues.email}
                aria-invalid={Boolean(fieldErrors.email)}
                aria-describedby={fieldErrors.email ? 'email-error' : undefined}
                onChange={event => setFormValues(current => ({ ...current, email: event.target.value }))}
              />
              {fieldErrors.email && (
                <p className="field-error" id="email-error">{fieldErrors.email}</p>
              )}

              <label className="field-label" htmlFor="otpCode">
                Reset code <span>*</span>
              </label>
              <input
                id="otpCode"
                name="otpCode"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                value={formValues.otpCode}
                aria-invalid={Boolean(fieldErrors.otpCode)}
                aria-describedby={fieldErrors.otpCode ? 'otpCode-error' : undefined}
                onChange={event => setFormValues(current => ({ ...current, otpCode: event.target.value }))}
              />
              {fieldErrors.otpCode && (
                <p className="field-error" id="otpCode-error">{fieldErrors.otpCode}</p>
              )}

              <Button type="submit" disabled={state.status === 'loading'}>
                Continue
              </Button>
            </form>
          ) : (
            <form className="auth-form" onSubmit={handlePasswordReset} noValidate>
              {maskedEmail && (
                <p className="auth-context">
                  Resetting password for <strong>{maskedEmail}</strong>
                </p>
              )}

              <label className="field-label" htmlFor="newPassword">
                New password <span>*</span>
              </label>
              <PasswordField
                id="newPassword"
                name="newPassword"
                autoComplete="new-password"
                value={formValues.newPassword}
                isVisible={isNewPasswordVisible}
                error={fieldErrors.newPassword}
                onChange={value => setFormValues(current => ({ ...current, newPassword: value }))}
                onToggle={() => setIsNewPasswordVisible(current => !current)}
              />
              {fieldErrors.newPassword && (
                <p className="field-error" id="newPassword-error">{fieldErrors.newPassword}</p>
              )}

              <label className="field-label" htmlFor="confirmPassword">
                Confirm password <span>*</span>
              </label>
              <PasswordField
                id="confirmPassword"
                name="confirmPassword"
                autoComplete="new-password"
                value={formValues.confirmPassword}
                isVisible={isConfirmPasswordVisible}
                error={fieldErrors.confirmPassword}
                onChange={value => setFormValues(current => ({ ...current, confirmPassword: value }))}
                onToggle={() => setIsConfirmPasswordVisible(current => !current)}
              />
              {fieldErrors.confirmPassword && (
                <p className="field-error" id="confirmPassword-error">{fieldErrors.confirmPassword}</p>
              )}

              <Button type="submit" disabled={state.status === 'loading'}>
                Reset password
              </Button>
            </form>
          )}

          {state.status !== 'idle' && (
            <p className={`auth-message ${state.status === 'error' ? 'is-error' : ''}`}>
              {state.message}
            </p>
          )}

          <p className="auth-resend">
            <PaperPlaneTilt weight="bold" aria-hidden="true" />
            {secondsUntilResend > 0 ? (
              <span>Send a new code in {secondsUntilResend}s</span>
            ) : formValues.email.trim() ? (
              <a
                href="#resend"
                aria-disabled={isResendDisabled}
                onClick={event => {
                  event.preventDefault()
                  void handleResendCode()
                }}
              >
                Send a new code
              </a>
            ) : (
              <a href="/forgot-password">Send a new code</a>
            )}
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

function readPendingEmail() {
  const queryEmail = new URLSearchParams(window.location.search).get('email')?.trim()
  if (queryEmail) {
    localStorage.setItem(pendingPasswordResetEmailKey, queryEmail)
    return queryEmail
  }

  return localStorage.getItem(pendingPasswordResetEmailKey) ?? ''
}

function readSecondsUntilResend() {
  const availableAt = Number(localStorage.getItem(resetCodeAvailableAtKey) ?? '0')
  if (!Number.isFinite(availableAt) || availableAt <= Date.now()) {
    return 0
  }

  return Math.ceil((availableAt - Date.now()) / 1000)
}

function startResendCooldown() {
  localStorage.setItem(
    resetCodeAvailableAtKey,
    String(Date.now() + resetCodeCooldownSeconds * 1000)
  )
}

type PasswordFieldProps = {
  id: string
  name: string
  autoComplete: string
  value: string
  isVisible: boolean
  error?: string
  onChange: (value: string) => void
  onToggle: () => void
}

function PasswordField({
  id,
  name,
  autoComplete,
  value,
  isVisible,
  error,
  onChange,
  onToggle
}: PasswordFieldProps) {
  return (
    <div className="password-control">
      <input
        id={id}
        name={name}
        type={isVisible ? 'text' : 'password'}
        autoComplete={autoComplete}
        value={value}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? `${id}-error` : undefined}
        onChange={event => onChange(event.target.value)}
      />
      <button
        className="password-toggle"
        type="button"
        aria-label={isVisible ? 'Hide password' : 'Show password'}
        onClick={onToggle}
      >
        {isVisible ? <EyeSlash weight="bold" /> : <Eye weight="bold" />}
      </button>
    </div>
  )
}
