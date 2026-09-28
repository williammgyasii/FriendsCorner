import { AnimatePresence, motion } from 'motion/react'

export function Countdown({ value }: { value: number | null }) {
  return (
    <AnimatePresence>
      {value !== null && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="fixed inset-0 z-50 grid place-items-center bg-[radial-gradient(circle,color-mix(in_srgb,var(--primary)_35%,transparent),rgb(28_25_21/0.75))] backdrop-blur-sm"
        >
          <div role="status" aria-label="Starting in" className="text-center text-white">
            <p className="text-xl font-semibold tracking-widest uppercase">Get ready</p>
            <AnimatePresence mode="popLayout">
              <motion.p
                key={value}
                initial={{ scale: 2.6, opacity: 0, rotate: -12 }}
                animate={{ scale: 1, opacity: 1, rotate: 0 }}
                exit={{ scale: 0.3, opacity: 0 }}
                transition={{ type: 'spring', stiffness: 260, damping: 16 }}
                className="text-[10rem] leading-none font-bold drop-shadow-[0_8px_0_var(--seat-b)]"
              >
                {value}
              </motion.p>
            </AnimatePresence>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
