import { AnimatePresence, MotionConfig, motion } from 'motion/react'
import { useEffect, useMemo, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { describeMystery, suspectPortrait, writingLines, type LeadLook, type MysteryLook, type SuspectLook } from '../mysteryLook.ts'
import { sendToRoom, type AppDispatch } from '../store/index.ts'
import { selectMystery, selectYou } from '../store/selectors.ts'

const particles = Array.from({ length: 18 }, (_, index) => index)

// Draws the look and sends messages; every rule lives on the server and every
// wording in mysteryLook. The only thing kept here is the local countdown
// between server messages.
export function MysteryScreen() {
  const mystery = useSelector(selectMystery)
  const you = useSelector(selectYou)
  const endsInMs = useCountdown(mystery?.endsInMs ?? null)
  const locksInMs = useCountdown(mystery?.locksInMs ?? null)
  const look = useMemo(
    () => (mystery ? describeMystery({ ...mystery, endsInMs, locksInMs }, you) : null),
    [mystery, you, endsInMs, locksInMs],
  )
  const dispatch = useDispatch<AppDispatch>()

  if (!look) {
    return null
  }

  const open = (lead: string) => dispatch(sendToRoom({ type: 'mystery-open', lead }))
  const accuse = (suspect: string) => dispatch(sendToRoom({ type: 'mystery-accuse', suspect }))
  const rematch = () => dispatch(sendToRoom({ type: 'mystery-rematch' }))
  const withdraw = () => dispatch(sendToRoom({ type: 'mystery-withdraw' }))

  return (
    <MotionConfig reducedMotion="user">
      <div className="mystery-screen" data-mood={look.mood} data-out={look.out || undefined}>
        <div className="mystery-ambience" aria-hidden="true">
          {particles.map((index) => (
            <span key={index} style={{ ['--i' as string]: index }} />
          ))}
        </div>

        <header className="mystery-top">
          <div className="mystery-face" data-face="you" />
          <div className="mystery-top-middle">
            <p role="status" className="mystery-status">
              {look.writing && <span className="mystery-spinner" aria-hidden="true" />}
              {look.status}
            </p>
            {look.clock && (
              <p role="timer" aria-label="Time left" className={`mystery-clock${look.clock.warn ? ' warn' : ''}`}>
                {look.clock.text}
              </p>
            )}
          </div>
          <div className="mystery-face" data-face="partner" />
        </header>

        {look.writing && <Writing />}

        {look.actions.tryAgain && (
          <button type="button" className="mystery-button primary mystery-again" onClick={rematch}>
            Try again
          </button>
        )}

        {look.title && <Case look={look} onOpen={open} onAccuse={accuse} onRematch={rematch} />}

        <AnimatePresence>
          {look.finalAnswer && <FinalAnswer name={look.finalAnswer.name} seconds={look.finalAnswer.seconds} onHold={withdraw} />}
        </AnimatePresence>
      </div>
    </MotionConfig>
  )
}

// Counts a server duration down locally, restarting whenever a new one arrives.
function useCountdown(ms: number | null) {
  const [from, setFrom] = useState({ ms, at: Date.now() })
  const [now, setNow] = useState(() => Date.now())
  if (from.ms !== ms) {
    setFrom({ ms, at: Date.now() })
  }

  useEffect(() => {
    if (ms === null) {
      return
    }
    const timer = setInterval(() => setNow(Date.now()), 250)
    return () => clearInterval(timer)
  }, [ms])

  return ms === null ? null : Math.max(0, ms - Math.max(0, now - from.at))
}

function Writing() {
  const [line, setLine] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => setLine((at) => (at + 1) % writingLines.length), 3500)
    return () => clearInterval(timer)
  }, [])

  return (
    <section className="mystery-writing" aria-live="polite">
      <motion.div
        className="mystery-envelope"
        animate={{ rotateY: [0, 12, -12, 0], y: [0, -6, 0] }}
        transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
        aria-hidden="true"
      >
        <span className="mystery-seal">✦</span>
      </motion.div>
      <AnimatePresence mode="wait">
        <motion.p key={line} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }}>
          {writingLines[line]}
        </motion.p>
      </AnimatePresence>
    </section>
  )
}

type CaseProps = {
  look: MysteryLook
  onOpen: (lead: string) => void
  onAccuse: (suspect: string) => void
  onRematch: () => void
}

function Case({ look, onOpen, onAccuse, onRematch }: CaseProps) {
  return (
    <main className="mystery-case">
      <motion.section
        className="mystery-folder"
        initial={{ rotateX: -75, opacity: 0, y: -20 }}
        animate={{ rotateX: 0, opacity: 1, y: 0 }}
        transition={{ type: 'spring', stiffness: 90, damping: 16 }}
      >
        <span className="mystery-folder-tab">Case file</span>
        <ul className="mystery-tags">
          {look.tags.map((tag) => (
            <li key={tag}>{tag}</li>
          ))}
        </ul>
        <h1>{look.title}</h1>
        <p className="mystery-setting">{look.setting}</p>
        {look.victim && (
          <p className="mystery-victim">
            <span className="mystery-victim-label">Victim</span>
            <strong>{look.victim.name}</strong> · {look.victim.found}
          </p>
        )}
      </motion.section>

      {look.reveal && <Reveal reveal={look.reveal} onRematch={onRematch} />}

      {look.hint && (
        <motion.p key={look.hint} className="mystery-hint" initial={{ opacity: 0, scale: 0.95 }} animate={{ opacity: 1, scale: 1 }}>
          {look.hint}
        </motion.p>
      )}

      <h2 className="mystery-heading">Suspects</h2>
      <div className="mystery-suspects">
        {look.suspects.map((suspect, index) => (
          <Suspect key={suspect.id} suspect={suspect} index={index} onOpen={onOpen} onAccuse={onAccuse} />
        ))}
      </div>

      <h2 className="mystery-heading">Places</h2>
      <div className="mystery-places">
        {look.places.map((place, index) => (
          <motion.div
            key={place.id}
            className="mystery-place"
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 + index * 0.08 }}
          >
            <h3>{place.name}</h3>
            <Leads leads={place.clues} onOpen={onOpen} />
          </motion.div>
        ))}
      </div>
    </main>
  )
}

function Reveal({ reveal, onRematch }: { reveal: NonNullable<MysteryLook['reveal']>; onRematch: () => void }) {
  return (
    <motion.section
      aria-label={reveal.headline}
      className={`mystery-reveal ${reveal.right ? 'right' : 'wrong'}`}
      initial={{ opacity: 0, rotateX: 30, y: 30 }}
      animate={{ opacity: 1, rotateX: 0, y: 0 }}
      transition={{ type: 'spring', stiffness: 120, damping: 18 }}
    >
      <motion.span
        className="mystery-stamp"
        aria-hidden="true"
        initial={{ scale: 3, rotate: -30, opacity: 0 }}
        animate={{ scale: 1, rotate: -10, opacity: 1 }}
        transition={{ delay: 0.35, type: 'spring', stiffness: 260, damping: 14 }}
      >
        {reveal.right ? 'Case closed' : 'Unsolved'}
      </motion.span>
      <h2>{reveal.headline}</h2>
      <p className="mystery-killer">
        It was <strong>{reveal.killer}</strong>.
      </p>
      <p>
        <b>How:</b> {reveal.how}
      </p>
      <p>
        <b>Why:</b> {reveal.why}
      </p>
      <p className="mystery-story">{reveal.story}</p>
      <p className="mystery-score">Score: {reveal.score}</p>
      <button type="button" className="mystery-button primary" onClick={onRematch}>
        New case
      </button>
    </motion.section>
  )
}

type SuspectProps = { suspect: SuspectLook; index: number; onOpen: (lead: string) => void; onAccuse: (suspect: string) => void }

function Suspect({ suspect, index, onOpen, onAccuse }: SuspectProps) {
  const marked = suspect.yours || suspect.partners

  return (
    <motion.article
      aria-label={suspect.name}
      className={`mystery-suspect${marked ? ' picked' : ''}`}
      initial={{ opacity: 0, rotateY: -40, y: 20 }}
      animate={{ opacity: 1, rotateY: 0, y: 0 }}
      transition={{ delay: 0.15 + index * 0.08, type: 'spring', stiffness: 120, damping: 16 }}
      whileHover={{ rotateX: 3, rotateY: index % 2 ? -3 : 3, y: -4 }}
    >
      <header>
        <span className="mystery-portrait" aria-hidden="true" style={{ width: 56, height: 56, borderRadius: '50%' }}>
          <img alt="" src={suspectPortrait(suspect.name)} />
        </span>
        <div>
          <h3>{suspect.name}</h3>
          <p className="mystery-role">{suspect.role}</p>
        </div>
        <p className="mystery-marks">
          {suspect.yours && <span className="mystery-mark you">You</span>}
          {suspect.partners && <span className="mystery-mark partner">Partner</span>}
        </p>
      </header>
      <p>
        <b>Motive:</b> {suspect.motive}
      </p>
      <p>
        <b>Alibi:</b> {suspect.alibi}
      </p>
      <Leads leads={suspect.statements} onOpen={onOpen} />
      {suspect.canAccuse && (
        <motion.button
          type="button"
          className={`mystery-button${suspect.yours ? ' primary' : ''} mystery-accuse`}
          aria-label={`Accuse ${suspect.name}`}
          aria-pressed={suspect.yours}
          whileTap={{ scale: 0.94 }}
          onClick={() => onAccuse(suspect.id)}
        >
          Accuse
        </motion.button>
      )}
    </motion.article>
  )
}

const finder = { you: 'You found this', partner: 'Partner found this' }

function Leads({ leads, onOpen }: { leads: LeadLook[]; onOpen: (lead: string) => void }) {
  return (
    <ul className="mystery-leads">
      {leads.map((lead) => (
        <li key={lead.id} className="mystery-lead-slot">
          {lead.text !== null ? (
            <motion.div
              className="mystery-lead open"
              initial={{ rotateY: 90, opacity: 0.4 }}
              animate={{ rotateY: 0, opacity: 1 }}
              transition={{ type: 'spring', stiffness: 160, damping: 18 }}
            >
              <p>{lead.text}</p>
              {lead.by && <span className={`mystery-finder ${lead.by}`}>{finder[lead.by]}</span>}
            </motion.div>
          ) : (
            <motion.button
              type="button"
              className="mystery-lead closed"
              aria-label={lead.label}
              disabled={!lead.canOpen}
              whileHover={lead.canOpen ? { rotateY: -12, y: -2 } : undefined}
              whileTap={lead.canOpen ? { scale: 0.97 } : undefined}
              onClick={() => onOpen(lead.id)}
            >
              <span className="mystery-lead-mark" aria-hidden="true">
                ?
              </span>
              {lead.canOpen ? 'Turn over' : 'Sealed'}
            </motion.button>
          )}
        </li>
      ))}
    </ul>
  )
}

function FinalAnswer({ name, seconds, onHold }: { name: string; seconds: number; onHold: () => void }) {
  return (
    <motion.div
      role="dialog"
      aria-modal="true"
      aria-label={`Final answer: ${name}`}
      className="mystery-final"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
    >
      <motion.div
        className="mystery-final-card"
        initial={{ rotateX: 60, scale: 0.8 }}
        animate={{ rotateX: 0, scale: 1 }}
        exit={{ rotateX: -40, scale: 0.9 }}
        transition={{ type: 'spring', stiffness: 160, damping: 16 }}
      >
        <p className="mystery-final-kicker">Your final answer</p>
        <h2>{name}</h2>
        <AnimatePresence mode="popLayout">
          <motion.p
            key={seconds}
            className="mystery-final-count"
            initial={{ scale: 1.8, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            exit={{ scale: 0.6, opacity: 0 }}
          >
            {seconds}
          </motion.p>
        </AnimatePresence>
        <button type="button" className="mystery-button" onClick={onHold}>
          Hold on
        </button>
      </motion.div>
    </motion.div>
  )
}
