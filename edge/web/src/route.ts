export type WebRoute = 'api' | 'assets'

export function webRouteFor(pathname: string): WebRoute {
  if (
    pathname === '/rooms' ||
    pathname === '/account' ||
    pathname === '/billing' ||
    pathname === '/billing/webhook' ||
    pathname === '/turn' ||
    pathname.startsWith('/ws/')
  ) {
    return 'api'
  }

  return 'assets'
}
