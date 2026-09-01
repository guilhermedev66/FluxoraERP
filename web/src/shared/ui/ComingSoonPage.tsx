import { Construction } from 'lucide-react'

export function ComingSoonPage({ title }: { title: string }) {
  return (
    <div className="mx-auto flex max-w-5xl flex-col items-center justify-center gap-3 px-6 py-24 text-center">
      <Construction className="h-8 w-8 text-text-muted" strokeWidth={1.5} aria-hidden="true" />
      <h1 className="text-xl font-semibold text-text-primary">{title}</h1>
      <p className="text-sm text-text-muted">Módulo planejado para um milestone futuro.</p>
    </div>
  )
}
