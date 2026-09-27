import { webRouteFor } from './route.ts'

type Env = {
  ASSETS: Fetcher
  API: Fetcher
}

export default {
  async fetch(request, env): Promise<Response> {
    const url = new URL(request.url)
    if (webRouteFor(url.pathname) === 'api') {
      return env.API.fetch(request)
    }

    return env.ASSETS.fetch(request)
  },
} satisfies ExportedHandler<Env>
