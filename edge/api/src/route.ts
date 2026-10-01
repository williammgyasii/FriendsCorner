export type ApiRoute = 'room' | 'account' | 'billing' | 'turn' | 'refuse' | 'missing'

export function apiRouteFor(method: string, pathname: string): ApiRoute {
  if (pathname === '/rooms') {
    return method === 'POST' ? 'room' : 'refuse'
  }

  if (pathname.startsWith('/ws/')) {
    return method === 'GET' ? 'room' : 'refuse'
  }

  if (pathname === '/account') {
    return method === 'GET' || method === 'POST' ? 'account' : 'refuse'
  }

  if (pathname === '/billing' || pathname === '/billing/webhook') {
    return method === 'POST' ? 'billing' : 'refuse'
  }

  if (pathname === '/turn') {
    return method === 'GET' ? 'turn' : 'refuse'
  }

  return 'missing'
}
