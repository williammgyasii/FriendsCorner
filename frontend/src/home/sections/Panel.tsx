import type { ReactNode } from 'react'

export function Panel({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="rounded-3xl bg-card p-4 shadow-sm ring-1 ring-border sm:p-6">
      <h2 className="text-base font-bold sm:text-lg">{title}</h2>
      <div className="mt-4">{children}</div>
    </section>
  )
}
