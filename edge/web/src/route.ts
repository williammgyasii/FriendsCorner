export type WebRoute = 'api' | 'assets'

export function webRouteFor(pathname: string): WebRoute {
  if (pathname === '/rooms' || pathname === '/turn' || pathname.startsWith('/ws/')) {
    return 'api'
  }

  return 'assets'
}
