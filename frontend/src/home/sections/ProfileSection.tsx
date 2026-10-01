import { LogOut } from 'lucide-react'
import { Badge } from '../../components/ui/badge.tsx'
import { Button } from '../../components/ui/button.tsx'
import { Avatar, AvatarImage } from '../../components/ui/avatar.tsx'
import type { Account } from '../../store/authApi.ts'
import { profilePortrait } from '../portrait.ts'
import { Fact } from './Fact.tsx'
import { Panel } from './Panel.tsx'

type Props = {
  account: Account
  onLogout: () => void
}

export function ProfileSection({ account, onLogout }: Props) {
  const plan =
    account.plan === 'corner' || account.plan === 'table' || account.plan === 'house'
      ? account.plan.charAt(0).toUpperCase() + account.plan.slice(1)
      : null

  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-[minmax(0,16rem)_1fr]">
      <Panel title="Your seat">
        <div className="flex flex-col items-center gap-4 py-2 text-center">
          <Avatar className="size-24 ring-4 ring-primary">
            <AvatarImage src={profilePortrait(account.gameName)} alt="" />
          </Avatar>
          <div>
            <p className="text-2xl font-bold">{account.gameName}</p>
            <p className="text-sm text-muted-foreground">{account.name}</p>
          </div>
          <Badge variant="secondary">{plan ?? 'No plan'}</Badge>
        </div>
      </Panel>
      <div className="grid content-start gap-3 sm:grid-cols-2">
        <Fact label="Name" value={account.name} />
        <Fact label="Game name" value={account.gameName} />
        <Fact label="Email" value={account.email} />
        <Fact label="Plan" value={plan ?? 'None'} />
        <Button type="button" variant="outline" className="w-full sm:col-span-2 md:hidden" onClick={onLogout}>
          <LogOut className="size-4" aria-hidden />
          Log out
        </Button>
      </div>
    </div>
  )
}
