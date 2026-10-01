import { getContainer } from '@cloudflare/containers'
import { gateRateLimit, type RateLimitEnv } from './rateLimit.ts'
import { apiRouteFor } from './route.ts'

type ApiEnv = RateLimitEnv & {
  ROOM_API: DurableObjectNamespace
  TURN_KEY_ID: string
  TURN_KEY_API_TOKEN: string
}

const roomId = /^[0-9a-f]{32}$/

export async function serveApi(request: Request, env: ApiEnv): Promise<Response> {
  const limited = await gateRateLimit(request, env)
  if (limited) {
    return limited
  }

  const url = new URL(request.url)
  switch (apiRouteFor(request.method, url.pathname)) {
    case 'room':
    case 'account':
    case 'billing':
      return getContainer(env.ROOM_API).fetch(request)
    case 'turn':
      return turn(url, env)
    case 'refuse':
      return new Response('Method not allowed', { status: 405 })
    default:
      return new Response('Not found', { status: 404 })
  }
}

async function turn(url: URL, env: ApiEnv): Promise<Response> {
  const room = url.searchParams.get('room') ?? ''
  if (!roomId.test(room)) {
    return new Response('Unknown room', { status: 404 })
  }

  const open = await getContainer(env.ROOM_API).fetch(new Request(`http://room-api/rooms/${room}`))
  if (open.status !== 204) {
    return new Response('Unknown room', { status: 404 })
  }

  const response = await fetch(
    `https://rtc.live.cloudflare.com/v1/turn/keys/${env.TURN_KEY_ID}/credentials/generate-ice-servers`,
    {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${env.TURN_KEY_API_TOKEN}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ ttl: 86400 }),
    },
  )
  if (!response.ok) {
    return new Response('TURN unavailable', { status: 502 })
  }

  return new Response(response.body, {
    headers: { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' },
  })
}
