import { Benefits } from './Benefits'
import { Footer } from './Footer'
import { Header } from './Header'
import { LinkForm } from './LinkForm'

export function HomePage() {
  return (
    <main className="page-shell">
      <Header />

      <section className="hero">
        <p className="eyebrow">Fast URL shortener</p>
        <h1>Paste a long link. Get a short one.</h1>
        <p className="hero-copy">
          Shorth makes clean links and QR codes in one step. No account required,
          no setup flow, just a link that is ready to share.
        </p>
        <LinkForm />
      </section>

      <Benefits />
      <Footer />
    </main>
  )
}
