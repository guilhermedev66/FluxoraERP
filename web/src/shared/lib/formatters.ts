/** R$ 1.250,50 / (R$ 1.250,50) for negatives / + R$ 1.250,50 when showSign is set. */
export function formatBRL(amount: number, options?: { showSign?: boolean }): string {
  const formatted = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Math.abs(amount))

  if (amount < 0) return options?.showSign ? `- ${formatted}` : `(${formatted})`
  if (amount > 0 && options?.showSign) return `+ ${formatted}`
  return formatted
}

/** dd/MM/yyyy in America/Sao_Paulo. */
export function formatDateBR(date: string | Date): string {
  const d = typeof date === 'string' ? new Date(date) : date
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: 'America/Sao_Paulo',
  }).format(d)
}

export type DueUrgency = 'urgent' | 'warning' | 'neutral'

/** Relative due-date label: D-2 (2d overdue), D+0 (due today), D+15 (due in 15 days). */
export function formatDueDays(dueDate: string | Date): { label: string; urgency: DueUrgency } {
  const target = typeof dueDate === 'string' ? new Date(dueDate) : new Date(dueDate.getTime())
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  target.setHours(0, 0, 0, 0)

  const diffDays = Math.round((target.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))

  if (diffDays < 0) return { label: `D${diffDays} (${Math.abs(diffDays)}d atraso)`, urgency: 'urgent' }
  if (diffDays === 0) return { label: 'D+0 (Vence hoje)', urgency: 'warning' }
  return { label: `D+${diffDays}`, urgency: 'neutral' }
}
