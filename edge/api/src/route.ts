export type ApiRoute = 'room' | 'turn' | 'refuse' | 'missing'

export function apiRouteFor(method: string, pathname: string): ApiRoute {
  if (pathname === '/rooms') {
    return method === 'POST' ? 'room' : 'refuse'
  }

  if (pathname.startsWith('/ws/')) {
    return method === 'GET' ? 'room' : 'refuse'
  }

  if (pathname === '/turn') {
    return method === 'GET' ? 'turn' : 'refuse'
  }

  return 'missing'
}
