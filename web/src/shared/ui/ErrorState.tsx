import { AlertTriangle } from 'lucide-react'
import { Button } from './Button'

interface ErrorStateProps {
  message?: string
  onRetry?: () => void
}

export function ErrorState({ message = 'Ocorreu um erro inesperado.', onRetry }: ErrorStateProps) {
  return (
    <div
      role="alert"
      className="flex flex-col items-center justify-center gap-3 rounded border border-danger-border bg-danger-bg py-16 text-center"
    >
      <AlertTriangle className="h-8 w-8 text-danger" strokeWidth={1.5} aria-hidden="true" />
      <p className="text-sm font-medium text-text-primary">{message}</p>
      {onRetry && (
        <Button variant="secondary" onClick={onRetry}>
          Tentar novamente
        </Button>
      )}
    </div>
  )
}
