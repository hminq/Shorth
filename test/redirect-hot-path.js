import http from 'k6/http'
import { check } from 'k6'
import { Counter } from 'k6/metrics'

http.setResponseCallback(http.expectedStatuses({ min: 300, max: 399 }))

const baseUrl = __ENV.BASE_URL ?? 'https://shorth.link'
const slug = __ENV.SLUG
const failedSamples = Number(__ENV.FAILED_SAMPLES ?? '10')
const redirectStatuses = [301, 302, 303, 307, 308]
const referrers = [
  { source: 'facebook', value: 'https://www.facebook.com/' },
  { source: 'instagram', value: 'https://www.instagram.com/' },
  { source: 'x', value: 'https://x.com/' },
  { source: 'tiktok', value: 'https://www.tiktok.com/' }
]

const statusCounter = new Counter('redirect_status_code')
const failedStatusCounter = new Counter('redirect_failed_status_code')

let loggedFailedSamples = 0

if (!slug) {
  throw new Error('SLUG is required. Example: k6 run -e SLUG=abc123 redirect-hot-path.js')
}

export const options = {
  maxRedirects: 0,
  scenarios: {
    redirect_hot_path: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 250,
      stages: [
        { duration: '20s', target: 100 },
        { duration: '40s', target: 200 },
        { duration: '40s', target: 200 },
        { duration: '20s', target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200']
  }
}

export default function () {
  const referrer = referrers[Math.floor(Math.random() * referrers.length)]
  const response = http.get(`${baseUrl}/${slug}`, {
    headers: {
      Referer: referrer.value
    },
    tags: {
      endpoint: 'redirect-hot-path',
      referrer_source: referrer.source
    }
  })
  const hasLocationHeader = Boolean(response.headers.Location ?? response.headers.location)
  const isRedirect = redirectStatuses.includes(response.status)

  statusCounter.add(1, { status: String(response.status) })

  if (!isRedirect || !hasLocationHeader) {
    failedStatusCounter.add(1, {
      status: String(response.status),
      has_location: String(hasLocationHeader)
    })

    if (loggedFailedSamples < failedSamples) {
      loggedFailedSamples += 1
      console.log(
        `FAIL status=${response.status} hasLocation=${hasLocationHeader} location=${response.headers.Location ?? response.headers.location ?? 'none'}`
      )
    }
  }

  check(response, {
    'returns redirect': () => isRedirect,
    'has location header': () => hasLocationHeader
  })
}
