import { Button } from './Button'
import { fetchMe, getAuthSession, logout, saveProfileSession, type AuthSession } from '../lib/api'
import { useEffect, useState } from 'react'

export function Header() {
  const [session, setSession] = useState<AuthSession | null>(() => getAuthSession())
  const [isMenuOpen, setIsMenuOpen] = useState(false)

  useEffect(() => {
    if (session) {
      return
    }

    async function syncProfile() {
      try {
        const profile = await fetchMe()
        saveProfileSession(profile)
        setSession(profile)
      } catch {
        setSession(null)
      }
    }

    void syncProfile()
  }, [session])

  useEffect(() => {
    function handleSessionUpdate(event: Event) {
      setSession((event as CustomEvent<AuthSession>).detail)
    }

    window.addEventListener('shorth:auth-session-updated', handleSessionUpdate)
    return () => window.removeEventListener('shorth:auth-session-updated', handleSessionUpdate)
  }, [])

  async function handleLogout() {
    try {
      await logout()
    } finally {
      window.location.href = '/'
    }
  }

  return (
    <header className="site-header">
      <a className="brand" href="/" aria-label="Shorth home">
        <span>Shorth</span>
      </a>
      <nav className="nav-links" aria-label="Primary navigation">
        {session ? (
          <div className="user-menu">
            <button
              className="user-chip"
              type="button"
              aria-expanded={isMenuOpen}
              aria-haspopup="menu"
              onClick={() => setIsMenuOpen(current => !current)}
            >
              <span className="user-name">{session.displayName}</span>
              <span className="user-avatar" aria-hidden="true">
                {session.avatarUrl ? (
                  <img src={session.avatarUrl} alt="" />
                ) : (
                  getInitials(session.displayName)
                )}
              </span>
            </button>
            {isMenuOpen && (
              <div className="user-menu-panel" role="menu">
                <a href="/profile" role="menuitem">
                  Profile
                </a>
                <a href="/links" role="menuitem">
                  My links
                </a>
                <button className="user-menu-signout" type="button" role="menuitem" onClick={() => void handleLogout()}>
                  Sign out
                </button>
              </div>
            )}
          </div>
        ) : (
          <Button type="button" onClick={() => { window.location.href = '/login' }}>
            Sign in
          </Button>
        )}
      </nav>
    </header>
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
