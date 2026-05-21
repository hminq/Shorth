import http from 'k6/http'
import { check } from 'k6'
import { Trend } from 'k6/metrics'

http.setResponseCallback(http.expectedStatuses({ min: 300, max: 399 }))

const baseUrl = __ENV.BASE_URL ?? 'https://shorth.link'
const slug = __ENV.SLUG
const referer = __ENV.REFERER ?? 'https://www.facebook.com/'
const redirectStatuses = [301, 302, 303, 307, 308]

const redirectDuration = new Trend('single_redirect_duration', true)

if (!slug) {
  throw new Error('SLUG is required. Example: k6 run -e SLUG=abc123 redirect-single-request.js')
}

export const options = {
  maxRedirects: 0,
  vus: 1,
  iterations: 1,
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200']
  }
}

export default function () {
  const response = http.get(`${baseUrl}/${slug}`, {
    headers: {
      Referer: referer
    },
    tags: {
      endpoint: 'redirect-single-request'
    }
  })
  const hasLocationHeader = Boolean(response.headers.Location ?? response.headers.location)
  const isRedirect = redirectStatuses.includes(response.status)

  redirectDuration.add(response.timings.duration)

  if (!isRedirect || !hasLocationHeader) {
    console.log(
      `FAIL status=${response.status} hasLocation=${hasLocationHeader} location=${response.headers.Location ?? response.headers.location ?? 'none'}`
    )
  }

  check(response, {
    'returns redirect': () => isRedirect,
    'has location header': () => hasLocationHeader
  })
}
