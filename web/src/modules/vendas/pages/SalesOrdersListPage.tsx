import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, ShoppingCart } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { clientesResource } from '@/modules/clientes/api/resource'
import { formatBRL, formatDateBR } from '@/shared/lib/formatters'
import { Button } from '@/shared/ui/Button'
import { EmptyState } from '@/shared/ui/EmptyState'
import { ErrorState } from '@/shared/ui/ErrorState'
import { StatusBadge } from '@/shared/ui/StatusBadge'
import { TableSkeleton } from '@/shared/ui/Skeleton'
import { useCreateSalesOrder } from '../api/mutations'
import { useSalesOrdersList } from '../api/queries'
import type { SalesOrderDto } from '../api/types'
import { SALES_ORDER_STATUS_LABEL, SALES_ORDER_STATUS_VARIANT } from './statusBadge'

export function SalesOrdersListPage() {
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { data: orders, isLoading, isError, error, refetch } = useSalesOrdersList({})
  const { data: clientes } = clientesResource.useList({ isActive: true, pageSize: 100 })

  const customerNameById = useMemo(() => {
    const map = new Map<string, string>()
    clientes?.forEach((c) => map.set(c.id, c.name))
    return map
  }, [clientes])

  return (
    <div>
      <header className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary">Pedidos de Venda</h1>
          <p className="text-sm text-text-muted">Orçamentos e pedidos — Draft → Aprovado → Cancelado.</p>
        </div>
        <Button onClick={() => setIsFormOpen((open) => !open)}>
          <Plus className="h-4 w-4" />
          Novo Pedido
        </Button>
      </header>

      {isFormOpen && <NewOrderForm onDone={() => setIsFormOpen(false)} />}

      {isLoading && <TableSkeleton columns={5} />}
      {isError && <ErrorState message={error?.message} onRetry={() => refetch()} />}

      {!isLoading && !isError && orders && orders.length === 0 && (
        <EmptyState
          icon={ShoppingCart}
          title="Nenhum pedido de venda"
          description="Crie o primeiro pedido para um cliente ativo."
          action={
            <Button onClick={() => setIsFormOpen(true)}>
              <Plus className="h-4 w-4" />
              Novo Pedido
            </Button>
          }
        />
      )}

      {!isLoading && !isError && orders && orders.length > 0 && (
        <div className="overflow-x-auto rounded border border-border">
          <table className="w-full text-left text-[13px]">
            <thead className="bg-surface-muted text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              <tr>
                <th scope="col" className="px-3 py-2.5">Cliente</th>
                <th scope="col" className="px-3 py-2.5 font-mono">Criado em</th>
                <th scope="col" className="px-3 py-2.5 text-right">Total</th>
                <th scope="col" className="w-[110px] px-3 py-2.5 text-right">Status</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <SalesOrderRow key={order.id} order={order} customerName={customerNameById.get(order.customerId)} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function SalesOrderRow({ order, customerName }: { order: SalesOrderDto; customerName?: string }) {
  const navigate = useNavigate()
  const openDetail = () => navigate(`/vendas/pedidos/${order.id}`)

  return (
    <tr
      role="button"
      tabIndex={0}
      aria-label={`Ver pedido de ${customerName ?? order.customerId}`}
      className="h-[42px] cursor-pointer border-t border-border hover:bg-surface-muted focus-visible:bg-surface-muted"
      onClick={openDetail}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault()
          openDetail()
        }
      }}
    >
      <td className="px-3 py-2 font-medium text-text-primary">{customerName ?? order.customerId}</td>
      <td className="px-3 py-2 font-mono text-xs text-text-muted">{formatDateBR(order.createdAtUtc)}</td>
      <td className="px-3 py-2 text-right font-mono tabular-nums">{formatBRL(order.total)}</td>
      <td className="px-3 py-2 text-right">
        <StatusBadge
          variant={SALES_ORDER_STATUS_VARIANT[order.status]}
          label={SALES_ORDER_STATUS_LABEL[order.status]}
        />
      </td>
    </tr>
  )
}

const newOrderSchema = z.object({ customerId: z.string().min(1, 'Selecione um cliente.') })
type NewOrderFormValues = z.infer<typeof newOrderSchema>

function NewOrderForm({ onDone }: { onDone: () => void }) {
  const navigate = useNavigate()
  const { data: clientes } = clientesResource.useList({ isActive: true, pageSize: 100 })
  const createOrder = useCreateSalesOrder()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<NewOrderFormValues>({ resolver: zodResolver(newOrderSchema) })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const order = await createOrder.mutateAsync({ customerId: values.customerId })
      onDone()
      navigate(`/vendas/pedidos/${order.id}`)
    } catch {
      // Field/conflict errors surface inline via react-hook-form in the (rare) case the
      // customer became inactive between load and submit — the 409 message is generic enough
      // to just show via the mutation's own error state below.
    }
  })

  return (
    <form onSubmit={onSubmit} className="mb-6 rounded border border-border bg-surface p-4">
      <label className="flex flex-col gap-1">
        <span className="text-xs font-semibold text-text-secondary">Cliente</span>
        <select {...register('customerId')} className="input" defaultValue="">
          <option value="" disabled>
            Selecione um cliente...
          </option>
          {clientes?.map((cliente) => (
            <option key={cliente.id} value={cliente.id}>
              {cliente.name}
            </option>
          ))}
        </select>
        {errors.customerId && (
          <span className="text-[11px] font-medium text-danger">{errors.customerId.message}</span>
        )}
      </label>
      {createOrder.isError && (
        <p className="mt-2 text-[11px] font-medium text-danger">{createOrder.error.message}</p>
      )}
      <div className="mt-4 flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onDone}>
          Cancelar
        </Button>
        <Button type="submit" disabled={createOrder.isPending}>
          {createOrder.isPending ? 'Criando...' : 'Criar Rascunho'}
        </Button>
      </div>
    </form>
  )
}
