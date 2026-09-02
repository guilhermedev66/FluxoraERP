import { Laptop, Moon, Sun } from 'lucide-react'
import type { ThemePreference } from '@/app/providers/ThemeProvider'
import { useTheme } from '@/app/providers/ThemeProvider'
import { cn } from '@/shared/lib/cn'

const OPTIONS: { value: ThemePreference; label: string; icon: typeof Sun }[] = [
  { value: 'light', label: 'Tema claro', icon: Sun },
  { value: 'dark', label: 'Tema escuro', icon: Moon },
  { value: 'system', label: 'Seguir o sistema', icon: Laptop },
]

/** Three-way Light/Dark/System control. A single toggle can't represent "follow the OS". */
export function ThemeSwitcher() {
  const { preference, setPreference } = useTheme()

  return (
    <div
      role="radiogroup"
      aria-label="Tema"
      className="flex items-center gap-0.5 rounded border border-border bg-surface-muted p-0.5"
    >
      {OPTIONS.map(({ value, label, icon: Icon }) => {
        const active = preference === value
        return (
          <button
            key={value}
            type="button"
            role="radio"
            aria-checked={active}
            aria-label={label}
            title={label}
            onClick={() => setPreference(value)}
            className={cn(
              'flex h-7 w-7 items-center justify-center rounded transition-colors duration-150 ease-out',
              active
                ? 'bg-surface text-text-primary shadow-sm'
                : 'text-text-muted hover:text-text-primary',
            )}
          >
            <Icon className="h-3.5 w-3.5" strokeWidth={2} aria-hidden="true" />
          </button>
        )
      })}
    </div>
  )
}
