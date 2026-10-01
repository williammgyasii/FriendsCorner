import { Check, CreditCard } from 'lucide-react'
import { useState } from 'react'
import { hostPlanOfferByName, hostPlanOffers, type HostPlanId, type HostPlanOffer } from '../../billingPlansLook.ts'
import { Button } from '../../components/ui/button.tsx'
import { billingApi } from '../../store/billingApi.ts'
import type { HomeView } from '../../homeLook.ts'
import { Panel } from './Panel.tsx'

type Props = {
  view: HomeView
}

export function BillingSection({ view }: Props) {
  const [busy, setBusy] = useState<string | null>(null)
  const [note, setNote] = useState<string | null>(null)
  const [startBilling] = billingApi.useStartBillingMutation()
  const current = hostPlanOfferByName(view.plan)

  const checkout = async (plan: HostPlanId) => {
    setBusy(plan)
    setNote(null)
    const result = await startBilling({ action: 'checkout', plan, returnUrl: location.origin })
    if ('data' in result && result.data) {
      location.assign(result.data.url)
      return
    }
    setNote('Could not open checkout. Try again.')
    setBusy(null)
  }

  const portal = async () => {
    setBusy('portal')
    setNote(null)
    const result = await startBilling({ action: 'portal', plan: null })
    if ('data' in result && result.data) {
      location.assign(result.data.url)
      return
    }
    setNote('Could not open billing. Try again.')
    setBusy(null)
  }

  return (
    <div className="grid gap-4">
      <Panel title="Your plan">
        <div className="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-start">
          <div className="flex min-w-0 items-start gap-4">
            <div
              className="grid size-14 shrink-0 place-items-center rounded-2xl text-white shadow-[0_3px_0_var(--plan-edge)]"
              style={{
                background: current?.color ?? 'var(--primary)',
                ['--plan-edge' as string]: current?.edge ?? 'color-mix(in srgb, var(--primary) 60%, #1c1915)',
              }}
            >
              <CreditCard className="size-7" aria-hidden />
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-[Bungee] text-3xl tracking-tight" style={{ color: current?.color ?? undefined }}>
                {view.plan ?? 'No plan'}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                {view.plan ? 'Friends play on your host plan.' : 'Pick a plan so you can host game night.'}
              </p>
            </div>
          </div>
          {view.plan ? (
            <Button type="button" className="w-full sm:ml-auto sm:w-auto" disabled={busy !== null} onClick={() => void portal()}>
              {busy === 'portal' ? 'Opening…' : 'Manage plan'}
            </Button>
          ) : null}
        </div>
        {current ? <PlanPerks offer={current} className="mt-5" /> : null}
      </Panel>

      {!view.plan ? (
        <div className="grid gap-4 lg:grid-cols-3">
          {hostPlanOffers().map((offer) => (
            <PlanCard key={offer.id} offer={offer} busy={busy === offer.id} onChoose={() => void checkout(offer.id)} />
          ))}
        </div>
      ) : null}

      {note ? <p className="text-sm font-medium text-destructive">{note}</p> : null}
    </div>
  )
}

function PlanCard({ offer, busy, onChoose }: { offer: HostPlanOffer; busy: boolean; onChoose: () => void }) {
  return (
    <article
      className="flex h-full flex-col rounded-3xl bg-card p-5 shadow-sm ring-2 ring-[var(--plan-color)] sm:p-6"
      style={{ ['--plan-color' as string]: offer.color, ['--plan-edge' as string]: offer.edge }}
    >
      <p className="font-[Bungee] text-3xl tracking-tight" style={{ color: offer.color }}>
        {offer.name}
      </p>
      <p className="mt-2 text-sm text-muted-foreground">{offer.tagline}</p>
      <p className="mt-4 font-[Bungee] text-4xl leading-none" style={{ color: offer.color }}>
        ${offer.price}
        <span className="ml-1 align-baseline font-[inherit] text-base text-muted-foreground">/mo</span>
      </p>
      <PlanPerks offer={offer} className="mt-5 flex-1" />
      <button
        type="button"
        disabled={busy}
        onClick={onChoose}
        className="mt-6 w-full rounded-full px-5 py-3 text-base font-bold text-white shadow-[0_4px_0_var(--plan-edge)] transition hover:-translate-y-0.5 disabled:opacity-70"
        style={{ background: offer.color }}
      >
        {busy ? 'Opening…' : `Choose ${offer.name}`}
      </button>
    </article>
  )
}

function PlanPerks({ offer, className = '' }: { offer: HostPlanOffer; className?: string }) {
  return (
    <ul className={`space-y-2 text-sm ${className}`}>
      {offer.perks.map((perk) => (
        <li key={perk} className="flex items-start gap-2">
          <Check className="mt-0.5 size-4 shrink-0" style={{ color: offer.color }} aria-hidden />
          <span>{perk}</span>
        </li>
      ))}
    </ul>
  )
}
