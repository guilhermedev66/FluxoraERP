import { Truck } from 'lucide-react'
import { ROLES } from '@/shared/auth/roles'
import { PartyCrudPage } from '@/shared/ui/PartyCrudPage'
import { fornecedoresResource } from '../api/resource'

export function FornecedoresListPage() {
  return (
    <PartyCrudPage
      resource={fornecedoresResource}
      title="Fornecedores"
      subtitle="Cadastro de fornecedores."
      entityLabel="Fornecedor"
      icon={Truck}
      // Backend SuppliersController only requires [Authorize] (any role) — no role
      // restriction to mirror yet. Narrow this once the backend adds one.
      createRoles={ROLES}
    />
  )
}
