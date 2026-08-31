import type { LucideIcon } from 'lucide-react'
import { cn } from '@/shared/lib/cn'

interface KpiCardProps {
  label: string
  value: string
  icon?: LucideIcon
  delta?: { label: string; direction: 'up' | 'down' | 'flat' }
  className?: string
}

const DELTA_COLOR: Record<NonNullable<KpiCardProps['delta']>['direction'], string> = {
  up: 'text-success',
  down: 'text-danger',
  flat: 'text-text-muted',
}

/** Hierarchy per architecture note §8.5: micro label -> large mono metric -> delta -> (sparkline, added per-screen). */
export function KpiCard({ label, value, icon: Icon, delta, className }: KpiCardProps) {
  return (
    <div className={cn('rounded border border-border bg-surface p-4', className)}>
      <div className="mb-2 flex items-center justify-between">
        <span className="text-xs font-medium uppercase tracking-wide text-text-muted">{label}</span>
        {Icon && <Icon className="h-4 w-4 text-text-muted" strokeWidth={1.5} />}
      </div>
      <div className="font-mono text-2xl font-bold tracking-tight text-text-primary tabular-nums">{value}</div>
      {delta && (
        <div className={cn('mt-1 text-xs font-medium', DELTA_COLOR[delta.direction])}>{delta.label}</div>
      )}
    </div>
  )
}
