export type HostPlanId = 'corner' | 'table' | 'house'

export type HostPlanOffer = {
  id: HostPlanId
  name: string
  price: number
  color: string
  edge: string
  tagline: string
  perks: string[]
}

export const allowanceGames = ['Tic-tac-toe', 'Chess', 'Letter Tiles', 'Murder Mystery'] as const

function allowanceLines(counts: number[] | 'unlimited'): string[] {
  if (counts === 'unlimited') {
    return allowanceGames.map((game) => `${game}: unlimited nights`)
  }
  return counts.map((count, index) => `${allowanceGames[index]}: ${count} nights/mo`)
}

const sharedPerk = 'Friends on your link play free'

export function hostPlanOffers(): HostPlanOffer[] {
  return [
    {
      id: 'corner',
      name: 'Corner',
      price: 15,
      color: '#5b8cff',
      edge: '#3d6fd4',
      tagline: 'A cozy booth for close friends.',
      perks: [sharedPerk, ...allowanceLines([30, 8, 8, 4])],
    },
    {
      id: 'table',
      name: 'Table',
      price: 20,
      color: '#f0587f',
      edge: '#c73d62',
      tagline: 'A bigger table for more game nights.',
      perks: [sharedPerk, ...allowanceLines([90, 24, 24, 12])],
    },
    {
      id: 'house',
      name: 'House',
      price: 50,
      color: '#d9a25f',
      edge: '#b8843f',
      tagline: 'The whole arcade, no limits.',
      perks: [sharedPerk, ...allowanceLines('unlimited')],
    },
  ]
}

export function hostPlanOffer(id: HostPlanId): HostPlanOffer {
  const offer = hostPlanOffers().find((plan) => plan.id === id)
  if (!offer) throw new Error(`Unknown plan: ${id}`)
  return offer
}

export function hostPlanOfferByName(name: string | null): HostPlanOffer | null {
  if (!name) return null
  const id = name.toLowerCase() as HostPlanId
  return hostPlanOffers().find((plan) => plan.id === id) ?? null
}
