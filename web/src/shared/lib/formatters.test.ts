import { describe, expect, it } from 'vitest'
import { formatBRL, formatDateBR, formatDueDays } from './formatters'

// Intl.NumberFormat('pt-BR', { style: 'currency', ... }) puts a non-breaking space (U+00A0)
// between "R$" and the amount, not a regular space — matches real rendered output.
const NBSP = ' '

describe('formatBRL', () => {
  it('formats positive amounts as pt-BR currency', () => {
    expect(formatBRL(1250.5)).toBe(`R$${NBSP}1.250,50`)
  })

  it('wraps negative amounts in parentheses by default', () => {
    expect(formatBRL(-42)).toBe(`(R$${NBSP}42,00)`)
  })

  it('prefixes a sign when showSign is set', () => {
    expect(formatBRL(42, { showSign: true })).toBe(`+ R$${NBSP}42,00`)
    expect(formatBRL(-42, { showSign: true })).toBe(`- R$${NBSP}42,00`)
  })
})

describe('formatDateBR', () => {
  it('formats an ISO date as dd/MM/yyyy', () => {
    expect(formatDateBR('2026-08-31T12:00:00Z')).toBe('31/08/2026')
  })
})

describe('formatDueDays', () => {
  it('flags overdue dates as urgent', () => {
    const yesterday = new Date()
    yesterday.setDate(yesterday.getDate() - 2)
    expect(formatDueDays(yesterday).urgency).toBe('urgent')
  })

  it('flags today as warning', () => {
    expect(formatDueDays(new Date()).urgency).toBe('warning')
  })

  it('flags future dates as neutral', () => {
    const nextWeek = new Date()
    nextWeek.setDate(nextWeek.getDate() + 7)
    expect(formatDueDays(nextWeek).urgency).toBe('neutral')
  })
})
