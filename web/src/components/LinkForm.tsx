import QRCode from 'qrcode'
import { ArrowSquareOut, Copy, DownloadSimple, Link as LinkIcon, QrCode } from '@phosphor-icons/react'
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

type ToolTab = 'shorten' | 'qr'

export function LinkForm() {
  const [state, setState] = useState<FormState>({ status: 'idle' })
  const [activeTab, setActiveTab] = useState<ToolTab>('shorten')
  const qrCanvasRef = useRef<HTMLCanvasElement | null>(null)

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
  }, [activeTab, state])

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
      void nextRecentLinks
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

  function downloadQrCode() {
    const canvas = qrCanvasRef.current
    if (!canvas || state.status !== 'success') {
      return
    }

    const link = document.createElement('a')
    link.href = canvas.toDataURL('image/png')
    link.download = `shorth-${state.link.slug}.png`
    link.click()
  }

  return (
    <section className="shorten-area" aria-label="Create short link">
      <div className="shorten-card">
        <div className="tool-tabs" role="tablist" aria-label="Choose link tool">
          <button
            className={activeTab === 'shorten' ? 'is-active' : ''}
            type="button"
            onClick={() => setActiveTab('shorten')}
          >
            <LinkIcon size={26} weight="bold" />
            Shorten a Link
          </button>
          <button
            className={activeTab === 'qr' ? 'is-active' : ''}
            type="button"
            onClick={() => setActiveTab('qr')}
          >
            <QrCode size={26} weight="bold" />
            Generate QR Code
          </button>
        </div>

        <form className="shorten-form" onSubmit={handleSubmit}>
          <label className="field-label" htmlFor="destinationUrl">
            Long URL <span>*</span>
          </label>
          <div className="url-control">
            <input
              id="destinationUrl"
              name="destinationUrl"
              type="url"
              placeholder="Paste your long link here"
              autoComplete="url"
              required
            />
            <Button type="submit">
              {state.status === 'success'
                ? activeTab === 'qr' ? 'Generate another QR code' : 'Shorten another link'
                : activeTab === 'qr' ? 'Generate QR Code' : 'Shorten Link'}
            </Button>
          </div>
        </form>
      </div>

      {state.status !== 'idle' && state.status !== 'success' && (
        <div className={`result-panel ${state.status === 'error' ? 'is-error' : ''}`}>
          {state.message}
        </div>
      )}

      {state.status === 'success' && activeTab === 'shorten' && (
        <div className="expanded-result">
          <label className="field-label" htmlFor="shortUrl">Shorth Link</label>
          <div className="readonly-link-row">
            <input id="shortUrl" value={state.link.shortUrl} readOnly />
            <button className="icon-button" type="button" onClick={() => void copyLink(state.link.shortUrl)} aria-label="Copy short link">
              <Copy size={20} weight="bold" />
            </button>
          </div>
          <div className="result-actions">
            <a className="action-button" href={state.link.shortUrl} target="_blank" rel="noreferrer">
              <ArrowSquareOut size={18} weight="bold" />
              Visit URL
            </a>
            <button className="action-button action-button-dark" type="button" onClick={() => void copyLink(state.link.shortUrl)}>
              <Copy size={18} weight="bold" />
              Copy
            </button>
          </div>
        </div>
      )}

      {state.status === 'success' && activeTab === 'qr' && (
        <div className="expanded-result qr-expanded-result">
          <div className="qr-preview">
            <canvas ref={qrCanvasRef} width="220" height="220" aria-label="QR code for short link" />
          </div>
          <div className="qr-download-panel">
            <h2>Download your QR code</h2>
            <button className="action-button action-button-dark" type="button" onClick={downloadQrCode}>
              <DownloadSimple size={18} weight="bold" />
              Download PNG
            </button>
          </div>
        </div>
      )}
    </section>
  )
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
