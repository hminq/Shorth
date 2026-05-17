import { useEffect, useMemo, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import {
  createUpload,
  fetchMe,
  saveProfileSession,
  updateProfile,
  uploadFileToS3,
  type UserProfile
} from '../lib/api'
import { Button } from './Button'
import { Footer } from './Footer'
import { Header } from './Header'
import { Eye, EyeSlash } from '@phosphor-icons/react'

type ProfileState =
  | { status: 'idle'; message?: string }
  | { status: 'loading'; message: string }
  | { status: 'success'; message: string }
  | { status: 'error'; message: string }

type FieldErrors = Partial<Record<
  'avatarFile' | 'displayName' | 'currentPassword' | 'newPassword' | 'confirmNewPassword',
  string
>>

const allowedAvatarTypes = new Set(['image/png', 'image/jpeg', 'image/webp'])
const maxAvatarSizeBytes = 1_048_576
const strongPasswordPattern = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9])\S+$/

export function ProfilePage() {
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [displayName, setDisplayName] = useState('')
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmNewPassword, setConfirmNewPassword] = useState('')
  const [isCurrentPasswordVisible, setIsCurrentPasswordVisible] = useState(false)
  const [isNewPasswordVisible, setIsNewPasswordVisible] = useState(false)
  const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false)
  const [state, setState] = useState<ProfileState>({ status: 'loading', message: 'Loading profile...' })
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const avatarInputRef = useRef<HTMLInputElement | null>(null)

  const avatarPreview = useMemo(() => {
    if (!avatarFile) {
      return profile?.avatarUrl ?? null
    }

    return URL.createObjectURL(avatarFile)
  }, [avatarFile, profile?.avatarUrl])

  useEffect(() => {
    return () => {
      if (avatarFile && avatarPreview?.startsWith('blob:')) {
        URL.revokeObjectURL(avatarPreview)
      }
    }
  }, [avatarFile, avatarPreview])

  useEffect(() => {
    async function loadProfile() {
      try {
        const nextProfile = await fetchMe()
        setProfile(nextProfile)
        setDisplayName(nextProfile.displayName)
        saveProfileSession(nextProfile)
        setState({ status: 'idle' })
      } catch (error) {
        setState({
          status: 'error',
          message: error instanceof Error ? error.message : 'Could not load profile.'
        })
      }
    }

    void loadProfile()
  }, [])

  useEffect(() => {
    if (state.status !== 'success') {
      return
    }

    const timeoutId = window.setTimeout(() => {
      setState({ status: 'idle' })
    }, 1500)

    return () => window.clearTimeout(timeoutId)
  }, [state.status])

  function handleAvatarChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null

    if (!file) {
      setAvatarFile(null)
      setFieldErrors(current => ({ ...current, avatarFile: undefined }))
      return
    }

    if (!allowedAvatarTypes.has(file.type)) {
      setFieldErrors(current => ({ ...current, avatarFile: 'Use a PNG, JPEG, or WebP image.' }))
      event.target.value = ''
      setAvatarFile(null)
      return
    }

    if (file.size > maxAvatarSizeBytes) {
      setFieldErrors(current => ({ ...current, avatarFile: 'Profile image must be 1 MB or smaller.' }))
      event.target.value = ''
      setAvatarFile(null)
      return
    }

    setAvatarFile(file)
    setFieldErrors(current => ({ ...current, avatarFile: undefined }))
    setState({ status: 'idle' })
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!profile) {
      setState({ status: 'error', message: 'Please sign in again to continue.' })
      return
    }

    const trimmedDisplayName = displayName.trim()
    const trimmedCurrentPassword = currentPassword.trim()
    const trimmedNewPassword = newPassword.trim()
    const trimmedConfirmNewPassword = confirmNewPassword.trim()
    const nextFieldErrors: FieldErrors = {}

    if (!trimmedDisplayName) {
      nextFieldErrors.displayName = 'Display name is required.'
    }

    if (trimmedNewPassword && (trimmedNewPassword.length < 8 || trimmedNewPassword.length > 72)) {
      nextFieldErrors.newPassword = 'New password must be between 8 and 72 characters.'
    } else if (trimmedNewPassword && !strongPasswordPattern.test(trimmedNewPassword)) {
      nextFieldErrors.newPassword = 'Use uppercase, lowercase, a number, and a special character. No spaces.'
    }

    if (trimmedNewPassword && profile.hasPassword && !trimmedCurrentPassword) {
      nextFieldErrors.currentPassword = 'Enter your current password.'
    }

    if (trimmedNewPassword !== trimmedConfirmNewPassword) {
      nextFieldErrors.confirmNewPassword = 'New passwords do not match.'
    }

    if (!trimmedNewPassword && trimmedConfirmNewPassword) {
      nextFieldErrors.newPassword = 'Enter a new password.'
    }

    setFieldErrors(nextFieldErrors)
    if (Object.keys(nextFieldErrors).length > 0) {
      return
    }

    setState({ status: 'loading', message: avatarFile ? 'Uploading profile image...' : 'Saving profile...' })

    try {
      let avatarUrl: string | undefined

      if (avatarFile) {
        const upload = await createUpload({
          fileType: 'profile_image',
          fileName: avatarFile.name,
          contentType: avatarFile.type,
          fileSizeBytes: avatarFile.size
        })
        await uploadFileToS3(upload, avatarFile)
        avatarUrl = upload.publicUrl
      }

      const nextProfile = await updateProfile({
        displayName: trimmedDisplayName,
        ...(avatarUrl ? { avatarUrl } : {}),
        ...(trimmedNewPassword ? {
          currentPassword: trimmedCurrentPassword,
          newPassword: trimmedNewPassword
        } : {})
      })

      setProfile(nextProfile)
      setDisplayName(nextProfile.displayName)
      setAvatarFile(null)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmNewPassword('')
      saveProfileSession(nextProfile)
      setState({ status: 'success', message: 'Profile updated.' })
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Could not update profile.'
      if (message.toLowerCase().includes('current password')) {
        setFieldErrors(current => ({ ...current, currentPassword: message }))
        setState({ status: 'idle' })
        return
      }

      setState({
        status: 'error',
        message
      })
    }
  }

  return (
    <main className="page-shell">
      <Header />

      <section className="account-page">
        <div className="account-copy">
          <p className="eyebrow">Account</p>
          <h1>Profile.</h1>
        </div>

        <section className="account-panel" aria-label="Profile editor">
          <form className="profile-form" onSubmit={handleSubmit}>
            <div className="profile-avatar-row">
              <button
                className="profile-avatar-preview"
                type="button"
                aria-label="Choose profile image"
                onClick={() => avatarInputRef.current?.click()}
              >
                {avatarPreview ? (
                  <img src={avatarPreview} alt="" />
                ) : (
                  getInitials(displayName)
                )}
                <span className="profile-avatar-overlay" aria-hidden="true">
                  Edit
                </span>
              </button>

              <input
                ref={avatarInputRef}
                className="profile-avatar-input"
                id="avatarFile"
                name="avatarFile"
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={handleAvatarChange}
              />

              {avatarFile && (
                <p className="profile-file-status">
                  Selected {avatarFile.name}
                </p>
              )}
              {fieldErrors.avatarFile && (
                <p className="field-error">{fieldErrors.avatarFile}</p>
              )}
            </div>

            <label className="field-label" htmlFor="displayName">
              Email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="off"
              readOnly
              value={profile?.email ?? ''}
            />

            <label className="field-label" htmlFor="displayName">
              Display name <span>*</span>
            </label>
            <input
              id="displayName"
              name="displayName"
              type="text"
              autoComplete="off"
              maxLength={100}
              required
              value={displayName}
              onChange={event => {
                setDisplayName(event.target.value)
                setFieldErrors(current => ({ ...current, displayName: undefined }))
              }}
            />
            {fieldErrors.displayName && (
              <p className="field-error">{fieldErrors.displayName}</p>
            )}

            <label className="field-label" htmlFor="currentPassword">
              Current password
            </label>
            <PasswordField
              id="currentPassword"
              name="currentPassword"
              autoComplete="current-password"
              maxLength={72}
              value={currentPassword}
              isVisible={isCurrentPasswordVisible}
              onChange={value => {
                setCurrentPassword(value)
                setFieldErrors(current => ({ ...current, currentPassword: undefined }))
              }}
              onToggleVisible={() => setIsCurrentPasswordVisible(current => !current)}
            />
            {fieldErrors.currentPassword && (
              <p className="field-error">{fieldErrors.currentPassword}</p>
            )}

            <div className="profile-password-grid">
              <div>
                <label className="field-label" htmlFor="newPassword">
                  New password
                </label>
                <PasswordField
                  id="newPassword"
                  name="newPassword"
                  autoComplete="new-password"
                  minLength={8}
                  maxLength={72}
                  value={newPassword}
                  isVisible={isNewPasswordVisible}
                  onChange={value => {
                    setNewPassword(value)
                    setFieldErrors(current => ({ ...current, newPassword: undefined, confirmNewPassword: undefined }))
                  }}
                  onToggleVisible={() => setIsNewPasswordVisible(current => !current)}
                />
                {fieldErrors.newPassword && (
                  <p className="field-error">{fieldErrors.newPassword}</p>
                )}
              </div>

              <div>
                <label className="field-label" htmlFor="confirmNewPassword">
                  Confirm password
                </label>
                <PasswordField
                  id="confirmNewPassword"
                  name="confirmNewPassword"
                  autoComplete="new-password"
                  minLength={8}
                  maxLength={72}
                  value={confirmNewPassword}
                  isVisible={isConfirmPasswordVisible}
                  onChange={value => {
                    setConfirmNewPassword(value)
                    setFieldErrors(current => ({ ...current, confirmNewPassword: undefined }))
                  }}
                  onToggleVisible={() => setIsConfirmPasswordVisible(current => !current)}
                />
                {fieldErrors.confirmNewPassword && (
                  <p className="field-error">{fieldErrors.confirmNewPassword}</p>
                )}
              </div>
            </div>

            <Button type="submit" disabled={state.status === 'loading' || !profile}>
              Save profile
            </Button>
          </form>

          {state.status !== 'idle' && (
            <p className={`auth-message ${state.status === 'error' ? 'is-error' : ''}`}>
              {state.message}
            </p>
          )}
        </section>
      </section>

      <Footer />
    </main>
  )
}

type PasswordFieldProps = {
  id: string
  name: string
  autoComplete: string
  value: string
  isVisible: boolean
  maxLength: number
  minLength?: number
  onChange: (value: string) => void
  onToggleVisible: () => void
}

function PasswordField({
  id,
  name,
  autoComplete,
  value,
  isVisible,
  maxLength,
  minLength,
  onChange,
  onToggleVisible
}: PasswordFieldProps) {
  return (
    <div className="password-control">
      <input
        id={id}
        name={name}
        type={isVisible ? 'text' : 'password'}
        autoComplete={autoComplete}
        minLength={minLength}
        maxLength={maxLength}
        value={value}
        onChange={event => onChange(event.target.value)}
      />
      <button
        className="password-toggle"
        type="button"
        aria-label={isVisible ? 'Hide password' : 'Show password'}
        onClick={onToggleVisible}
      >
        {isVisible ? <EyeSlash weight="bold" /> : <Eye weight="bold" />}
      </button>
    </div>
  )
}

function getInitials(displayName: string) {
  return displayName
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase())
    .join('') || 'U'
}
