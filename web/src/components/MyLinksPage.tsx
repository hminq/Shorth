import { useEffect, useState } from 'react'
import { fetchUserLinks, shortUrl, type UserLinkSummary, type UserLinksResponse } from '../lib/api'
import { Footer } from './Footer'
import { Header } from './Header'

type LinksState =
  | { status: 'loading'; message: string }
  | { status: 'ready'; payload: UserLinksResponse }
  | { status: 'error'; message: string }

export function MyLinksPage() {
  const [state, setState] = useState<LinksState>({ status: 'loading', message: 'Loading links...' })

  useEffect(() => {
    async function loadLinks() {
      try {
        setState({ status: 'ready', payload: await fetchUserLinks() })
      } catch (error) {
        setState({
          status: 'error',
          message: error instanceof Error ? error.message : 'Could not load links.'
        })
      }
    }

    void loadLinks()
  }, [])

  return (
    <main className="page-shell">
      <Header />

      <section className="account-page">
        <div className="account-copy">
          <p className="eyebrow">Links</p>
          <h1>My links.</h1>
        </div>

        <section className="account-panel" aria-label="User links">
          {state.status === 'loading' && <p className="auth-message">{state.message}</p>}
          {state.status === 'error' && <p className="auth-message is-error">{state.message}</p>}
          {state.status === 'ready' && (
            <div className="my-links-list">
              {state.payload.items.length === 0 ? (
                <p className="empty-state">No links yet.</p>
              ) : (
                state.payload.items.map(link => <MyLinkCard key={link.id} link={link} />)
              )}
            </div>
          )}
        </section>
      </section>

      <Footer />
    </main>
  )
}

function MyLinkCard({ link }: { link: UserLinkSummary }) {
  return (
    <article className="my-link-card">
      <div className="my-link-main">
        <a className="my-link-short" href={shortUrl(link.slug)} target="_blank" rel="noreferrer">
          {shortUrl(link.slug)}
        </a>
        <p>{link.destinationUrl}</p>
      </div>
      <dl className="my-link-meta">
        <div>
          <dt>Clicks</dt>
          <dd>{link.clickCount}</dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>{formatDate(link.createdAt)}</dd>
        </div>
      </dl>
    </article>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  }).format(new Date(value))
}
