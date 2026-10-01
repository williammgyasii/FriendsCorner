import { Container, getContainer } from '@cloudflare/containers'
import { apiRouteFor } from './route.ts'

type Env = {
  ROOM_API: DurableObjectNamespace<RoomApi>
  DATABASE_URL: string
  TURN_KEY_ID: string
  TURN_KEY_API_TOKEN: string
  OPENAI_API_KEY: string
  AUTH_TICKET_KEY: string
  STRIPE_SECRET_KEY: string
  STRIPE_WEBHOOK_SECRET: string
  STRIPE_PRICE_CORNER: string
  STRIPE_PRICE_TABLE: string
  STRIPE_PRICE_HOUSE: string
}

const roomId = /^[0-9a-f]{32}$/

// Rooms live in the API's memory, so every request goes to one instance.
export class RoomApi extends Container<Env> {
  defaultPort = 8080
  sleepAfter = '30m'
  // Set once with: npx wrangler secret put OPENAI_API_KEY -c api/wrangler.jsonc
  // and: npx wrangler secret put AUTH_TICKET_KEY -c api/wrangler.jsonc
  envVars = {
    ConnectionStrings__FriendsCorner: this.env.DATABASE_URL,
    OpenAI__ApiKey: this.env.OPENAI_API_KEY,
    Auth__TicketKey: this.env.AUTH_TICKET_KEY,
    Stripe__SecretKey: this.env.STRIPE_SECRET_KEY,
    Stripe__WebhookSecret: this.env.STRIPE_WEBHOOK_SECRET,
    Stripe__Prices__Corner: this.env.STRIPE_PRICE_CORNER,
    Stripe__Prices__Table: this.env.STRIPE_PRICE_TABLE,
    Stripe__Prices__House: this.env.STRIPE_PRICE_HOUSE,
  }
}

// No public hostname: only Workers holding a service binding can reach this.
export default {
  async fetch(request, env): Promise<Response> {
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
  },
} satisfies ExportedHandler<Env>

// Holding a live room link is the lock: no open room, no TURN credentials.
async function turn(url: URL, env: Env): Promise<Response> {
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
