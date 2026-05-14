import { Benefits } from './Benefits'
import { Footer } from './Footer'
import { Header } from './Header'
import { LinkForm } from './LinkForm'

export function HomePage() {
  return (
    <main className="page-shell">
      <Header />

      <section className="hero">
        <p className="eyebrow">Minimal URL shortener</p>
        <h1>Short links without the ceremony.</h1>
        <p className="hero-copy">
          Shorth turns long URLs into clean redirects in one move. Paste a link, get a slug, share it.
          Accounts and analytics can wait until you need them.
        </p>
        <LinkForm />
      </section>

      <section id="how-it-works" className="editorial-grid">
        <article>
          <span className="step-index">01</span>
          <h2>Paste the destination.</h2>
          <p>Start with the link you already have. No account, campaign setup, or dashboard detour required.</p>
        </article>
        <article>
          <span className="step-index">02</span>
          <h2>Get a clean redirect.</h2>
          <p>Shorth gives you a compact link that is easier to share in messages, bios, documents, and QR codes.</p>
        </article>
        <article>
          <span className="step-index">03</span>
          <h2>Keep it simple.</h2>
          <p>Use it once and move on, or sign in later when you want to save links and see more details.</p>
        </article>
      </section>

      <Benefits />
      <Footer />
    </main>
  )
}
