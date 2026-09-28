import { Check } from 'lucide-react'
import { motion } from 'motion/react'
import { useDispatch } from 'react-redux'
import { gameCards, type LobbyView } from '../lobbyLook.ts'
import { sendToRoom, type AppDispatch } from '../store/index.ts'

const art: Record<string, { glyph: string; from: string; to: string }> = {
  room: { glyph: '🛋️', from: '#ffe8c7', to: '#ffc98a' },
  tictactoe: { glyph: '✕ ○', from: '#dbe6ff', to: '#9db8ff' },
  chess: { glyph: '♞', from: '#e9e4dc', to: '#b9ad9b' },
}

export function GameGrid({ view }: { view: LobbyView }) {
  const dispatch = useDispatch<AppDispatch>()

  return (
    <section aria-label="Games" className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">{view.canPick ? 'Pick a game' : 'Games'}</h2>
      <div className="grid grid-cols-3 gap-3 sm:max-w-xl">
        {gameCards.map((game, index) => {
          const picked = view.pick?.id === game.id
          const look = art[game.id]
          return (
            <motion.button
              key={game.id}
              type="button"
              disabled={!view.canPick}
              onClick={() => dispatch(sendToRoom({ type: 'pick', game: game.id }))}
              initial={{ opacity: 0, y: 24 }}
              animate={{ opacity: 1, y: 0, scale: picked ? 1.03 : 1 }}
              transition={{ delay: index * 0.07, type: 'spring', stiffness: 300, damping: 22 }}
              whileHover={view.canPick ? { y: -6, rotate: index % 2 ? 1 : -1 } : undefined}
              whileTap={view.canPick ? { scale: 0.96 } : undefined}
              aria-pressed={picked}
              title={game.blurb}
              className={`relative flex flex-col overflow-hidden rounded-2xl bg-card text-center shadow-[0_4px_0_#e2dace] ring-2 transition-shadow disabled:cursor-default ${
                picked ? 'ring-primary shadow-[0_4px_0_var(--primary),0_12px_28px_color-mix(in_srgb,var(--primary)_25%,transparent)]' : 'ring-transparent'
              } ${!view.canPick && !picked ? 'opacity-70' : ''}`}
            >
              {picked && (
                <motion.span
                  layoutId="picked-glow"
                  className="pointer-events-none absolute inset-0 rounded-2xl ring-4 ring-primary/40"
                  transition={{ type: 'spring', stiffness: 400, damping: 30 }}
                />
              )}
              <div
                className="grid h-16 place-items-center text-3xl"
                style={{ background: `linear-gradient(135deg, ${look.from}, ${look.to})` }}
                aria-hidden
              >
                <motion.span animate={picked ? { rotate: [0, -8, 8, 0], scale: [1, 1.2, 1] } : {}} transition={{ duration: 0.6 }}>
                  {look.glyph}
                </motion.span>
              </div>
              {picked && (
                <motion.span
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  className="absolute top-1.5 right-1.5 grid size-6 place-items-center rounded-full bg-primary text-primary-foreground"
                >
                  <Check className="size-3.5" aria-label="Picked" />
                </motion.span>
              )}
              <div className="flex flex-1 flex-col items-center justify-center gap-0.5 px-2 py-2.5">
                <span className="text-sm leading-tight font-bold sm:text-base">{game.name}</span>
                <span className="text-xs font-semibold text-muted-foreground">{game.players}</span>
              </div>
            </motion.button>
          )
        })}
      </div>
    </section>
  )
}
