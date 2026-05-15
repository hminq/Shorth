import { Button } from './Button'
import { getAuthSession } from '../lib/api'

export function Header() {
  const session = getAuthSession()

  return (
    <header className="site-header">
      <a className="brand" href="/" aria-label="Shorth home">
        <span>Shorth</span>
      </a>
      <nav className="nav-links" aria-label="Primary navigation">
        {session ? (
          <a className="user-chip" href="/dashboard" aria-label={`Signed in as ${session.displayName}`}>
            <span className="user-name">{session.displayName}</span>
            <span className="user-avatar" aria-hidden="true">
              {getInitials(session.displayName)}
            </span>
          </a>
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
