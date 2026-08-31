import { cn } from '@/shared/lib/cn'

export type StatusVariant = 'success' | 'warning' | 'destructive' | 'neutral'

const VARIANT_CLASSES: Record<StatusVariant, { badge: string; dot: string }> = {
  success: {
    badge: 'bg-success-bg text-success border-success-border',
    dot: 'bg-success',
  },
  warning: {
    badge: 'bg-warning-bg text-warning border-warning-border',
    dot: 'bg-warning',
  },
  destructive: {
    badge: 'bg-danger-bg text-danger border-danger-border',
    dot: 'bg-danger',
  },
  neutral: {
    badge: 'bg-neutral-bg text-neutral border-neutral-border',
    dot: 'bg-neutral',
  },
}

interface StatusBadgeProps {
  variant: StatusVariant
  label: string
  className?: string
}

/** Success = Liquidado/Pago/Recebido/Ativo · Warning = Vence Hoje/Em Aberto · Destructive = Vencido/Cancelado · Neutral = Rascunho/Agendado. */
export function StatusBadge({ variant, label, className }: StatusBadgeProps) {
  const classes = VARIANT_CLASSES[variant]
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded border px-2 py-0.5 text-[11px] font-medium leading-none',
        classes.badge,
        className,
      )}
    >
      <span className={cn('h-1.5 w-1.5 rounded-full', classes.dot)} />
      {label}
    </span>
  )
}
