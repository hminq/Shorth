import { useEffect, useState } from 'react'
import { fetchUserLinks, shortUrl, type UserLinkSummary, type UserLinksResponse } from '../lib/api'
import { Footer } from './Footer'
import { Header } from './Header'

type LinksState =
  | { status: 'loading'; message: string }
  | { status: 'ready'; payload: UserLinksResponse }
  | { status: 'error'; message: string }

export function MyLinksPage() {
  const page = getPageFromUrl()
  const [state, setState] = useState<LinksState>({ status: 'loading', message: 'Loading links...' })

  useEffect(() => {
    async function loadLinks() {
      try {
        setState({ status: 'ready', payload: await fetchUserLinks(page) })
      } catch (error) {
        setState({
          status: 'error',
          message: error instanceof Error ? error.message : 'Could not load links.'
        })
      }
    }

    void loadLinks()
  }, [page])

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
            <div className="my-links-view">
              <div className="my-links-list">
                {state.payload.items.length === 0 ? (
                  <p className="empty-state">{state.payload.page === 1 ? 'No links yet.' : 'No links on this page.'}</p>
                ) : (
                  state.payload.items.map(link => <MyLinkCard key={link.id} link={link} />)
                )}
              </div>

              <nav className="links-pagination" aria-label="Links pagination">
                <a
                  className={state.payload.page <= 1 ? 'is-disabled' : ''}
                  href={state.payload.page <= 1 ? undefined : buildPageHref(state.payload.page - 1)}
                  aria-disabled={state.payload.page <= 1}
                >
                  Prev
                </a>
                <span>Page {state.payload.page}</span>
                <a
                  className={!state.payload.hasNextPage ? 'is-disabled' : ''}
                  href={!state.payload.hasNextPage ? undefined : buildPageHref(state.payload.page + 1)}
                  aria-disabled={!state.payload.hasNextPage}
                >
                  Next
                </a>
              </nav>
            </div>
          )}
        </section>
      </section>

      <Footer />
    </main>
  )
}

function getPageFromUrl() {
  const value = new URLSearchParams(window.location.search).get('page')
  if (!value) {
    return 1
  }

  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : 1
}

function buildPageHref(page: number) {
  return page <= 1 ? '/links' : `/links?page=${page}`
}

function MyLinkCard({ link }: { link: UserLinkSummary }) {
  return (
    <article className="my-link-card">
      <div className="my-link-main">
        <a className="my-link-short" href={shortUrl(link.slug)} target="_blank" rel="noreferrer">
          {shortUrl(link.slug)}
        </a>
        <p>{link.destinationUrl}</p>
        <a className="my-link-analytics" href={`/links/${link.id}/analytics`}>
          Analytics
        </a>
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
