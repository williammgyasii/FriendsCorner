import { Users } from 'lucide-react'
import { Panel } from './Panel.tsx'

export function FriendsSection() {
  return (
    <Panel title="Friends">
      <div className="flex flex-col items-center gap-4 py-8 text-center">
        <div className="grid size-20 place-items-center rounded-3xl bg-accent text-primary shadow-[0_4px_0_color-mix(in_srgb,var(--primary)_40%,#1c1915)]">
          <Users className="size-10" aria-hidden />
        </div>
        <p className="max-w-sm text-lg font-semibold">No friends saved yet</p>
        <p className="max-w-md text-sm text-muted-foreground">
          Hit Play on Home, share the room link, and the people who join show up here once we store them.
        </p>
      </div>
    </Panel>
  )
}
