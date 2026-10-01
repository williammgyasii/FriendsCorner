export function Fact({ label, value }: { label: string; value: string }) {
  return (
    <section className="rounded-3xl bg-card p-4 shadow-sm ring-1 ring-border sm:p-5">
      <p className="text-xs font-bold tracking-[0.16em] text-muted-foreground uppercase">{label}</p>
      <p className="mt-2 text-lg font-bold break-all sm:text-xl">{value}</p>
    </section>
  )
}
