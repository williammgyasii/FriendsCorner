export type DoorField = 'name' | 'gameName' | 'email' | 'password'

export type DoorView =
  | { kind: 'redirect'; to: string }
  | { kind: 'enter'; room: string }
  | { kind: 'home' }
  | {
      kind: 'login' | 'register'
      title: string
      fields: DoorField[]
      action: string
      link: { href: string; label: string }
      fonts: { title: 'Bungee'; form: 'Fredoka' }
    }

const fonts = { title: 'Bungee', form: 'Fredoka' } as const

export function doorLook(pathname: string, search: string, signedIn: boolean): DoorView {
  const room = new URLSearchParams(search).get('room')
  if (signedIn && room) {
    return { kind: 'enter', room }
  }
  if (signedIn) {
    return { kind: 'home' }
  }
  if (pathname === '/register') {
    return {
      kind: 'register',
      title: 'Create account',
      fields: ['name', 'gameName', 'email', 'password'],
      action: 'Create account',
      link: { href: '/login', label: 'Sign in' },
      fonts,
    }
  }
  if (pathname === '/login') {
    return {
      kind: 'login',
      title: 'Sign in',
      fields: ['email', 'password'],
      action: 'Sign in',
      link: { href: '/register', label: 'Create account' },
      fonts,
    }
  }

  if (room) {
    return { kind: 'redirect', to: `/register?room=${encodeURIComponent(room)}` }
  }

  const plan = new URLSearchParams(search).get('plan')
  return { kind: 'redirect', to: plan ? `/login?plan=${encodeURIComponent(plan)}` : '/login' }
}
