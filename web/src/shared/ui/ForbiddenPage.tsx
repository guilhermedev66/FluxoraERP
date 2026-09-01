import { ShieldAlert } from 'lucide-react'
import { Link } from 'react-router-dom'

export function ForbiddenPage() {
  return (
    <div className="mx-auto flex max-w-5xl flex-col items-center justify-center gap-3 px-6 py-24 text-center">
      <ShieldAlert className="h-8 w-8 text-danger" strokeWidth={1.5} aria-hidden="true" />
      <h1 className="text-xl font-semibold text-text-primary">Acesso não permitido</h1>
      <p className="text-sm text-text-muted">Você não tem permissão para acessar esta página.</p>
      <Link to="/" className="mt-2 text-sm font-medium text-text-muted hover:text-text-primary hover:underline">
        Voltar ao início
      </Link>
    </div>
  )
}
