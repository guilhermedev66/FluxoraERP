import { ShieldAlert } from 'lucide-react'

export function ForbiddenPage() {
  return (
    <div className="mx-auto flex max-w-5xl flex-col items-center justify-center gap-3 px-6 py-24 text-center">
      <ShieldAlert className="h-8 w-8 text-danger" strokeWidth={1.5} />
      <h1 className="text-xl font-semibold text-text-primary">Acesso não permitido</h1>
      <p className="text-sm text-text-muted">Você não tem permissão para acessar esta página.</p>
    </div>
  )
}
