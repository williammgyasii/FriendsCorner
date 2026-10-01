import { Container } from '@cloudflare/containers'
import { serveApi } from './serve.ts'

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
  ACCOUNT_POST: RateLimit
  ROOM_POST: RateLimit
  BILLING_POST: RateLimit
  TURN_GET: RateLimit
}

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
  fetch: serveApi,
} satisfies ExportedHandler<Env>
