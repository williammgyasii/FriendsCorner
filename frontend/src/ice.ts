export type IceServer = {
  urls: string[]
  username?: string
  credential?: string
}

export const fallbackIceServers: IceServer[] = [{ urls: ['stun:stun.cloudflare.com:3478'] }]

// Browsers block port 53, so those URLs only time out.
const blockedPort = /:53(\?|$)/

export function iceServersFrom(body: unknown): IceServer[] {
  if (!body || typeof body !== 'object' || !Array.isArray((body as { iceServers?: unknown }).iceServers)) {
    return fallbackIceServers
  }

  const servers = (body as { iceServers: unknown[] }).iceServers.flatMap((entry): IceServer[] => {
    if (!entry || typeof entry !== 'object') {
      return []
    }
    const { urls, username, credential } = entry as { urls?: unknown; username?: unknown; credential?: unknown }
    const list = (Array.isArray(urls) ? urls : [urls]).filter(
      (url): url is string => typeof url === 'string' && !blockedPort.test(url),
    )
    if (list.length === 0) {
      return []
    }
    const server: IceServer = { urls: list }
    if (typeof username === 'string' && typeof credential === 'string') {
      server.username = username
      server.credential = credential
    }
    return [server]
  })

  return servers.length > 0 ? servers : fallbackIceServers
}
