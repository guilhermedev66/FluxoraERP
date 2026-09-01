import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from '@/app/layout/AppShell'
import { RequireRole } from '@/shared/auth/RequireRole'
import { ROLES } from '@/shared/auth/roles'
import { ForbiddenPage } from '@/shared/ui/ForbiddenPage'
import { LoginPage } from '@/modules/auth/pages/LoginPage'
import { DashboardPage } from '@/modules/dashboard/pages/DashboardPage'
import { ClientesListPage } from '@/modules/clientes/pages/ClientesListPage'
import { FornecedoresListPage } from '@/modules/fornecedores/pages/FornecedoresListPage'
import { VendasLayout } from '@/modules/vendas/pages/VendasLayout'
import { SalesOrdersListPage } from '@/modules/vendas/pages/SalesOrdersListPage'
import { SalesOrderDetailPage } from '@/modules/vendas/pages/SalesOrderDetailPage'
import { ProductsListPage } from '@/modules/produtos/pages/ProductsListPage'
import { ComprasPage } from '@/modules/compras/pages/ComprasPage'
import { FinanceiroPage } from '@/modules/financeiro/pages/FinanceiroPage'
import { CaixaPage } from '@/modules/caixa/pages/CaixaPage'
import { RelatoriosPage } from '@/modules/relatorios/pages/RelatoriosPage'
import { AdministracaoPage } from '@/modules/administracao/pages/AdministracaoPage'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/403', element: <ForbiddenPage /> },
  {
    path: '/',
    element: (
      <RequireRole roles={ROLES}>
        <AppShell />
      </RequireRole>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'clientes', element: <ClientesListPage /> },
      { path: 'fornecedores', element: <FornecedoresListPage /> },
      {
        path: 'vendas',
        element: <VendasLayout />,
        children: [
          { index: true, element: <SalesOrdersListPage /> },
          { path: 'produtos', element: <ProductsListPage /> },
          { path: 'pedidos/:id', element: <SalesOrderDetailPage /> },
        ],
      },
      { path: 'compras', element: <ComprasPage /> },
      { path: 'financeiro', element: <FinanceiroPage /> },
      { path: 'caixa', element: <CaixaPage /> },
      { path: 'relatorios', element: <RelatoriosPage /> },
      { path: 'administracao', element: <AdministracaoPage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
