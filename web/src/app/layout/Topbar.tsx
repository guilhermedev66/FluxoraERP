import { LogOut, Menu, Moon, Sun } from 'lucide-react'
import { useTheme } from '@/app/providers/ThemeProvider'
import { useAuth } from '@/shared/auth/AuthContext'
import { Button } from '@/shared/ui/Button'

interface TopbarProps {
  onOpenMobileNav: () => void
}

export function Topbar({ onOpenMobileNav }: TopbarProps) {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-surface px-4">
      <div>
        <button
          onClick={onOpenMobileNav}
          aria-label="Abrir menu"
          className="flex h-8 w-8 items-center justify-center rounded text-text-muted hover:bg-surface-muted lg:hidden"
        >
          <Menu className="h-4 w-4" />
        </button>
      </div>
      <div className="flex items-center gap-3">
        <button
          onClick={toggleTheme}
          aria-label="Alternar tema"
          className="flex h-8 w-8 items-center justify-center rounded text-text-muted hover:bg-surface-muted"
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </button>
        <span className="text-sm text-text-secondary">{user?.displayName ?? user?.email}</span>
        <Button variant="ghost" onClick={logout}>
          <LogOut className="h-4 w-4" />
          Sair
        </Button>
      </div>
    </header>
  )
}
