import {
  LayoutDashboard,
  Users,
  Truck,
  ShoppingCart,
  ShoppingBag,
  Wallet,
  Landmark,
  BarChart3,
  Settings,
  type LucideIcon,
} from 'lucide-react'
import type { Role } from '@/shared/auth/roles'

export interface NavItem {
  label: string
  path: string
  icon: LucideIcon
  /** Roles allowed to view this module — currently permissive since only Clientes/Fornecedores
   *  have real backend policies; tighten per-module once the corresponding backend controller
   *  declares [Authorize(Roles = ...)]. */
  roles: readonly Role[]
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

const ALL_ROLES: readonly Role[] = ['Admin', 'Manager', 'Sales', 'Finance']

export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Visão Geral',
    items: [{ label: 'Dashboard', path: '/', icon: LayoutDashboard, roles: ALL_ROLES }],
  },
  {
    label: 'Comercial',
    items: [
      { label: 'Clientes', path: '/clientes', icon: Users, roles: ALL_ROLES },
      { label: 'Fornecedores', path: '/fornecedores', icon: Truck, roles: ALL_ROLES },
      { label: 'Vendas', path: '/vendas', icon: ShoppingCart, roles: ALL_ROLES },
      { label: 'Compras', path: '/compras', icon: ShoppingBag, roles: ALL_ROLES },
    ],
  },
  {
    label: 'Financeiro',
    items: [
      { label: 'Financeiro', path: '/financeiro', icon: Wallet, roles: ALL_ROLES },
      { label: 'Caixa', path: '/caixa', icon: Landmark, roles: ALL_ROLES },
      { label: 'Relatórios', path: '/relatorios', icon: BarChart3, roles: ALL_ROLES },
    ],
  },
  {
    label: 'Sistema',
    // Expected to become Admin-only once Administração has a real backend policy — left
    // permissive for now rather than guessing, per the "mirror the backend" rule above.
    items: [{ label: 'Administração', path: '/administracao', icon: Settings, roles: ALL_ROLES }],
  },
]
