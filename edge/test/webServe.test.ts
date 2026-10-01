import assert from 'node:assert/strict'
import { test } from 'node:test'
import { serveWeb } from '../web/src/serve.ts'

function fetcher(responses: Record<string, Response>): Fetcher {
  return {
    fetch(input) {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
      const path = new URL(url).pathname
      return Promise.resolve(responses[path] ?? new Response('missing', { status: 404 }))
    },
  }
}

test('client routes without static files serve the app shell', async () => {
  const assets = fetcher({
    '/login': new Response('missing', { status: 404 }),
    '/index.html': new Response('<!doctype html>', { status: 200, headers: { 'content-type': 'text/html' } }),
  })
  const api = fetcher({})

  const response = await serveWeb(new Request('https://play.friendscorner.app/login'), { ASSETS: assets, API: api })

  assert.equal(response.status, 200)
  assert.equal(await response.text(), '<!doctype html>')
})

test('missing files with an extension stay 404', async () => {
  const assets = fetcher({})
  const api = fetcher({})

  const response = await serveWeb(new Request('https://play.friendscorner.app/missing.js'), { ASSETS: assets, API: api })

  assert.equal(response.status, 404)
})

test('account requests go to the API worker', async () => {
  const assets = fetcher({})
  const api = fetcher({
    '/account': new Response('{}', { status: 200, headers: { 'content-type': 'application/json' } }),
  })

  const response = await serveWeb(new Request('https://play.friendscorner.app/account'), { ASSETS: assets, API: api })

  assert.equal(response.status, 200)
  assert.equal(await response.text(), '{}')
})
