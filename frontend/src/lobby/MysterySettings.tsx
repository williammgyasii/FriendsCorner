import { motion } from 'motion/react'
import { useDispatch } from 'react-redux'
import type { MysterySettingsView, SettingChoice } from '../lobbyLook.ts'
import { sendToRoom, type AppDispatch } from '../store/index.ts'

export function MysterySettings({ view }: { view: MysterySettingsView }) {
  const dispatch = useDispatch<AppDispatch>()
  const send = (change: Partial<MysterySettingsView['chosen']>) =>
    dispatch(sendToRoom({ type: 'mystery-settings', ...view.chosen, ...change }))

  return (
    <motion.section
      aria-label="Case settings"
      initial={{ opacity: 0, rotateX: -12, y: 16 }}
      animate={{ opacity: 1, rotateX: 0, y: 0 }}
      className="case-settings flex flex-col gap-4 rounded-md p-4 sm:p-5"
    >
      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
        <h2 className="shrink-0 font-serif text-lg font-semibold">The case file</h2>
        {!view.canChange && <span className="text-xs text-muted-foreground">The Game Master sets the case</span>}
      </div>
      <Choices name="Level" choices={view.levels} enabled={view.canChange} onPick={(level) => send({ level })} />
      <Choices name="Mode" choices={view.modes} enabled={view.canChange} onPick={(mode) => send({ mode })} />
    </motion.section>
  )
}

function Choices<T extends string>({
  name,
  choices,
  enabled,
  onPick,
}: {
  name: string
  choices: SettingChoice<T>[]
  enabled: boolean
  onPick: (id: T) => void
}) {
  return (
    <div role="radiogroup" aria-label={name} className="grid grid-cols-1 gap-2 sm:grid-cols-2">
      {choices.map((choice) => (
        <motion.button
          key={choice.id}
          type="button"
          role="radio"
          aria-checked={choice.on}
          disabled={!enabled}
          onClick={() => !choice.on && onPick(choice.id)}
          whileTap={enabled ? { scale: 0.97 } : undefined}
          className="case-choice flex flex-col items-start gap-0.5 rounded-sm px-3 py-2.5 text-left disabled:cursor-default"
        >
          <span className="font-semibold">{choice.label}</span>
          <span className="text-xs text-muted-foreground">{choice.blurb}</span>
        </motion.button>
      ))}
    </div>
  )
}
