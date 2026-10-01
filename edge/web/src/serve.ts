import { webRouteFor } from './route.ts'

type WebEnv = {
  ASSETS: Fetcher
  API: Fetcher
}

export async function serveWeb(request: Request, env: WebEnv): Promise<Response> {
  const url = new URL(request.url)
  if (webRouteFor(url.pathname) === 'api') {
    return env.API.fetch(request)
  }

  const asset = await env.ASSETS.fetch(request)
  if (asset.status !== 404) {
    return asset
  }

  // Client routes like /login and /register have no static file; serve the app shell.
  if (url.pathname.includes('.')) {
    return asset
  }

  const shell = new URL('/index.html', url.origin)
  return env.ASSETS.fetch(new Request(shell, { headers: request.headers, method: request.method }))
}
