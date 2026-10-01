import { billingLook } from './billingLook.ts'

export type HomeView = {
  kind: 'home'
  gameName: string
  plan: 'Corner' | 'Table' | 'House' | null
  action: 'Start a session'
  failure: 'Could not open it. Try again'
}

const names = { corner: 'Corner', table: 'Table', house: 'House' } as const

export function homeLook(gameName: string, plan: string | null): HomeView {
  const known = plan === 'corner' || plan === 'table' || plan === 'house' ? names[plan] : null
  return {
    kind: 'home',
    gameName,
    plan: known,
    action: 'Start a session',
    failure: 'Could not open it. Try again',
  }
}

// Checkout stays in front of the home. A stored plan, or no plan query, is the home.
export function signedInScreen(gameName: string, plan: string | null, requested: string | null) {
  const billing = billingLook(true, plan, requested)
  if (billing.kind === 'checkout') {
    return billing
  }
  return homeLook(gameName, plan)
}
