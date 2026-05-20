import http from 'k6/http'
import { check, sleep } from 'k6'

http.setResponseCallback(http.expectedStatuses({ min: 300, max: 399 }))

const baseUrl = __ENV.BASE_URL ?? 'https://shorth.link'
const slug = __ENV.SLUG

if (!slug) {
  throw new Error('SLUG is required. Example: k6 run -e SLUG=abc123 redirect-hot-path.js')
}

export const options = {
  maxRedirects: 0,
  stages: [
    { duration: '15s', target: 10 },
    { duration: '30s', target: 20 },
    { duration: '15s', target: 0 }
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200']
  }
}

export default function () {
  const response = http.get(`${baseUrl}/${slug}`, {
    tags: {
      endpoint: 'redirect-hot-path'
    }
  })

  check(response, {
    'returns redirect': r => [301, 302, 303, 307, 308].includes(r.status),
    'has location header': r => Boolean(r.headers.Location ?? r.headers.location)
  })

  sleep(0.1)
}
