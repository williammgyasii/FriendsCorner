import { apiRouteFor } from './route.ts'

export type RateLimitEnv = {
  ACCOUNT_POST: RateLimit
  ROOM_POST: RateLimit
  BILLING_POST: RateLimit
  TURN_GET: RateLimit
}

export function clientIp(request: Request): string {
  const connecting = request.headers.get('CF-Connecting-IP')
  if (connecting) {
    return connecting
  }

  const forwarded = request.headers.get('X-Forwarded-For')
  if (forwarded) {
    return forwarded.split(',')[0]!.trim()
  }

  return 'unknown'
}

export async function gateRateLimit(request: Request, env: RateLimitEnv): Promise<Response | null> {
  const url = new URL(request.url)
  const route = apiRouteFor(request.method, url.pathname)
  const ip = clientIp(request)

  let limiter: RateLimit | null = null
  switch (route) {
    case 'account':
      if (request.method === 'POST') {
        limiter = env.ACCOUNT_POST
      }
      break
    case 'room':
      if (request.method === 'POST' && url.pathname === '/rooms') {
        limiter = env.ROOM_POST
      }
      break
    case 'billing':
      if (request.method === 'POST' && url.pathname === '/billing') {
        limiter = env.BILLING_POST
      }
      break
    case 'turn':
      limiter = env.TURN_GET
      break
  }

  if (!limiter) {
    return null
  }

  const outcome = await limiter.limit({ key: ip })
  if (outcome.success) {
    return null
  }

  return new Response('', { status: 429 })
}
