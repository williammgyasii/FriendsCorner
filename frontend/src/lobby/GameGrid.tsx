import { Check } from 'lucide-react'
import { motion } from 'motion/react'
import { useDispatch } from 'react-redux'
import { gameCards, type LobbyView } from '../lobbyLook.ts'
import { sendToRoom, type AppDispatch } from '../store/index.ts'

const art: Record<string, { glyph: string; pip: string; ink: string }> = {
  room: { glyph: '🛋️', pip: '♥', ink: '#f2a65a' },
  tictactoe: { glyph: '✕ ○', pip: '♦', ink: '#7aa2ff' },
  chess: { glyph: '♞', pip: '♠', ink: '#d9cbb3' },
  tiles: { glyph: 'A B C', pip: '♣', ink: '#f2c86b' },
  mystery: { glyph: '🔍', pip: '✦', ink: '#c9a3ff' },
}

export function GameGrid({ view }: { view: LobbyView }) {
  const dispatch = useDispatch<AppDispatch>()

  return (
    <section aria-label="Games" className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">{view.canPick ? 'Pick a game' : 'Games'}</h2>
      <div className="game-deck grid grid-cols-3 gap-3 sm:grid-cols-5 sm:gap-4">
        {gameCards.map((game, index) => {
          const picked = view.pick?.id === game.id
          const look = art[game.id]
          return (
            <motion.button
              key={game.id}
              type="button"
              disabled={!view.canPick}
              onClick={() => dispatch(sendToRoom({ type: 'pick', game: game.id }))}
              initial={{ opacity: 0, y: 30, rotateY: -70 }}
              animate={{ opacity: 1, y: picked ? -8 : 0, rotateY: 0, scale: picked ? 1.04 : 1 }}
              transition={{ delay: index * 0.07, type: 'spring', stiffness: 260, damping: 22 }}
              whileHover={view.canPick ? { y: -10, rotateX: 8, rotateZ: index % 2 ? 1.5 : -1.5 } : undefined}
              whileTap={view.canPick ? { scale: 0.96 } : undefined}
              aria-pressed={picked}
              title={game.blurb}
              data-picked={picked || undefined}
              className={`game-card relative flex aspect-[5/7] flex-col disabled:cursor-default ${!view.canPick && !picked ? 'opacity-60' : ''}`}
              style={{ ['--ink' as string]: look.ink }}
            >
              <span data-pip className="game-pip top-1.5 left-2" aria-hidden>
                {look.pip}
              </span>
              <span data-pip className="game-pip right-2 bottom-1.5 rotate-180" aria-hidden>
                {look.pip}
              </span>
              <span className="game-frame grid flex-1 place-items-center" aria-hidden>
                <motion.span
                  className="text-2xl sm:text-3xl"
                  animate={picked ? { rotate: [0, -8, 8, 0], scale: [1, 1.2, 1] } : {}}
                  transition={{ duration: 0.6 }}
                >
                  {look.glyph}
                </motion.span>
              </span>
              <span className="flex flex-col items-center gap-0.5 px-1.5 pb-3">
                <span className="text-xs leading-tight font-bold sm:text-sm">{game.name}</span>
                <span className="text-[0.7rem] font-semibold text-muted-foreground">{game.players}</span>
              </span>
              {picked && (
                <motion.span
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  className="absolute -top-2 -right-2 grid size-6 place-items-center rounded-full bg-primary text-primary-foreground"
                >
                  <Check className="size-3.5" aria-label="Picked" />
                </motion.span>
              )}
            </motion.button>
          )
        })}
      </div>
    </section>
  )
}
