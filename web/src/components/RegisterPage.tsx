import { useState, type FormEvent } from 'react'
import { Eye, EyeSlash } from '@phosphor-icons/react'
import { getGoogleLoginUrl, registerLocal } from '../lib/api'
import { Button } from './Button'
import { Footer } from './Footer'
import { Header } from './Header'

type RegisterState =
  | { status: 'idle' }
  | { status: 'loading'; message: string }
  | { status: 'success'; message: string }
  | { status: 'error'; message: string }

type RegisterField = 'email' | 'displayName' | 'password' | 'confirmPassword'
type RegisterFieldErrors = Partial<Record<RegisterField, string>>

const pendingVerificationEmailKey = 'shorth.pendingVerificationEmail'
const resendAvailableAtKey = 'shorth.verificationResendAvailableAt'
const resendCooldownSeconds = 60
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const strongPasswordPattern = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$/

export function RegisterPage() {
  const [state, setState] = useState<RegisterState>({ status: 'idle' })
  const [fieldErrors, setFieldErrors] = useState<RegisterFieldErrors>({})
  const [isPasswordVisible, setIsPasswordVisible] = useState(false)
  const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false)
  const [formValues, setFormValues] = useState({
    email: '',
    displayName: '',
    password: '',
    confirmPassword: ''
  })

  async function handleRegister(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const formData = new FormData(event.currentTarget)
    const email = String(formData.get('email') ?? '').trim()
    const displayName = String(formData.get('displayName') ?? '').trim()
    const password = String(formData.get('password') ?? '')
    const confirmPassword = String(formData.get('confirmPassword') ?? '')
    const nextFieldErrors: RegisterFieldErrors = {}

    if (!email) {
      nextFieldErrors.email = 'Email is required.'
    } else if (!emailPattern.test(email)) {
      nextFieldErrors.email = 'Enter a valid email address.'
    }

    if (!displayName) {
      nextFieldErrors.displayName = 'Display name is required.'
    } else if (displayName.length > 100) {
      nextFieldErrors.displayName = 'Display name must be 100 characters or fewer.'
    }

    if (!password) {
      nextFieldErrors.password = 'Password is required.'
    } else if (password.length < 8 || password.length > 72) {
      nextFieldErrors.password = 'Password must be between 8 and 72 characters.'
    } else if (!strongPasswordPattern.test(password)) {
      nextFieldErrors.password = 'Use uppercase, lowercase, a number, and a special character. No spaces.'
    }

    if (!confirmPassword) {
      nextFieldErrors.confirmPassword = 'Confirm your password.'
    } else if (confirmPassword !== password) {
      nextFieldErrors.confirmPassword = 'Passwords do not match.'
    }

    if (Object.keys(nextFieldErrors).length > 0) {
      setFieldErrors(nextFieldErrors)
      setState({ status: 'idle' })
      return
    }

    setFieldErrors({})
    setState({ status: 'loading', message: 'Creating account...' })

    try {
      const result = await registerLocal(email, password, displayName)
      if (result.requiresEmailVerification) {
        localStorage.setItem(pendingVerificationEmailKey, result.email)
        localStorage.setItem(
          resendAvailableAtKey,
          String(Date.now() + resendCooldownSeconds * 1000)
        )
        setState({ status: 'success', message: 'Verification code sent. Check your email.' })
        window.location.href = `/register/verify?email=${encodeURIComponent(result.email)}`
        return
      }

      setState({ status: 'success', message: 'Account created. You can sign in now.' })
      window.setTimeout(() => {
        window.location.href = '/login'
      }, 900)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Could not create your account.'
      if (message.toLowerCase().includes('verification code')) {
        localStorage.setItem(pendingVerificationEmailKey, email)
        setState({ status: 'success', message: 'Verification code already sent. Opening verification.' })
        window.location.href = `/register/verify?email=${encodeURIComponent(email)}`
        return
      }

      setState({ status: 'error', message })
    }
  }

  async function handleGoogleSignUp() {
    setState({ status: 'loading', message: 'Opening Google sign up...' })

    try {
      window.location.href = await getGoogleLoginUrl()
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not start Google sign up.'
      })
    }
  }

  return (
    <main className="page-shell">
      <Header />

      <section className="auth-page">
        <div className="auth-copy">
          <p className="eyebrow">New account</p>
          <h1>Sign up.</h1>
        </div>

        <section className="auth-card" aria-label="Create account form">
              <form className="auth-form" onSubmit={handleRegister} noValidate>
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

                <label className="field-label" htmlFor="displayName">
                  Display name <span>*</span>
                </label>
                <input
                  id="displayName"
                  name="displayName"
                  type="text"
                  autoComplete="name"
                  value={formValues.displayName}
                  aria-invalid={Boolean(fieldErrors.displayName)}
                  aria-describedby={fieldErrors.displayName ? 'displayName-error' : undefined}
                  onChange={event => setFormValues(current => ({ ...current, displayName: event.target.value }))}
                />
                {fieldErrors.displayName && (
                  <p className="field-error" id="displayName-error">{fieldErrors.displayName}</p>
                )}

                <label className="field-label" htmlFor="password">
                  Password <span>*</span>
                </label>
                <PasswordField
                  id="password"
                  name="password"
                  autoComplete="new-password"
                  value={formValues.password}
                  isVisible={isPasswordVisible}
                  error={fieldErrors.password}
                  onChange={value => setFormValues(current => ({ ...current, password: value }))}
                  onToggle={() => setIsPasswordVisible(current => !current)}
                />
                {fieldErrors.password && (
                  <p className="field-error" id="password-error">{fieldErrors.password}</p>
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
                  Create account
                </Button>
              </form>

              <div className="auth-divider" aria-hidden="true">
                Or
              </div>

              <button
                className="action-button auth-google-button"
                type="button"
                disabled={state.status === 'loading'}
                onClick={() => void handleGoogleSignUp()}
              >
                <GoogleLogo />
                Continue with Google
              </button>

              <p className="auth-helper">
                Already have a code?{' '}
                <a href={buildVerifyHref(formValues.email)} onClick={() => rememberPendingEmail(formValues.email)}>
                  Enter verification code.
                </a>
              </p>

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

function buildVerifyHref(email: string) {
  const trimmedEmail = email.trim()
  if (!trimmedEmail) {
    return '/register/verify'
  }

  return `/register/verify?email=${encodeURIComponent(trimmedEmail)}`
}

function rememberPendingEmail(email: string) {
  const trimmedEmail = email.trim()
  if (trimmedEmail) {
    localStorage.setItem(pendingVerificationEmailKey, trimmedEmail)
  }
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
