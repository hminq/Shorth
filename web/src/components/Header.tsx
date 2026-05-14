import { Button } from './Button'

export function Header() {
  return (
    <header className="site-header">
      <a className="brand" href="/" aria-label="Shorth home">
        <img src="/shorth_logo.svg" alt="" />
        <span>Shorth</span>
      </a>
      <nav className="nav-links" aria-label="Primary navigation">
        <a href="#how-it-works">How it works</a>
        <Button type="button" onClick={() => { window.location.href = '#signin' }}>
          Sign in
        </Button>
      </nav>
    </header>
  )
}
