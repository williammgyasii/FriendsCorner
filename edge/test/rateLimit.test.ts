import assert from 'node:assert/strict'
import { test } from 'node:test'
import { clientIp, gateRateLimit, type RateLimitEnv } from '../api/src/rateLimit.ts'

function limiter(max: number): RateLimit {
  const counts = new Map<string, number>()
  return {
    limit({ key }) {
      const count = (counts.get(key) ?? 0) + 1
      counts.set(key, count)
      return Promise.resolve({ success: count <= max })
    },
  }
}

function env(overrides: Partial<RateLimitEnv> = {}): RateLimitEnv {
  return {
    ACCOUNT_POST: limiter(10),
    ROOM_POST: limiter(20),
    BILLING_POST: limiter(20),
    TURN_GET: limiter(60),
    ...overrides,
  }
}

function post(path: string, ip = '203.0.113.1') {
  return new Request(`https://play.friendscorner.app${path}`, {
    method: 'POST',
    headers: { 'CF-Connecting-IP': ip },
  })
}

test('client IP prefers CF-Connecting-IP', () => {
  const request = new Request('https://play.friendscorner.app/account', {
    headers: {
      'CF-Connecting-IP': '203.0.113.9',
      'X-Forwarded-For': '198.51.100.2',
    },
  })
  assert.equal(clientIp(request), '203.0.113.9')
})

test('POST /account is limited to ten per IP per minute', async () => {
  const limits = env()
  for (let i = 0; i < 10; i++) {
    assert.equal(await gateRateLimit(post('/account'), limits), null)
  }
  const blocked = await gateRateLimit(post('/account'), limits)
  assert.ok(blocked)
  assert.equal(blocked!.status, 429)
  assert.equal(await blocked!.text(), '')
})

test('GET /account is not rate limited', async () => {
  const limits = env({
    ACCOUNT_POST: limiter(0),
  })
  const request = new Request('https://play.friendscorner.app/account', {
    method: 'GET',
    headers: { 'CF-Connecting-IP': '203.0.113.1' },
  })
  assert.equal(await gateRateLimit(request, limits), null)
})

test('POST /billing/webhook is not rate limited', async () => {
  const limits = env({
    BILLING_POST: limiter(0),
  })
  assert.equal(await gateRateLimit(post('/billing/webhook'), limits), null)
})

test('POST /billing is rate limited', async () => {
  const limits = env({ BILLING_POST: limiter(1) })
  assert.equal(await gateRateLimit(post('/billing'), limits), null)
  const blocked = await gateRateLimit(post('/billing'), limits)
  assert.equal(blocked?.status, 429)
})
