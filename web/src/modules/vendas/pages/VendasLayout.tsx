import { NavLink, Outlet } from 'react-router-dom'
import { cn } from '@/shared/lib/cn'

const TABS = [
  { label: 'Pedidos', path: '/vendas', end: true },
  { label: 'Produtos', path: '/vendas/produtos', end: false },
]

export function VendasLayout() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-6 sm:px-6 sm:py-8">
      <nav className="mb-6 flex gap-1 border-b border-border">
        {TABS.map((tab) => (
          <NavLink
            key={tab.path}
            to={tab.path}
            end={tab.end}
            className={({ isActive }) =>
              cn(
                'border-b-2 px-3 py-2 text-sm font-medium transition-colors duration-150',
                isActive
                  ? 'border-accent text-text-primary'
                  : 'border-transparent text-text-muted hover:text-text-primary',
              )
            }
          >
            {tab.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  )
}
