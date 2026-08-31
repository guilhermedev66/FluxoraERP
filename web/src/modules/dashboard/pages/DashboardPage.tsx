import { useAuth } from '@/shared/auth/AuthContext'
import { formatBRL } from '@/shared/lib/formatters'
import { CardSkeleton } from '@/shared/ui/Skeleton'
import { ErrorState } from '@/shared/ui/ErrorState'
import { KpiCard } from '@/shared/ui/KpiCard'
import { useDashboardSummary } from '../api/queries'

function docLabel(count: number): string {
  return count === 1 ? '1 documento' : `${count} documentos`
}

export function DashboardPage() {
  const { user } = useAuth()
  const { data, isLoading, isError, error, refetch } = useDashboardSummary()

  return (
    <div className="mx-auto max-w-5xl px-6 py-8">
      <header className="mb-6">
        <h1 className="text-2xl font-bold tracking-tight text-text-primary">
          Olá, {user?.displayName ?? user?.email ?? 'usuário'}
        </h1>
        <p className="text-sm text-text-muted">Visão geral financeira consolidada.</p>
      </header>

      {isLoading && (
        <div className="mb-4 grid grid-cols-4 gap-4">
          <CardSkeleton />
          <CardSkeleton />
          <CardSkeleton />
          <CardSkeleton />
        </div>
      )}

      {isError && <ErrorState message={error?.message} onRetry={() => refetch()} />}

      {data && (
        <>
          <div className="mb-4 grid grid-cols-4 gap-4">
            <KpiCard label="Saldo Consolidado" value={formatBRL(data.currentBalance)} />
            <KpiCard label="Receitas Realizadas (Mês)" value={formatBRL(data.monthRevenue)} />
            <KpiCard label="Despesas Realizadas (Mês)" value={formatBRL(data.monthExpenses)} />
            <KpiCard
              label="Resultado Operacional Líq."
              value={formatBRL(data.monthNet, { showSign: true })}
              delta={
                data.monthRevenue > 0
                  ? {
                      label: `Margem: ${((data.monthNet / data.monthRevenue) * 100).toFixed(1)}%`,
                      direction: data.monthNet > 0 ? 'up' : data.monthNet < 0 ? 'down' : 'flat',
                    }
                  : undefined
              }
            />
          </div>

          <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-text-muted">
            Farol de Vencimentos
          </p>
          <div className="grid grid-cols-3 gap-4">
            <KpiCard
              label="Vencidos"
              value={formatBRL(data.overdueReceivablesAmount + data.overduePayablesAmount)}
              delta={{
                label: docLabel(data.overdueReceivablesCount + data.overduePayablesCount),
                direction: 'flat',
              }}
            />
            <KpiCard
              label="Vencem Hoje"
              value={formatBRL(data.dueTodayAmount)}
              delta={{ label: docLabel(data.dueTodayCount), direction: 'flat' }}
            />
            <KpiCard
              label="A Vencer (30 dias)"
              value={formatBRL(data.dueNext30DaysAmount)}
              delta={{ label: docLabel(data.dueNext30DaysCount), direction: 'flat' }}
            />
          </div>
        </>
      )}
    </div>
  )
}
