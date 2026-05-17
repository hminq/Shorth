import { useEffect, useMemo, useState } from 'react'
import { fetchLinkAnalytics, type LinkAnalyticsResponse } from '../lib/api'
import { Footer } from './Footer'
import { Header } from './Header'

type AnalyticsState =
  | { status: 'loading'; message: string }
  | { status: 'ready'; payload: LinkAnalyticsResponse }
  | { status: 'error'; message: string }

export function LinkAnalyticsPage() {
  const linkId = useMemo(() => getLinkIdFromPath(), [])
  const [state, setState] = useState<AnalyticsState>({ status: 'loading', message: 'Loading analytics...' })

  useEffect(() => {
    async function loadAnalytics() {
      if (!linkId) {
        setState({ status: 'error', message: 'We could not find what you requested.' })
        return
      }

      try {
        setState({ status: 'ready', payload: await fetchLinkAnalytics(linkId) })
      } catch (error) {
        setState({
          status: 'error',
          message: error instanceof Error ? error.message : 'Could not load analytics.'
        })
      }
    }

    void loadAnalytics()
  }, [linkId])

  return (
    <main className="page-shell">
      <Header />

      <section className="account-page">
        <div className="account-copy">
          <p className="eyebrow">Analytics</p>
          <h1>Analytics.</h1>
        </div>

        <section className="account-panel" aria-label="Link analytics">
          <div className="panel-toolbar">
            <a className="panel-back-link" href="/links">
              Back to links
            </a>
          </div>
          {state.status === 'loading' && <p className="auth-message">{state.message}</p>}
          {state.status === 'error' && <p className="auth-message is-error">{state.message}</p>}
          {state.status === 'ready' && <AnalyticsPanel analytics={state.payload} />}
        </section>
      </section>

      <Footer />
    </main>
  )
}

function AnalyticsPanel({ analytics }: { analytics: LinkAnalyticsResponse }) {
  return (
    <div className="link-analytics-panel">
      <dl className="link-analytics-summary">
        <div>
          <dt>Total clicks</dt>
          <dd>{analytics.totalClicks}</dd>
        </div>
        <div>
          <dt>Last click</dt>
          <dd>{analytics.lastClickedAt ? formatDateTime(analytics.lastClickedAt) : 'None'}</dd>
        </div>
        <div>
          <dt>Window</dt>
          <dd>{formatDate(analytics.from)} - {formatDate(analytics.to)}</dd>
        </div>
      </dl>

      <div className="link-analytics-grid">
        <section aria-label="Daily clicks">
          <h2>Daily clicks</h2>
          {analytics.daily.length === 0 ? (
            <p className="empty-state">No clicks in this window.</p>
          ) : (
            <div className="analytics-table">
              {analytics.daily.map(day => (
                <div className="analytics-row" key={day.date}>
                  <span>{formatDate(day.date)}</span>
                  <strong>{day.clicks}</strong>
                  <small>{day.uniqueVisitors} unique</small>
                </div>
              ))}
            </div>
          )}
        </section>

        <section aria-label="Top countries">
          <h2>Top countries</h2>
          {analytics.topCountries.length === 0 ? (
            <p className="empty-state">No country data yet.</p>
          ) : (
            <div className="country-list">
              {analytics.topCountries.map(country => (
                <div className="country-row" key={country.countryCode}>
                  <span>{country.countryCode}</span>
                  <div className="country-bar" aria-hidden="true">
                    <span style={{ width: `${Math.min(country.percent, 100)}%` }} />
                  </div>
                  <strong>{country.percent.toFixed(2)}%</strong>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  )
}

function getLinkIdFromPath() {
  const match = window.location.pathname.match(/^\/links\/([^/]+)\/analytics$/)
  return match?.[1] ?? null
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  }).format(new Date(value))
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value))
}
