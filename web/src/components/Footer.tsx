import { GithubLogo, InstagramLogo } from '@phosphor-icons/react'

const GithubUrl = 'https://github.com/hminq/Shorth'
const InstagramUrl = 'https://www.instagram.com/toilam0nesy/'

export function Footer() {
  return (
    <footer className="site-footer">
      <span>Shorth</span>
      <div className="footer-links" aria-label="Contact links">
        <a href={GithubUrl} target="_blank" rel="noreferrer" aria-label="Shorth on GitHub">
          <GithubLogo size={18} weight="bold" />
          GitHub
        </a>
        <a href={InstagramUrl} target="_blank" rel="noreferrer" aria-label="Shorth on Instagram">
          <InstagramLogo size={18} weight="bold" />
          Instagram
        </a>
      </div>
    </footer>
  )
}
