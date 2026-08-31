import type { LucideIcon } from 'lucide-react'
import { Inbox } from 'lucide-react'
import type { ReactNode } from 'react'

interface EmptyStateProps {
  icon?: LucideIcon
  title: string
  description?: string
  action?: ReactNode
  secondaryAction?: ReactNode
}

export function EmptyState({
  icon: Icon = Inbox,
  title,
  description,
  action,
  secondaryAction,
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded border border-dashed border-border py-16 text-center">
      <Icon className="h-8 w-8 text-text-muted" strokeWidth={1.5} />
      <div className="space-y-1">
        <p className="text-sm font-medium text-text-primary">{title}</p>
        {description && <p className="text-sm text-text-muted">{description}</p>}
      </div>
      {(action || secondaryAction) && (
        <div className="mt-2 flex items-center gap-2">
          {action}
          {secondaryAction}
        </div>
      )}
    </div>
  )
}
