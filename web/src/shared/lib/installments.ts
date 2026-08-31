export interface InstallmentItem {
  installmentNumber: number
  totalInstallments: number
  dueDate: string
  amount: number
}

/**
 * Splits a total into N installments without losing centavos to floating-point rounding.
 * The remainder (in cents) is distributed one-by-one to the earliest installments —
 * mirrors the backend's own installment-splitting rule (docs/architecture.md).
 */
export function generateInstallments(
  totalAmount: number,
  installmentCount: number,
  firstDueDate: Date,
  intervalDays = 30,
): InstallmentItem[] {
  const totalCents = Math.round(totalAmount * 100)
  const baseCents = Math.floor(totalCents / installmentCount)
  const remainderCents = totalCents % installmentCount

  const items: InstallmentItem[] = []

  for (let i = 0; i < installmentCount; i++) {
    const itemCents = baseCents + (i < remainderCents ? 1 : 0)
    const dueDate = new Date(firstDueDate)
    dueDate.setDate(dueDate.getDate() + i * intervalDays)

    items.push({
      installmentNumber: i + 1,
      totalInstallments: installmentCount,
      dueDate: dueDate.toISOString().split('T')[0],
      amount: itemCents / 100,
    })
  }

  return items
}
