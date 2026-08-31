import { type ButtonHTMLAttributes, forwardRef } from 'react'
import { cn } from '@/shared/lib/cn'

type Variant = 'primary' | 'secondary' | 'ghost' | 'destructive'

const VARIANT_CLASSES: Record<Variant, string> = {
  primary: 'bg-accent text-accent-foreground hover:opacity-90',
  secondary: 'border border-border bg-surface text-text-primary hover:bg-surface-muted',
  ghost: 'text-text-primary hover:bg-surface-muted',
  destructive: 'bg-danger text-white hover:opacity-90',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'primary', className, ...props }, ref) => (
    <button
      ref={ref}
      className={cn(
        'inline-flex h-9 items-center justify-center gap-1.5 rounded px-3 text-sm font-medium transition-colors duration-150 ease-out disabled:pointer-events-none disabled:opacity-50',
        VARIANT_CLASSES[variant],
        className,
      )}
      {...props}
    />
  ),
)
Button.displayName = 'Button'
