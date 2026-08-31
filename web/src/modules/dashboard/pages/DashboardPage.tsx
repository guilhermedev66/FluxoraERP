import { useAuth } from '@/shared/auth/AuthContext'
import { formatBRL } from '@/shared/lib/formatters'
import { KpiCard } from '@/shared/ui/KpiCard'

/**
 * Placeholder dashboard proving out the KPI-card visual pattern (architecture note §8.5).
 * Values are illustrative — wired to real Contas a Pagar/Receber data once Financeiro
 * lands on the backend (Milestone 3).
 */
export function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="mx-auto max-w-5xl px-6 py-8">
      <header className="mb-6">
        <h1 className="text-2xl font-bold tracking-tight text-text-primary">
          Olá, {user?.displayName ?? user?.email ?? 'usuário'}
        </h1>
        <p className="text-sm text-text-muted">Visão geral do dia — dados ilustrativos, aguardando Milestone 3.</p>
      </header>

      <div className="grid grid-cols-3 gap-4">
        <KpiCard label="Vencidos" value={formatBRL(0)} delta={{ label: '0 documentos', direction: 'flat' }} />
        <KpiCard label="Vencem Hoje" value={formatBRL(0)} delta={{ label: '0 documentos', direction: 'flat' }} />
        <KpiCard label="A Vencer (30 dias)" value={formatBRL(0)} delta={{ label: '0 documentos', direction: 'flat' }} />
      </div>
    </div>
  )
}
