import type { FaceTile } from './face.ts'

export type FacesSnapshot = {
  local: MediaStream | null
  remote: MediaStream | null
  remoteTile: FaceTile
}

// The live camera streams, shared between the face call (which owns them)
// and the React screens (which only show them).
export type Faces = {
  get(): FacesSnapshot
  set(change: Partial<FacesSnapshot>): void
  subscribe(listener: () => void): () => void
}

export function createFaces(): Faces {
  let current: FacesSnapshot = { local: null, remote: null, remoteTile: 'hidden' }
  const listeners = new Set<() => void>()
  return {
    get: () => current,
    set(change) {
      current = { ...current, ...change }
      listeners.forEach((listener) => listener())
    },
    subscribe(listener) {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
  }
}
