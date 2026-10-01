import { serveWeb } from './serve.ts'

type Env = {
  ASSETS: Fetcher
  API: Fetcher
}

export default {
  fetch: serveWeb,
} satisfies ExportedHandler<Env>
