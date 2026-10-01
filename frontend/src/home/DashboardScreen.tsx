import {
  CreditCard,
  Home,
  LogOut,
  Play,
  Settings,
  UserRound,
  Users,
} from 'lucide-react'
import { motion } from 'motion/react'
import { useState } from 'react'
import { useSelector } from 'react-redux'
import { Badge } from '../components/ui/badge.tsx'
import { Button } from '../components/ui/button.tsx'
import { Avatar, AvatarImage } from '../components/ui/avatar.tsx'
import type { HomeView } from '../homeLook.ts'
import { authApi } from '../store/authApi.ts'
import type { RootState } from '../store/index.ts'
import { roomApi } from '../store/roomApi.ts'
import { dashboardNav, mobileTabBarHeight, sectionTitle, type DashboardSection } from './dashboardLook.ts'
import { profilePortrait } from './portrait.ts'
import { BillingSection } from './sections/BillingSection.tsx'
import { FriendsSection } from './sections/FriendsSection.tsx'
import { HomeSection } from './sections/HomeSection.tsx'
import { ProfileSection } from './sections/ProfileSection.tsx'
import { SettingsSection } from './sections/SettingsSection.tsx'

const icons = {
  home: Home,
  friends: Users,
  settings: Settings,
  profile: UserRound,
  billing: CreditCard,
} as const

const playButtonClass =
  'flex w-full items-center justify-center gap-2 rounded-full bg-primary px-5 py-3 text-base font-bold text-primary-foreground shadow-[0_4px_0_color-mix(in_srgb,var(--primary)_60%,#1c1915)] disabled:opacity-70 md:w-auto'

type Props = {
  view: HomeView
  email: string
}

export function DashboardScreen({ view, email }: Props) {
  const account = useSelector((state: RootState) => state.auth.account)
  const [section, setSection] = useState<DashboardSection>('home')
  const [playBusy, setPlayBusy] = useState(false)
  const [playNote, setPlayNote] = useState<string | null>(null)
  const [createRoom] = roomApi.useCreateRoomMutation()
  const [signOut] = authApi.useSignOutMutation()

  const play = async () => {
    setPlayBusy(true)
    setPlayNote(null)
    const opened = await createRoom()
    if ('data' in opened) {
      location.search = `?room=${opened.data}`
      return
    }
    setPlayNote(view.failure)
    setPlayBusy(false)
  }

  const leave = async () => {
    const result = await signOut()
    if ('error' in result && result.error && 'status' in result.error && result.error.status === 'FETCH_ERROR') return
    location.assign('/login')
  }

  const playButton = (
    <motion.button
      type="button"
      disabled={playBusy}
      onClick={() => void play()}
      animate={playBusy ? { scale: 0.96 } : { scale: [1, 1.04, 1] }}
      transition={playBusy ? { duration: 0.15 } : { duration: 1.2, repeat: Infinity }}
      whileTap={{ scale: 0.94, y: 3 }}
      className={playButtonClass}
    >
      <Play className="size-4 fill-current" aria-hidden />
      {playBusy ? 'Opening…' : 'Play'}
    </motion.button>
  )

  return (
    <div className="tw min-h-dvh bg-background bg-[radial-gradient(circle,color-mix(in_srgb,var(--primary)_14%,transparent)_1.6px,transparent_2px)] bg-[length:26px_26px] md:flex">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-border bg-card/80 backdrop-blur md:flex lg:w-64">
        <div className="flex items-center gap-3 px-5 py-6">
          <Avatar className="size-11 ring-2 ring-primary">
            <AvatarImage src={profilePortrait(view.gameName)} alt="" />
          </Avatar>
          <div className="min-w-0">
            <p className="text-[0.65rem] font-bold tracking-[0.2em] text-[#d9a25f] uppercase">Friends Corner</p>
            <p className="truncate text-base font-bold">{view.gameName}</p>
          </div>
        </div>

        <nav className="flex flex-1 flex-col gap-1 px-3">
          {dashboardNav.map((item) => (
            <NavButton key={item.id} item={item} active={section === item.id} onPick={setSection} layout="sidebar" />
          ))}
        </nav>

        <div className="p-4">
          <Button type="button" variant="outline" className="w-full" onClick={() => void leave()}>
            <LogOut className="size-4" aria-hidden />
            Log out
          </Button>
        </div>
      </aside>

      <div
        className="flex min-w-0 flex-1 flex-col max-md:pb-[calc(4.5rem+env(safe-area-inset-bottom))] md:pb-0"
      >
        <header className="sticky top-0 z-10 border-b border-border/60 bg-background/90 px-4 py-3 backdrop-blur sm:px-6 md:px-8 md:py-4">
          <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-3">
            <div className="flex min-w-0 flex-1 items-center gap-3">
              <Avatar className="size-10 ring-2 ring-primary md:hidden">
                <AvatarImage src={profilePortrait(view.gameName)} alt="" />
              </Avatar>
              <div className="min-w-0">
                <p className="text-[0.65rem] font-bold tracking-[0.16em] text-[#d9a25f] uppercase md:hidden">
                  Friends Corner
                </p>
                <p className="hidden text-xs font-bold tracking-[0.16em] text-muted-foreground uppercase md:block">
                  Dashboard
                </p>
                <h1 className="truncate text-xl font-bold sm:text-2xl">{sectionTitle(section)}</h1>
              </div>
            </div>
            <Badge variant="secondary" className="shrink-0">
              {view.plan ?? 'No plan'}
            </Badge>
            <div className="hidden md:block">{playButton}</div>
          </div>
          <div className="mx-auto mt-3 max-w-5xl md:hidden">{playButton}</div>
        </header>

        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-5 sm:px-6 sm:py-8 md:px-8">
          {section === 'home' ? <HomeSection view={view} email={email} note={playNote} /> : null}
          {section === 'friends' ? <FriendsSection /> : null}
          {section === 'settings' ? <SettingsSection /> : null}
          {section === 'profile' && account ? <ProfileSection account={account} onLogout={() => void leave()} /> : null}
          {section === 'billing' ? <BillingSection view={view} /> : null}
        </main>
      </div>

      <nav aria-label="Dashboard sections" className="fixed inset-x-0 bottom-0 z-20 md:hidden">
        <div className="border-t border-border bg-card/98 shadow-[0_-10px_30px_rgba(0,0,0,0.35)] backdrop-blur-xl">
          <div
            className="mx-auto grid max-w-lg grid-cols-5 px-1 pt-1"
            style={{
              minHeight: mobileTabBarHeight,
              paddingBottom: 'max(0.5rem, env(safe-area-inset-bottom))',
            }}
          >
            {dashboardNav.map((item) => (
              <NavButton key={item.id} item={item} active={section === item.id} onPick={setSection} layout="tab" />
            ))}
          </div>
        </div>
      </nav>
    </div>
  )
}

function NavButton({
  item,
  active,
  onPick,
  layout,
}: {
  item: (typeof dashboardNav)[number]
  active: boolean
  onPick: (section: DashboardSection) => void
  layout: 'sidebar' | 'tab'
}) {
  const Icon = icons[item.id]

  if (layout === 'tab') {
    return (
      <button
        type="button"
        aria-label={item.label}
        aria-current={active ? 'page' : undefined}
        onClick={() => onPick(item.id)}
        className={`flex min-h-14 min-w-0 flex-col items-center justify-center gap-1 rounded-2xl px-0.5 py-1 transition ${
          active ? 'bg-primary/12 text-primary' : 'text-muted-foreground'
        }`}
      >
        <Icon className={`size-5 shrink-0 ${active ? 'text-primary' : ''}`} aria-hidden />
        <span className="w-full truncate text-center text-[0.68rem] font-bold leading-none">{item.tab}</span>
      </button>
    )
  }

  return (
    <button
      type="button"
      aria-current={active ? 'page' : undefined}
      onClick={() => onPick(item.id)}
      className={`flex w-full items-center gap-2 rounded-2xl px-4 py-2.5 text-sm font-bold transition ${
        active
          ? 'bg-primary text-primary-foreground shadow-[0_3px_0_color-mix(in_srgb,var(--primary)_60%,#1c1915)]'
          : 'text-muted-foreground hover:bg-muted hover:text-foreground'
      }`}
    >
      <Icon className="size-4 shrink-0" aria-hidden />
      {item.label}
    </button>
  )
}
