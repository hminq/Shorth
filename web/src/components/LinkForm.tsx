import QRCode from 'qrcode'
import { ArrowSquareOut, Copy, DownloadSimple } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { createAnonymousLink, shortUrl } from '../lib/api'
import { Button } from './Button'

const RECENT_LINKS_KEY = 'shorth.recentAnonymousLinks'
const MaxRecentLinks = 3

type FormState =
  | { status: 'idle' }
  | { status: 'loading'; message: string }
  | { status: 'success'; link: RecentLink }
  | { status: 'error'; message: string }

type RecentLink = {
  id: string
  slug: string
  shortUrl: string
  destinationUrl: string
  createdAt: string
}

export function LinkForm() {
  const [state, setState] = useState<FormState>({ status: 'idle' })
  const [recentLinks, setRecentLinks] = useState<RecentLink[]>(() => readRecentLinks())
  const qrCanvasRef = useRef<HTMLCanvasElement | null>(null)
  const resultRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (state.status !== 'success' || !qrCanvasRef.current) {
      return
    }

    void QRCode.toCanvas(qrCanvasRef.current, state.link.shortUrl, {
      width: 220,
      margin: 1,
      color: {
        dark: '#26251e',
        light: '#ffffff'
      }
    })
  }, [state])

  useEffect(() => {
    if (state.status !== 'success') {
      return
    }

    requestAnimationFrame(() => {
      resultRef.current?.scrollIntoView({
        behavior: 'smooth',
        block: 'center'
      })
    })
  }, [state])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (state.status === 'success') {
      setState({ status: 'idle' })
      return
    }

    const form = event.currentTarget
    const formData = new FormData(form)
    const destinationUrl = String(formData.get('destinationUrl') ?? '').trim()

    if (!destinationUrl) {
      setState({ status: 'error', message: 'Paste a URL first.' })
      return
    }

    setState({ status: 'loading', message: 'Creating link...' })

    try {
      const link = await createAnonymousLink(destinationUrl)
      const recentLink = {
        id: link.id,
        slug: link.slug,
        shortUrl: shortUrl(link.slug),
        destinationUrl: link.destinationUrl,
        createdAt: link.createdAt
      }

      const nextRecentLinks = saveRecentLink(recentLink)
      setRecentLinks(nextRecentLinks)
      setState({ status: 'success', link: recentLink })
      form.reset()
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof Error ? error.message : 'Could not create short link.'
      })
    }
  }

  async function copyLink(url: string) {
    await navigator.clipboard.writeText(url)
    window.alert('Copied to clipboard.')
  }

  function downloadPngQrCode() {
    const canvas = qrCanvasRef.current
    if (!canvas || state.status !== 'success') {
      return
    }

    const link = document.createElement('a')
    link.href = canvas.toDataURL('image/png')
    link.download = `shorth-${state.link.slug}.png`
    link.click()
  }

  async function downloadSvgQrCode() {
    if (state.status !== 'success') {
      return
    }

    const svg = await QRCode.toString(state.link.shortUrl, {
      type: 'svg',
      margin: 1,
      color: {
        dark: '#26251e',
        light: '#ffffff'
      }
    })
    const blob = new Blob([svg], { type: 'image/svg+xml' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `shorth-${state.link.slug}.svg`
    link.click()
    URL.revokeObjectURL(link.href)
  }

  function scrollToRecentLinks() {
    document.getElementById('recent-links')?.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    })
  }

  return (
    <section className="shorten-area" aria-label="Create short link">
      <div className="shorten-card">
        <form className="shorten-form" onSubmit={handleSubmit}>
          <label className="field-label" htmlFor="destinationUrl">
            {state.status === 'success' ? 'Original link' : 'Long URL'} <span>*</span>
          </label>
          <div className="url-control">
            {state.status === 'success' ? (
              <input
                id="destinationUrl"
                name="destinationUrl"
                type="url"
                value={state.link.destinationUrl}
                readOnly
              />
            ) : (
              <input
                id="destinationUrl"
                name="destinationUrl"
                type="url"
                placeholder="Paste your long link here"
                autoComplete="url"
                required
              />
            )}
            <Button type="submit">
              {state.status === 'success' ? 'Shorten another' : 'Shorten'}
            </Button>
          </div>
        </form>

        {state.status !== 'idle' && state.status !== 'success' && (
          <div className={`result-panel ${state.status === 'error' ? 'is-error' : ''}`}>
            {state.message}
          </div>
        )}

        {state.status === 'success' && (
          <div ref={resultRef} className="expanded-result result-with-qr">
            <div className="qr-result-card" aria-label="QR code for short link">
              <canvas ref={qrCanvasRef} width="220" height="220" />
              <div className="qr-actions">
                <button className="action-button" type="button" onClick={downloadPngQrCode}>
                  <DownloadSimple size={16} weight="bold" />
                  PNG
                </button>
                <button className="action-button" type="button" onClick={() => void downloadSvgQrCode()}>
                  <DownloadSimple size={16} weight="bold" />
                  SVG
                </button>
              </div>
            </div>

            <div className="link-result-card">
              <LinkSummary link={state.link} />
              <div className="result-actions">
                <a className="action-button" href={state.link.shortUrl} target="_blank" rel="noreferrer">
                  <ArrowSquareOut size={16} weight="bold" />
                  Visit URL
                </a>
                <button className="action-button action-button-dark" type="button" onClick={() => void copyLink(state.link.shortUrl)}>
                  <Copy size={16} weight="bold" />
                  Copy
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {recentLinks.length > 0 && (
        <>
          <button className="scroll-cue" type="button" onClick={scrollToRecentLinks}>
            See your recent links ↓
          </button>
          <RecentLinks links={recentLinks} onCopy={copyLink} id="recent-links" />
        </>
      )}
    </section>
  )
}

function RecentLinks({
  links,
  onCopy,
  id
}: {
  links: RecentLink[]
  onCopy: (url: string) => Promise<void>
  id: string
}) {
  return (
    <section id={id} className="recent-links" aria-label="Your recent links">
      <div className="recent-links-header">
        <h2>Your recent links</h2>
      </div>
      <div className="recent-link-list">
        {links.map(link => (
          <article className="recent-link-card" key={link.id}>
            <DestinationFavicon url={link.destinationUrl} />
            <div className="recent-link-body">
              <a className="recent-short-url" href={link.shortUrl} target="_blank" rel="noreferrer">
                {link.shortUrl}
              </a>
              <span className="recent-destination-url" title={link.destinationUrl}>
                {link.destinationUrl}
              </span>
            </div>
            <div className="recent-link-actions">
              <a className="recent-link-action" href={link.shortUrl} target="_blank" rel="noreferrer" aria-label="Open recent link">
                <ArrowSquareOut size={15} weight="bold" />
              </a>
              <button className="recent-link-action" type="button" onClick={() => void onCopy(link.shortUrl)} aria-label="Copy recent link">
                <Copy size={15} weight="bold" />
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function DestinationFavicon({ url }: { url: string }) {
  const [hasError, setHasError] = useState(false)
  const faviconUrl = getFaviconUrl(url)

  if (!faviconUrl || hasError) {
    return <span className="recent-favicon recent-favicon-placeholder" aria-hidden="true" />
  }

  return (
    <img
      className="recent-favicon"
      src={faviconUrl}
      alt=""
      loading="lazy"
      onError={() => setHasError(true)}
    />
  )
}

function LinkSummary({ link }: { link: RecentLink }) {
  const shortLinkParts = getShortLinkParts(link.shortUrl)

  return (
    <div className="link-summary" aria-label="Created link summary">
      <div>
        <label className="field-label" htmlFor="shortSlug">Shorten</label>
        <div className="short-link-builder">
          <div className="short-domain-field">
            <span>{shortLinkParts.domain}</span>
          </div>
          <span className="short-link-separator">/</span>
          <input id="shortSlug" value={shortLinkParts.slug} readOnly aria-label="Short link slug" />
        </div>
      </div>
    </div>
  )
}

function getShortLinkParts(url: string) {
  try {
    const parsedUrl = new URL(url)
    return {
      domain: parsedUrl.host,
      slug: parsedUrl.pathname.replace(/^\/+/, '')
    }
  } catch {
    return {
      domain: url,
      slug: ''
    }
  }
}

function getFaviconUrl(url: string) {
  try {
    const domain = new URL(url).hostname
    return `https://www.google.com/s2/favicons?domain=${encodeURIComponent(domain)}&sz=64`
  } catch {
    return null
  }
}

function readRecentLinks(): RecentLink[] {
  const raw = localStorage.getItem(RECENT_LINKS_KEY)
  if (!raw) {
    return []
  }

  try {
    return JSON.parse(raw) as RecentLink[]
  } catch {
    localStorage.removeItem(RECENT_LINKS_KEY)
    return []
  }
}

function saveRecentLink(link: RecentLink) {
  const nextLinks = [
    link,
    ...readRecentLinks().filter(recentLink => recentLink.id !== link.id)
  ].slice(0, MaxRecentLinks)

  localStorage.setItem(RECENT_LINKS_KEY, JSON.stringify(nextLinks))
  return nextLinks
}
