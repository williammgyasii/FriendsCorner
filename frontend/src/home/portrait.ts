import { createAvatar } from '@dicebear/core'
import { create, meta, schema } from '@dicebear/avataaars'

const avataaars = { create, meta, schema }

export function profilePortrait(name: string) {
  return createAvatar(avataaars, { seed: name || 'friend', size: 80 }).toDataUri()
}
