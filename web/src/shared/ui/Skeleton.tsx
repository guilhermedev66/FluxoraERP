import { cn } from '@/shared/lib/cn'

export function Skeleton({ className }: { className?: string }) {
  return <div className={cn('animate-pulse rounded bg-surface-muted', className)} />
}

/** Keeps 1:1 dimensional parity with a real table: headers stay crisp, only rows shimmer. */
export function TableSkeleton({ rows = 8, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="overflow-hidden rounded border border-border" role="status" aria-label="Carregando dados">
      <div className="flex border-b border-border bg-surface-muted px-3 py-2.5">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} className="mr-6 h-3 w-20 last:mr-0" />
        ))}
      </div>
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex items-center border-b border-border px-3 py-2.5 last:border-0">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} className="mr-6 h-3 w-24 last:mr-0" />
          ))}
        </div>
      ))}
    </div>
  )
}

export function CardSkeleton() {
  return (
    <div className="rounded border border-border bg-surface p-4">
      <Skeleton className="mb-3 h-3 w-24" />
      <Skeleton className="mb-2 h-6 w-32" />
      <Skeleton className="h-3 w-16" />
    </div>
  )
}
