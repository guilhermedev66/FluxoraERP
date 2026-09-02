import { BarChart3 } from 'lucide-react'
import { formatBRL } from '@/shared/lib/formatters'
import { EmptyState } from '@/shared/ui/EmptyState'
import { ErrorState } from '@/shared/ui/ErrorState'
import { Skeleton } from '@/shared/ui/Skeleton'
import { useNetResultTrend } from '../api/queries'

const MONTH_LABEL = new Intl.DateTimeFormat('pt-BR', { month: 'short', timeZone: 'UTC' })

function periodLabel(period: string): string {
  // period is "yyyy-MM"; parsed as UTC midnight to avoid local-timezone month drift.
  const [year, month] = period.split('-').map(Number)
  const label = MONTH_LABEL.format(new Date(Date.UTC(year, month - 1, 1))).replace('.', '')
  return `${label}/${String(year).slice(2)}`
}

// h-40 (10rem = 160px) is used consistently below so the skeleton and the real chart occupy the
// same footprint, avoiding a layout shift when data arrives.
const CHART_HEIGHT_CLASS = 'h-40'

export function NetResultChart() {
  const { data, isLoading, isError, error, refetch } = useNetResultTrend()

  return (
    <section aria-labelledby="net-result-heading" className="rounded border border-border bg-surface p-4">
      <div className="mb-4 flex items-center justify-between">
        <h2 id="net-result-heading" className="text-xs font-semibold uppercase tracking-wider text-text-muted">
          Receita x Despesa (6 meses)
        </h2>
        <div className="flex items-center gap-3 text-xs text-text-muted">
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full" style={{ backgroundColor: 'var(--chart-series-1)' }} />
            Receita
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full" style={{ backgroundColor: 'var(--chart-series-2)' }} />
            Despesa
          </span>
        </div>
      </div>

      {isLoading && (
        <div role="status" aria-label="Carregando gráfico de receita x despesa">
          <Skeleton className={`w-full ${CHART_HEIGHT_CLASS}`} />
        </div>
      )}

      {isError && <ErrorState message={error?.message} onRetry={() => refetch()} />}

      {data && data.length === 0 && (
        <EmptyState
          icon={BarChart3}
          title="Sem movimentação no período"
          description="Vendas aprovadas e compras confirmadas nos últimos 6 meses aparecem aqui."
        />
      )}

      {data && data.length > 0 && <BarRows data={data} />}
    </section>
  )
}

function BarRows({ data }: { data: { period: string; revenue: number; expenses: number }[] }) {
  const max = Math.max(1, ...data.flatMap((d) => [d.revenue, d.expenses]))

  return (
    <div
      className={`flex items-end justify-between gap-3 ${CHART_HEIGHT_CLASS}`}
      role="img"
      aria-label={`Receita e despesa por mês: ${data
        .map((d) => `${periodLabel(d.period)}, receita ${formatBRL(d.revenue)}, despesa ${formatBRL(d.expenses)}`)
        .join('; ')}`}
    >
      {data.map((d) => (
        <div key={d.period} className="flex h-full flex-1 flex-col items-center justify-end gap-1">
          <div className="flex h-full w-full items-end justify-center gap-1">
            <div
              className="w-full max-w-3.5 rounded-t-sm transition-[height] duration-300 ease-out"
              style={{
                height: `${(d.revenue / max) * 100}%`,
                backgroundColor: 'var(--chart-series-1)',
                minHeight: d.revenue > 0 ? 2 : 0,
              }}
            />
            <div
              className="w-full max-w-3.5 rounded-t-sm transition-[height] duration-300 ease-out"
              style={{
                height: `${(d.expenses / max) * 100}%`,
                backgroundColor: 'var(--chart-series-2)',
                minHeight: d.expenses > 0 ? 2 : 0,
              }}
            />
          </div>
          <span className="text-[11px] font-medium text-text-muted">{periodLabel(d.period)}</span>
        </div>
      ))}
    </div>
  )
}
