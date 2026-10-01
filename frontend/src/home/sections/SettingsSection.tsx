import { SlidersHorizontal } from 'lucide-react'
import { Panel } from './Panel.tsx'

export function SettingsSection() {
  return (
    <Panel title="Settings">
      <div className="flex flex-col items-center gap-4 py-8 text-center">
        <div className="grid size-20 place-items-center rounded-3xl bg-accent text-primary shadow-[0_4px_0_color-mix(in_srgb,var(--primary)_40%,#1c1915)]">
          <SlidersHorizontal className="size-10" aria-hidden />
        </div>
        <p className="max-w-sm text-lg font-semibold">Settings are on the way</p>
        <p className="max-w-md text-sm text-muted-foreground">
          Camera, mic, and account prefs from the lobby will land here. Nothing is stored yet.
        </p>
      </div>
    </Panel>
  )
}
