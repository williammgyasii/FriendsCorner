export type FaceTile = 'hidden' | 'waiting' | 'live' | 'unavailable'

export function describeFace(remote: 'absent' | 'waiting' | 'live' | 'unavailable'): {
  tile: FaceTile
  movementAllowed: boolean
} {
  const movementAllowed = true
  if (remote === 'unavailable') {
    return { tile: 'unavailable', movementAllowed }
  }
  if (remote === 'live') {
    return { tile: 'live', movementAllowed }
  }
  if (remote === 'waiting') {
    return { tile: 'waiting', movementAllowed }
  }
  return { tile: 'hidden', movementAllowed }
}
