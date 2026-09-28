import confetti from 'canvas-confetti'
import { Crown, MicOff, Minus, Plus, VideoOff } from 'lucide-react'
import { AnimatePresence, motion } from 'motion/react'
import { useEffect, useRef, useSyncExternalStore } from 'react'
import { useDispatch } from 'react-redux'
import type { Faces } from '../faces.ts'
import type { LobbyView, RoomSeat, Slot } from '../lobbyLook.ts'
import { sendToRoom, type AppDispatch } from '../store/index.ts'
import { FaceVideo } from './FaceVideo.tsx'
import { seatColor } from './seatStyle.ts'

const maxSeats = 4
const calm = () => typeof matchMedia !== 'function' || matchMedia('(prefers-reduced-motion: reduce)').matches

type Props = { view: LobbyView; capacity: number; you: RoomSeat; faces: Faces }

export function PlayerSlots({ view, capacity, you, faces }: Props) {
  const dispatch = useDispatch<AppDispatch>()
  const streams = useSyncExternalStore(faces.subscribe, faces.get)
  const joined = view.slots.filter((slot) => slot.kind === 'member').length
  const seen = useRef(joined)

  useEffect(() => {
    if (joined > seen.current && !calm()) {
      void confetti({ particleCount: 70, spread: 80, startVelocity: 36, origin: { y: 0.35 }, colors: ['#2f6bff', '#e23d6b', '#ffd23f'] })
    }
    seen.current = joined
  }, [joined])

  // The face call is between seats A and B; everyone else shows a badge.
  const streamFor = (seat: RoomSeat) => {
    if (seat === you) {
      return streams.local
    }
    const pair = (you === 'A' && seat === 'B') || (you === 'B' && seat === 'A')
    return pair && streams.remoteTile === 'live' ? streams.remote : null
  }

  const resize = (size: number) => dispatch(sendToRoom({ type: 'capacity', size }))

  return (
    <section aria-label="Players" className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">
          Players <span className="text-muted-foreground">{joined}/{capacity}</span>
        </h2>
        {view.canResize && (
          <div className="flex items-center gap-1">
            <motion.button
              whileTap={{ scale: 0.9 }}
              type="button"
              aria-label="Remove a seat"
              disabled={capacity <= Math.max(2, joined)}
              onClick={() => resize(capacity - 1)}
              className="grid size-9 place-items-center rounded-full bg-card shadow-sm ring-1 ring-border disabled:opacity-40"
            >
              <Minus className="size-4" aria-hidden />
            </motion.button>
            <motion.button
              whileTap={{ scale: 0.9 }}
              type="button"
              aria-label="Add a seat"
              disabled={capacity >= maxSeats}
              onClick={() => resize(capacity + 1)}
              className="grid size-9 place-items-center rounded-full bg-card shadow-sm ring-1 ring-border disabled:opacity-40"
            >
              <Plus className="size-4" aria-hidden />
            </motion.button>
          </div>
        )}
      </div>
      <ul className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <AnimatePresence mode="popLayout">
          {view.slots.map((slot) => (
            <motion.li
              key={slot.kind === 'member' ? slot.seat : `open-${slot.index}`}
              layout
              initial={{ opacity: 0, scale: 0.6, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.6 }}
              transition={{ type: 'spring', stiffness: 380, damping: 24 }}
            >
              {slot.kind === 'member' ? (
                <MemberCard slot={slot} stream={streamFor(slot.seat)} />
              ) : (
                <OpenCard />
              )}
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>
    </section>
  )
}

function MemberCard({ slot, stream }: { slot: Extract<Slot, { kind: 'member' }>; stream: MediaStream | null }) {
  const color = seatColor[slot.seat]
  return (
    <div
      className="relative overflow-hidden rounded-3xl bg-card p-2 shadow-[0_6px_0_var(--seat),0_16px_32px_color-mix(in_srgb,var(--seat)_22%,transparent)] ring-2 ring-[var(--seat)]"
      style={{ ['--seat' as string]: color }}
    >
      {slot.isHost && (
        <motion.div
          initial={{ rotate: -30, y: -12, opacity: 0 }}
          animate={{ rotate: -12, y: 0, opacity: 1 }}
          transition={{ type: 'spring', stiffness: 300, damping: 12 }}
          className="absolute -top-1 -left-1 z-10 grid size-10 place-items-center rounded-full bg-[#ffd23f] shadow-md"
        >
          <Crown className="size-5 text-[#8a5a00]" aria-hidden />
        </motion.div>
      )}
      <div className="relative aspect-[4/3] overflow-hidden rounded-2xl bg-muted">
        {stream && slot.camera ? (
          <FaceVideo stream={stream} mirrored={slot.isYou} />
        ) : (
          <div className="grid size-full place-items-center text-4xl font-bold text-white" style={{ background: color }}>
            {slot.isYou ? 'You' : slot.label.replace('Player ', 'P')}
          </div>
        )}
        <div className="absolute right-2 bottom-2 flex gap-1">
          {!slot.camera && (
            <span className="grid size-7 place-items-center rounded-full bg-black/60 text-white" title="Camera off">
              <VideoOff className="size-4" aria-label="Camera off" />
            </span>
          )}
          {!slot.mic && (
            <span className="grid size-7 place-items-center rounded-full bg-black/60 text-white" title="Mic off">
              <MicOff className="size-4" aria-label="Mic off" />
            </span>
          )}
        </div>
      </div>
      <div className="flex items-center justify-between gap-2 px-2 pt-2 pb-1">
        <div className="min-w-0">
          <p className="truncate font-semibold">{slot.label}</p>
          <p className="text-xs text-muted-foreground">
            {slot.isHost ? 'Game Master' : slot.watching ? 'Watching' : 'Player'}
          </p>
        </div>
        {!slot.isHost && (
          <AnimatePresence mode="wait">
            <motion.span
              key={slot.ready ? 'ready' : 'waiting'}
              initial={{ scale: 0.4, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.4, opacity: 0 }}
              className={`rounded-full px-2.5 py-1 text-xs font-bold ${
                slot.ready ? 'bg-[#17a864] text-white' : 'bg-muted text-muted-foreground'
              }`}
            >
              {slot.ready ? 'Ready!' : 'Not ready'}
            </motion.span>
          </AnimatePresence>
        )}
      </div>
    </div>
  )
}

function OpenCard() {
  return (
    <div className="grid aspect-[4/3.9] place-items-center rounded-3xl border-2 border-dashed border-border bg-card/50 p-4 text-center">
      <div>
        <motion.div
          animate={{ scale: [1, 1.15, 1], opacity: [0.5, 1, 0.5] }}
          transition={{ duration: 1.6, repeat: Infinity }}
          className="mx-auto mb-2 size-12 rounded-full bg-muted"
        />
        <p className="text-sm font-medium text-muted-foreground">Waiting for a player…</p>
      </div>
    </div>
  )
}
