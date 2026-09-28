import { Check, Crown, Hand, Link2, Play } from 'lucide-react'
import { AnimatePresence, motion } from 'motion/react'
import { useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import type { Faces } from '../faces.ts'
import type { LobbyView } from '../lobbyLook.ts'
import { sendToRoom, type AppDispatch, type RootState } from '../store/index.ts'
import { selectConnection, selectLobby, selectLobbyView, selectYou } from '../store/selectors.ts'
import { Countdown } from './Countdown.tsx'
import { GameGrid } from './GameGrid.tsx'
import { MediaSetup } from './MediaSetup.tsx'
import { PlayerSlots } from './PlayerSlots.tsx'

const connectionWords = {
  connecting: 'Connecting to the lobby…',
  open: 'Joining…',
  full: 'This lobby is full.',
  gone: 'This lobby is gone.',
}

export function LobbyScreen({ faces }: { faces: Faces }) {
  const view = useSelector(selectLobbyView)
  const capacity = useSelector((state: RootState) => selectLobby(state)?.capacity ?? 2)
  const you = useSelector(selectYou)
  const connection = useSelector(selectConnection)

  if (!view || connection === 'full' || connection === 'gone') {
    return (
      <div className="tw grid min-h-dvh place-items-center bg-background p-6">
        <p className="text-2xl font-semibold">{connectionWords[connection]}</p>
      </div>
    )
  }

  return (
    <div className="tw min-h-dvh bg-background bg-[radial-gradient(circle,color-mix(in_srgb,var(--primary)_14%,transparent)_1.6px,transparent_2px)] bg-[length:26px_26px]">
      <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 pt-6 pb-36 sm:px-8">
        <Header view={view} />
        <PlayerSlots view={view} capacity={capacity} you={you} faces={faces} />
        <GameGrid view={view} />
        <MediaSetup faces={faces} />
      </div>
      <ActionBar view={view} />
      <Countdown value={view.countdown} />
    </div>
  )
}

function Header({ view }: { view: LobbyView }) {
  const [copied, setCopied] = useState(false)
  const copy = () => {
    void navigator.clipboard?.writeText(location.href)
    setCopied(true)
    setTimeout(() => setCopied(false), 1800)
  }

  return (
    <header className="flex flex-wrap items-center justify-between gap-4">
      <div className="flex flex-col gap-2">
        <p className="text-xs font-bold tracking-[0.2em] text-[#8a5a2b] uppercase">Friends Corner · Lobby</p>
        <AnimatePresence mode="wait">
          <motion.h1
            key={view.headline}
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -12 }}
            className="flex items-center gap-2 text-3xl font-bold sm:text-4xl"
          >
            {view.role === 'host' && <Crown className="size-8 shrink-0 text-[#e0a800]" aria-hidden />}
            {view.headline}
          </motion.h1>
        </AnimatePresence>
      </div>
      <motion.button
        type="button"
        whileTap={{ scale: 0.94 }}
        onClick={copy}
        className="flex items-center gap-2 rounded-full bg-card px-4 py-2.5 font-semibold shadow-[0_4px_0_#e2dace] ring-1 ring-border"
      >
        {copied ? <Check className="size-4 text-[#17a864]" aria-hidden /> : <Link2 className="size-4" aria-hidden />}
        {copied ? 'Invite copied!' : 'Copy invite'}
      </motion.button>
    </header>
  )
}

function ActionBar({ view }: { view: LobbyView }) {
  const dispatch = useDispatch<AppDispatch>()

  return (
    <div className="pointer-events-none fixed inset-x-0 bottom-0 z-40 bg-gradient-to-t from-background via-background/95 to-transparent px-4 pt-8 pb-[max(1.25rem,env(safe-area-inset-bottom))]">
      <div className="pointer-events-auto mx-auto flex max-w-md justify-center">
        {view.start && (
          <motion.button
            type="button"
            disabled={!view.start.enabled}
            onClick={() => dispatch(sendToRoom({ type: 'start' }))}
            animate={view.start.enabled ? { scale: [1, 1.04, 1] } : { scale: 1 }}
            transition={view.start.enabled ? { duration: 1.2, repeat: Infinity } : undefined}
            whileTap={view.start.enabled ? { scale: 0.95, y: 4 } : undefined}
            className="flex w-full items-center justify-center gap-2 rounded-full bg-primary px-6 py-4 text-xl font-bold text-primary-foreground shadow-[0_6px_0_color-mix(in_srgb,var(--primary)_60%,#1c1915)] disabled:bg-muted disabled:text-muted-foreground disabled:shadow-[0_6px_0_#d8d0c3]"
          >
            {view.start.enabled ? <Play className="size-5 fill-current" aria-hidden /> : null}
            {view.start.label}
          </motion.button>
        )}
        {view.ready && (
          <motion.button
            type="button"
            disabled={!view.ready.enabled}
            onClick={() => dispatch(sendToRoom({ type: 'ready', ready: !view.ready?.on }))}
            whileTap={view.ready.enabled ? { scale: 0.95, y: 4 } : undefined}
            animate={view.ready.on ? { rotate: [0, -2, 2, 0] } : {}}
            className={`flex w-full items-center justify-center gap-2 rounded-full px-6 py-4 text-xl font-bold shadow-[0_6px_0_var(--edge)] disabled:opacity-50 ${
              view.ready.on ? 'bg-[#17a864] text-white' : 'bg-[var(--seat-b)] text-white'
            }`}
            style={{ ['--edge' as string]: view.ready.on ? '#0f7a47' : 'color-mix(in srgb, var(--seat-b) 60%, #1c1915)' }}
          >
            {view.ready.on ? <Check className="size-5" aria-hidden /> : <Hand className="size-5" aria-hidden />}
            {view.ready.on ? 'Ready! Tap to undo' : view.ready.enabled ? "I'm ready" : 'Ready up once a game is picked'}
          </motion.button>
        )}
      </div>
    </div>
  )
}
