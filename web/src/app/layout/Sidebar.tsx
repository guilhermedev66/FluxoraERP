import { X } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '@/shared/auth/AuthContext'
import { cn } from '@/shared/lib/cn'
import { NAV_GROUPS } from './navigation'

interface SidebarProps {
  isMobileOpen: boolean
  onClose: () => void
}

export function Sidebar({ isMobileOpen, onClose }: SidebarProps) {
  const { hasRole } = useAuth()

  return (
    <>
      {isMobileOpen && (
        <div className="fixed inset-0 z-30 bg-black/40 lg:hidden" onClick={onClose} aria-hidden="true" />
      )}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-40 flex w-60 shrink-0 flex-col border-r border-border bg-surface transition-transform duration-200 ease-out lg:static lg:translate-x-0',
          isMobileOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex h-14 items-center justify-between border-b border-border px-4">
          <span className="text-sm font-bold tracking-tight text-text-primary">Fluxora ERP</span>
          <button
            onClick={onClose}
            aria-label="Fechar menu"
            className="flex h-8 w-8 items-center justify-center rounded text-text-muted hover:bg-surface-muted lg:hidden"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <nav aria-label="Navegação principal" className="flex-1 space-y-5 overflow-y-auto px-2 py-4">
          {NAV_GROUPS.map((group) => {
            const visibleItems = group.items.filter((item) => hasRole(...item.roles))
            if (visibleItems.length === 0) return null

            return (
              <div key={group.label}>
                <p className="mb-1 px-2 text-[11px] font-semibold uppercase tracking-wider text-text-muted">
                  {group.label}
                </p>
                <div className="space-y-0.5">
                  {visibleItems.map((item) => (
                    <NavLink
                      key={item.path}
                      to={item.path}
                      end={item.path === '/'}
                      onClick={onClose}
                      className={({ isActive }) =>
                        cn(
                          'flex items-center gap-2 rounded px-2 py-1.5 text-sm font-medium transition-colors duration-150',
                          isActive
                            ? 'bg-surface-muted text-text-primary'
                            : 'text-text-secondary hover:bg-surface-muted hover:text-text-primary',
                        )
                      }
                    >
                      <item.icon className="h-4 w-4" strokeWidth={1.5} />
                      {item.label}
                    </NavLink>
                  ))}
                </div>
              </div>
            )
          })}
        </nav>
      </aside>
    </>
  )
}
