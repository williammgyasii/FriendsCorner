export type BillingView =
  | { kind: 'login'; fields: ['email', 'password']; actions: ['Create account', 'Sign in'] }
  | { kind: 'checkout'; plan: 'corner' | 'table' | 'house' }
  | { kind: 'lobby'; action: 'Open a lobby'; plan: 'Corner' | 'Table' | 'House' | null; manage: 'Manage plan' | null }

const names = { corner: 'Corner', table: 'Table', house: 'House' } as const

export function billingLook(signedIn: boolean, plan: string | null, requested: string | null): BillingView {
  if (!signedIn) {
    return {
      kind: 'login',
      fields: ['email', 'password'],
      actions: ['Create account', 'Sign in'],
    }
  }

  const asked = requested === 'corner' || requested === 'table' || requested === 'house' ? requested : null
  if (asked && !plan) {
    return { kind: 'checkout', plan: asked }
  }

  const known = plan === 'corner' || plan === 'table' || plan === 'house' ? plan : null
  return {
    kind: 'lobby',
    action: 'Open a lobby',
    plan: known ? names[known] : null,
    manage: known ? 'Manage plan' : null,
  }
}
