import { Users } from 'lucide-react'
import { ROLES } from '@/shared/auth/roles'
import { PartyCrudPage } from '@/shared/ui/PartyCrudPage'
import { clientesResource } from '../api/resource'

export function ClientesListPage() {
  return (
    <PartyCrudPage
      resource={clientesResource}
      title="Clientes"
      subtitle="Cadastro de clientes (PF/PJ)."
      entityLabel="Cliente"
      icon={Users}
      // Backend CustomersController only requires [Authorize] (any role) — no role
      // restriction to mirror yet. Narrow this once the backend adds one.
      createRoles={ROLES}
    />
  )
}
