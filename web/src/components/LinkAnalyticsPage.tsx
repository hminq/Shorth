import { useEffect, useMemo, useRef, useState, type CSSProperties, type PointerEvent } from 'react'
import createGlobe, { type Globe } from 'cobe'
import {
  FacebookLogo,
  TwitterLogo,
  type Icon
} from '@phosphor-icons/react'
import { fetchLinkAnalytics, type LinkAnalyticsResponse } from '../lib/api'
import instagramLogoUrl from '../assets/instagram-logo.svg'
import tiktokLogoUrl from '../assets/tiktok-logo.svg'
import { Footer } from './Footer'
import { Header } from './Header'

type AnalyticsWindow = {
  from: string
  to: string
}

type AnalyticsWindowValidation = {
  windowRange: AnalyticsWindow
  message: string | null
}

type AnalyticsState =
  | { status: 'loading'; message: string }
  | { status: 'ready'; payload: LinkAnalyticsResponse }
  | { status: 'error'; message: string }

type GlobeRotation = {
  lat: number
  lng: number
}

const countryCoordinates: Record<string, { lat: number; lng: number }> = {
  AU: { lat: -25.27, lng: 133.78 },
  BR: { lat: -14.24, lng: -51.93 },
  CA: { lat: 56.13, lng: -106.35 },
  DE: { lat: 51.17, lng: 10.45 },
  FR: { lat: 46.23, lng: 2.21 },
  GB: { lat: 55.38, lng: -3.44 },
  IN: { lat: 20.59, lng: 78.96 },
  JP: { lat: 36.2, lng: 138.25 },
  KR: { lat: 35.91, lng: 127.77 },
  SG: { lat: 1.35, lng: 103.82 },
  US: { lat: 37.09, lng: -95.71 },
  VN: { lat: 14.06, lng: 108.28 }
}

const countryColors = [
  { rgb: [0.96, 0.31, 0] as [number, number, number], css: '#f54e00' },
  { rgb: [0.02, 0.1, 0.21] as [number, number, number], css: '#011936' },
  { rgb: [0.1, 0.52, 0.29] as [number, number, number], css: '#1a854a' }
]

const referrerIcons: Partial<Record<LinkAnalyticsResponse['topReferrers'][number]['source'], Icon>> = {
  facebook: FacebookLogo,
  x: TwitterLogo
}

const referrerColors: Record<LinkAnalyticsResponse['topReferrers'][number]['source'], string> = {
  facebook: '#1877f2',
  instagram: '#e4405f',
  x: '#1da1f2',
  tiktok: '#000000'
}

const maxAnalyticsWindowDays = 366
const analyticsMessageMs = 1500

export function LinkAnalyticsPage() {
  const linkId = useMemo(() => getLinkIdFromPath(), [])
  const [windowRange, setWindowRange] = useState<AnalyticsWindow>(() => getDefaultWindow())
  const [windowMessage, setWindowMessage] = useState<string | null>(null)
  const [state, setState] = useState<AnalyticsState>({ status: 'loading', message: 'Loading analytics...' })

  useEffect(() => {
    if (!windowMessage) {
      return
    }

    const timeoutId = window.setTimeout(() => setWindowMessage(null), analyticsMessageMs)
    return () => window.clearTimeout(timeoutId)
  }, [windowMessage])

  useEffect(() => {
    async function loadAnalytics() {
      if (!linkId) {
        setState({ status: 'error', message: 'We could not find what you requested.' })
        return
      }

      try {
        setState({ status: 'loading', message: 'Loading analytics...' })
        const payload = await fetchLinkAnalytics(linkId, windowRange)
        setState({ status: 'ready', payload })
      } catch (error) {
        setState({
          status: 'error',
          message: error instanceof Error ? error.message : 'Could not load analytics.'
        })
      }
    }

    void loadAnalytics()
  }, [linkId, windowRange])

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
          {state.status === 'ready' && (
            <AnalyticsPanel
              analytics={state.payload}
              windowRange={windowRange}
              windowMessage={windowMessage}
              onWindowChange={nextWindowRange => {
                const validation = normalizeAnalyticsWindow(nextWindowRange)
                setWindowRange(validation.windowRange)
                setWindowMessage(validation.message)
              }}
            />
          )}
        </section>
      </section>

      <Footer />
    </main>
  )
}

function AnalyticsPanel({
  analytics,
  windowRange,
  windowMessage,
  onWindowChange
}: {
  analytics: LinkAnalyticsResponse
  windowRange: AnalyticsWindow
  windowMessage: string | null
  onWindowChange: (windowRange: AnalyticsWindow) => void
}) {
  return (
    <div className="link-analytics-panel">
      <div className="link-analytics-top">
        <div className="analytics-stat-field">
          <span>Total clicks</span>
          <strong>{analytics.totalClicks}</strong>
        </div>

        <AnalyticsWindowControls windowRange={windowRange} onWindowChange={onWindowChange} />
      </div>
      {windowMessage && <p className="auth-message is-error analytics-window-message">{windowMessage}</p>}

      <div className="link-analytics-grid">
        <div className="analytics-main-column">
          <section className="analytics-referrer-section" aria-label="Top referrers">
            <h2>Top referrers</h2>
            <TopReferrersPanel referrers={analytics.topReferrers} />
          </section>

          <section aria-label="Daily clicks">
            <h2>Daily clicks</h2>
            {analytics.daily.length === 0 ? (
              <p className="empty-state">No clicks in this window.</p>
            ) : (
              <DailyClicksChart daily={analytics.daily} />
            )}
          </section>
        </div>

        <section className="analytics-country-section" aria-label="Top countries">
          <h2>Top countries</h2>
          <TopCountriesPanel countries={analytics.topCountries} />
        </section>
      </div>
    </div>
  )
}

function TopReferrersPanel({ referrers }: { referrers: LinkAnalyticsResponse['topReferrers'] }) {
  return (
    <div className="referrer-list">
      {referrers.map(referrer => {
        const ReferrerIcon = referrerIcons[referrer.source]

        return (
          <article
            className={`referrer-row referrer-row-${referrer.source}`}
            key={referrer.source}
            style={{ '--referrer-color': referrerColors[referrer.source] } as CSSProperties}
          >
            <span className="referrer-icon" aria-label={referrer.label}>
              {referrer.source === 'instagram' || referrer.source === 'tiktok' ? (
                <img src={referrer.source === 'instagram' ? instagramLogoUrl : tiktokLogoUrl} alt="" />
              ) : ReferrerIcon ? (
                <ReferrerIcon size={32} weight={['facebook', 'x'].includes(referrer.source) ? 'fill' : 'bold'} />
              ) : null}
            </span>
            <strong>{referrer.clicks}</strong>
          </article>
        )
      })}
    </div>
  )
}

function AnalyticsWindowControls({
  windowRange,
  onWindowChange
}: {
  windowRange: AnalyticsWindow
  onWindowChange: (windowRange: AnalyticsWindow) => void
}) {
  const updateDate = (field: keyof AnalyticsWindow, value: string) => {
    const next = { ...windowRange, [field]: value }

    if (!next.from || !next.to || next.from > next.to) {
      return
    }

    onWindowChange(next)
  }

  return (
    <div className="analytics-window-controls" aria-label="Analytics window">
      <label>
        From
        <input
          type="date"
          value={windowRange.from}
          max={windowRange.to}
          onChange={event => updateDate('from', event.target.value)}
        />
      </label>
      <label>
        To
        <input
          type="date"
          value={windowRange.to}
          min={windowRange.from}
          onChange={event => updateDate('to', event.target.value)}
        />
      </label>
    </div>
  )
}

function DailyClicksChart({ daily }: { daily: LinkAnalyticsResponse['daily'] }) {
  const scrollRef = useRef<HTMLDivElement | null>(null)
  const dragStartRef = useRef<{ x: number; scrollLeft: number } | null>(null)
  const [tooltip, setTooltip] = useState<{ date: string; clicks: number; x: number; y: number } | null>(null)
  const maxClicks = Math.max(...daily.map(day => day.clicks), 1)

  const startDrag = (event: PointerEvent<HTMLDivElement>) => {
    event.currentTarget.setPointerCapture(event.pointerId)
    dragStartRef.current = {
      x: event.clientX,
      scrollLeft: event.currentTarget.scrollLeft
    }
  }

  const drag = (event: PointerEvent<HTMLDivElement>) => {
    const start = dragStartRef.current
    const target = scrollRef.current

    if (!start || !target) {
      return
    }

    target.scrollLeft = start.scrollLeft - (event.clientX - start.x)
  }

  const stopDrag = (event: PointerEvent<HTMLDivElement>) => {
    event.currentTarget.releasePointerCapture(event.pointerId)
    dragStartRef.current = null
  }

  return (
    <div className="analytics-chart" aria-label="Daily click chart">
      {tooltip && (
        <div className="analytics-chart-tooltip" style={{ left: `${tooltip.x}px`, top: `${tooltip.y}px` }}>
          <span>{formatDate(tooltip.date)}</span>
          <strong>{tooltip.clicks} clicks</strong>
        </div>
      )}
      <div
        ref={scrollRef}
        className="analytics-bars"
        style={{ gridTemplateColumns: `repeat(${daily.length}, minmax(14px, 1fr))` }}
        onPointerDown={startDrag}
        onPointerMove={drag}
        onPointerUp={stopDrag}
        onPointerCancel={stopDrag}
      >
        {daily.map(day => (
          <span
            className="analytics-bar"
            key={day.date}
            style={{ height: `${Math.max((day.clicks / maxClicks) * 100, day.clicks > 0 ? 8 : 0)}%` }}
            aria-label={`${formatDate(day.date)}: ${day.clicks} clicks`}
            onPointerEnter={event => setTooltip(getChartTooltipPosition(event.currentTarget, day))}
            onPointerMove={event => setTooltip(getChartTooltipPosition(event.currentTarget, day))}
            onPointerLeave={() => setTooltip(null)}
          />
        ))}
      </div>
      <div className="analytics-chart-axis">
        <span>{formatDate(daily[0]?.date ?? '')}</span>
        <span>{formatDate(daily[daily.length - 1]?.date ?? '')}</span>
      </div>
    </div>
  )
}

function getChartTooltipPosition(
  target: Element,
  day: LinkAnalyticsResponse['daily'][number]
) {
  const container = target.closest('.analytics-chart')

  if (!container) {
    return {
      date: day.date,
      clicks: day.clicks,
      x: 0,
      y: 0
    }
  }

  const targetRect = target.getBoundingClientRect()
  const containerRect = container.getBoundingClientRect()
  const center = targetRect.left + targetRect.width / 2 - containerRect.left
  const top = targetRect.top - containerRect.top - 64

  return {
    date: day.date,
    clicks: day.clicks,
    x: clamp(center, 112, containerRect.width - 112),
    y: clamp(top, 12, containerRect.height - 86)
  }
}

function TopCountriesPanel({ countries }: { countries: LinkAnalyticsResponse['topCountries'] }) {
  const [rotation, setRotation] = useState<GlobeRotation>({ lat: 8, lng: 105 })

  const focusCountry = (countryCode: string) => {
    const coordinates = countryCoordinates[countryCode.toUpperCase()]

    if (!coordinates) {
      return
    }

    setRotation({
      lat: clamp(coordinates.lat, -65, 65),
      lng: normalizeLng(coordinates.lng)
    })
  }

  return (
    <div className="analytics-country-panel">
      <CountryGlobe countries={countries} rotation={rotation} onRotationChange={setRotation} />
      {countries.length === 0 ? (
        <p className="empty-state">No country data yet.</p>
      ) : (
        <div className="country-list">
          {countries.map((country, index) => {
            const color = countryColors[index % countryColors.length].css

            return (
              <button
                className="country-row"
                key={country.countryCode}
                type="button"
                onClick={() => focusCountry(country.countryCode)}
              >
                <span>{country.countryCode}</span>
                <div className="country-bar" aria-hidden="true">
                  <span style={{ width: `${Math.min(country.percent, 100)}%`, background: color }} />
                </div>
                <strong>{country.percent.toFixed(2)}%</strong>
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}

function CountryGlobe({
  countries,
  rotation,
  onRotationChange
}: {
  countries: LinkAnalyticsResponse['topCountries']
  rotation: GlobeRotation
  onRotationChange: (rotation: GlobeRotation) => void
}) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const globeRef = useRef<Globe | null>(null)
  const dragStartRef = useRef<{ x: number; y: number; rotation: GlobeRotation } | null>(null)
  const markers = useMemo(() => countries
    .map((country, index) => {
      const coordinates = countryCoordinates[country.countryCode.toUpperCase()]

      if (!coordinates) {
        return null
      }

      return {
        location: [coordinates.lat, coordinates.lng] as [number, number],
        size: 0.035 + Math.min(country.percent, 100) / 1300,
        color: countryColors[index % countryColors.length].rgb
      }
    })
    .filter(marker => marker !== null), [countries])

  useEffect(() => {
    const canvas = canvasRef.current

    if (!canvas) {
      return
    }

    const globe = createGlobe(canvas, {
      width: 560,
      height: 560,
      phi: toCobePhi(rotation.lng),
      theta: toGlobeRadians(rotation.lat),
      dark: 0,
      diffuse: 1.1,
      mapSamples: 16000,
      mapBrightness: 4.2,
      mapBaseBrightness: 0.12,
      baseColor: [0.9, 0.94, 0.98],
      markerColor: [0.96, 0.31, 0],
      glowColor: [1, 1, 1],
      markers,
      markerElevation: 0.04,
      devicePixelRatio: Math.min(window.devicePixelRatio || 1, 2),
      scale: 1,
      opacity: 1
    })

    globeRef.current = globe

    return () => {
      globe.destroy()
      globeRef.current = null
    }
  }, [])

  useEffect(() => {
    globeRef.current?.update({
      markers,
      phi: toCobePhi(rotation.lng),
      theta: toGlobeRadians(rotation.lat)
    })
  }, [markers, rotation])

  const startDrag = (event: PointerEvent<HTMLCanvasElement>) => {
    event.currentTarget.setPointerCapture(event.pointerId)
    dragStartRef.current = {
      x: event.clientX,
      y: event.clientY,
      rotation
    }
  }

  const drag = (event: PointerEvent<HTMLCanvasElement>) => {
    const start = dragStartRef.current

    if (!start) {
      return
    }

    onRotationChange({
      lng: normalizeLng(start.rotation.lng - (event.clientX - start.x) * 0.7),
      lat: clamp(start.rotation.lat + (event.clientY - start.y) * 0.45, -65, 65)
    })
  }

  const stopDrag = (event: PointerEvent<HTMLCanvasElement>) => {
    event.currentTarget.releasePointerCapture(event.pointerId)
    dragStartRef.current = null
  }

  return (
    <div className="analytics-globe">
      <canvas
        ref={canvasRef}
        className="analytics-globe-canvas"
        width="280"
        height="280"
        aria-label="Interactive country globe"
        onPointerDown={startDrag}
        onPointerMove={drag}
        onPointerUp={stopDrag}
        onPointerCancel={stopDrag}
      />
    </div>
  )
}

function toGlobeRadians(value: number) {
  return value * Math.PI / 180
}

function toCobePhi(longitude: number) {
  return toGlobeRadians(270 - longitude)
}

function normalizeLng(value: number) {
  if (value > 180) {
    return value - 360
  }

  if (value < -180) {
    return value + 360
  }

  return value
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max)
}

function getLinkIdFromPath() {
  const match = window.location.pathname.match(/^\/links\/([^/]+)\/analytics$/)
  return match?.[1] ?? null
}

function getDefaultWindow(): AnalyticsWindow {
  const to = new Date()
  const from = new Date()
  from.setDate(to.getDate() - 29)

  return {
    from: formatDateInput(from),
    to: formatDateInput(to)
  }
}

function formatDateInput(value: Date) {
  const year = value.getFullYear()
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const day = `${value.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

function normalizeAnalyticsWindow(windowRange: AnalyticsWindow): AnalyticsWindowValidation {
  const today = startOfLocalDay(new Date())
  const fromDate = parseDateInput(windowRange.from)
  const toDate = parseDateInput(windowRange.to)

  if (!fromDate || !toDate) {
    return {
      windowRange: getDefaultWindow(),
      message: 'Use a valid date range.'
    }
  }

  let normalizedFrom = fromDate
  let normalizedTo = toDate
  let message: string | null = null

  if (normalizedTo > today) {
    normalizedTo = today
    message = 'Date range was adjusted.'
  }

  if (normalizedFrom > normalizedTo) {
    normalizedFrom = normalizedTo
    message = 'Date range was adjusted.'
  }

  const rangeDays = getDayDiff(normalizedFrom, normalizedTo) + 1
  if (rangeDays > maxAnalyticsWindowDays) {
    normalizedFrom = new Date(normalizedTo)
    normalizedFrom.setDate(normalizedTo.getDate() - (maxAnalyticsWindowDays - 1))
    message = `Date range was limited to ${maxAnalyticsWindowDays} days.`
  }

  return {
    windowRange: {
      from: formatDateInput(normalizedFrom),
      to: formatDateInput(normalizedTo)
    },
    message
  }
}

function parseDateInput(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return null
  }

  const parsed = startOfLocalDay(new Date(`${value}T00:00:00`))
  return Number.isNaN(parsed.getTime()) ? null : parsed
}

function startOfLocalDay(value: Date) {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate())
}

function getDayDiff(from: Date, to: Date) {
  const millisecondsPerDay = 24 * 60 * 60 * 1000
  return Math.round((to.getTime() - from.getTime()) / millisecondsPerDay)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  }).format(new Date(value))
}
