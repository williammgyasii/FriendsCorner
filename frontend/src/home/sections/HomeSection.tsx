import type { HomeView } from '../../homeLook.ts'
import { Fact } from './Fact.tsx'

type Props = {
  view: HomeView
  email: string
  note: string | null
}

export function HomeSection({ view, email, note }: Props) {
  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
      <Fact label="Plan" value={view.plan ?? 'None'} />
      <Fact label="Game name" value={view.gameName} />
      <Fact label="Email" value={email || '—'} />
      {note ? <p className="text-sm font-medium text-destructive sm:col-span-3">{note}</p> : null}
    </div>
  )
}
