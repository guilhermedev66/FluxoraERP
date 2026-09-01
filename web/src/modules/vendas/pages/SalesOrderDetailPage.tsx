import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useParams } from 'react-router-dom'
import { z } from 'zod'
import type { ApiError } from '@/shared/api/errors'
import { clientesResource } from '@/modules/clientes/api/resource'
import { useProductsList } from '@/modules/produtos/api/queries'
import { formatBRL, formatDateBR } from '@/shared/lib/formatters'
import { Button } from '@/shared/ui/Button'
import { ErrorState } from '@/shared/ui/ErrorState'
import { StatusBadge } from '@/shared/ui/StatusBadge'
import { CardSkeleton } from '@/shared/ui/Skeleton'
import { useAddSalesOrderLine, useApproveSalesOrder, useCancelSalesOrder } from '../api/mutations'
import { useSalesOrder } from '../api/queries'
import { SALES_ORDER_STATUS_LABEL, SALES_ORDER_STATUS_VARIANT } from './statusBadge'

export function SalesOrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { data: order, isLoading, isError, error, refetch } = useSalesOrder(id ?? '')
  const { data: clientes } = clientesResource.useList({ pageSize: 100 })

  if (!id) return <Navigate to="/vendas" replace />
  if (isLoading) return <CardSkeleton />
  if (isError) return <ErrorState message={error?.message} onRetry={() => refetch()} />
  if (!order) return null

  const customerName = clientes?.find((c) => c.id === order.customerId)?.name ?? order.customerId
  const isDraft = order.status === 'Draft'

  return (
    <div>
      <Link to="/vendas" className="mb-4 inline-flex items-center gap-1 text-sm text-text-muted hover:text-text-primary">
        <ArrowLeft className="h-4 w-4" />
        Voltar para Pedidos
      </Link>

      <header className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="mb-1 flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight text-text-primary">{customerName}</h1>
            <StatusBadge variant={SALES_ORDER_STATUS_VARIANT[order.status]} label={SALES_ORDER_STATUS_LABEL[order.status]} />
          </div>
          <p className="text-sm text-text-muted">
            Criado em {formatDateBR(order.createdAtUtc)}
            {order.approvedAtUtc && ` · Aprovado em ${formatDateBR(order.approvedAtUtc)}`}
          </p>
        </div>
        {isDraft && <CancelOrderButton orderId={order.id} />}
      </header>

      <div className="mb-6 overflow-x-auto rounded border border-border">
        <table className="w-full text-left text-[13px]">
          <thead className="bg-surface-muted text-[11px] font-semibold uppercase tracking-wider text-text-muted">
            <tr>
              <th scope="col" className="px-3 py-2.5">Produto</th>
              <th scope="col" className="px-3 py-2.5 text-right">Qtd.</th>
              <th scope="col" className="px-3 py-2.5 text-right">Preço Unit.</th>
              <th scope="col" className="px-3 py-2.5 text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {order.lines.length === 0 && (
              <tr>
                <td colSpan={4} className="px-3 py-6 text-center text-sm text-text-muted">
                  Nenhum item adicionado ainda.
                </td>
              </tr>
            )}
            {order.lines.map((line) => (
              <tr key={line.id} className="h-[42px] border-t border-border">
                <td className="px-3 py-2 font-medium text-text-primary">{line.productName}</td>
                <td className="px-3 py-2 text-right font-mono tabular-nums">{line.quantity}</td>
                <td className="px-3 py-2 text-right font-mono tabular-nums">{formatBRL(line.unitPrice)}</td>
                <td className="px-3 py-2 text-right font-mono tabular-nums">{formatBRL(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t border-border bg-surface-muted">
              <td colSpan={3} className="px-3 py-2 text-right text-xs font-semibold uppercase tracking-wide text-text-muted">
                Total do Pedido
              </td>
              <td className="px-3 py-2 text-right font-mono text-sm font-bold tabular-nums">{formatBRL(order.total)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      {isDraft && (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <AddLineForm orderId={order.id} />
          <ApproveOrderForm orderId={order.id} disabled={order.lines.length === 0} />
        </div>
      )}
    </div>
  )
}

const addLineSchema = z.object({
  productId: z.string().min(1, 'Selecione um produto.'),
  quantity: z.coerce.number().positive('Quantidade deve ser maior que zero.'),
})
type AddLineFormInput = z.input<typeof addLineSchema>
type AddLineFormValues = z.output<typeof addLineSchema>

function AddLineForm({ orderId }: { orderId: string }) {
  const { data: products } = useProductsList({ isActive: true })
  const addLine = useAddSalesOrderLine(orderId)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AddLineFormInput, unknown, AddLineFormValues>({
    resolver: zodResolver(addLineSchema),
    defaultValues: { quantity: 1 },
  })

  const onSubmit = handleSubmit(async (values) => {
    await addLine.mutateAsync({ productId: values.productId, quantity: values.quantity })
    reset({ productId: '', quantity: 1 })
  })

  return (
    <form onSubmit={onSubmit} className="rounded border border-border bg-surface p-4">
      <h2 className="mb-3 text-sm font-semibold text-text-primary">Adicionar Item</h2>
      <label className="mb-3 flex flex-col gap-1">
        <span className="text-xs font-semibold text-text-secondary">Produto</span>
        <select {...register('productId')} className="input" defaultValue="">
          <option value="" disabled>
            Selecione...
          </option>
          {products?.map((product) => (
            <option key={product.id} value={product.id}>
              {product.name} — {formatBRL(product.price)}
            </option>
          ))}
        </select>
        {errors.productId && <span className="text-[11px] font-medium text-danger">{errors.productId.message}</span>}
      </label>
      <label className="mb-3 flex flex-col gap-1">
        <span className="text-xs font-semibold text-text-secondary">Quantidade</span>
        <input type="number" step="any" min="0" {...register('quantity')} className="input" />
        {errors.quantity && <span className="text-[11px] font-medium text-danger">{errors.quantity.message}</span>}
      </label>
      {addLine.isError && <p className="mb-3 text-[11px] font-medium text-danger">{addLine.error.message}</p>}
      <Button type="submit" disabled={addLine.isPending} className="w-full">
        {addLine.isPending ? 'Adicionando...' : 'Adicionar Item'}
      </Button>
    </form>
  )
}

const approveSchema = z.object({
  installmentCount: z.coerce.number().int().min(1, 'Mínimo de 1 parcela.'),
  firstDueDate: z.string().min(1, 'Informe o primeiro vencimento.'),
  intervalDays: z.coerce.number().int().min(1).default(30),
})
type ApproveFormInput = z.input<typeof approveSchema>
type ApproveFormValues = z.output<typeof approveSchema>

function ApproveOrderForm({ orderId, disabled }: { orderId: string; disabled: boolean }) {
  const approveOrder = useApproveSalesOrder(orderId)
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ApproveFormInput, unknown, ApproveFormValues>({
    resolver: zodResolver(approveSchema),
    defaultValues: { installmentCount: 1, intervalDays: 30 },
  })

  const onSubmit = handleSubmit(async (values) => {
    await approveOrder.mutateAsync(values)
  })

  return (
    <form onSubmit={onSubmit} className="rounded border border-border bg-surface p-4">
      <h2 className="mb-3 text-sm font-semibold text-text-primary">Aprovar Pedido</h2>
      <p className="mb-3 text-xs text-text-muted">
        Gera as parcelas de recebimento (Contas a Receber) no momento da aprovação.
      </p>
      <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">Parcelas</span>
          <input type="number" min="1" {...register('installmentCount')} className="input" />
          {errors.installmentCount && (
            <span className="text-[11px] font-medium text-danger">{errors.installmentCount.message}</span>
          )}
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">Intervalo (dias)</span>
          <input type="number" min="1" {...register('intervalDays')} className="input" />
        </label>
      </div>
      <label className="mb-3 flex flex-col gap-1">
        <span className="text-xs font-semibold text-text-secondary">Primeiro Vencimento</span>
        <input type="date" {...register('firstDueDate')} className="input" />
        {errors.firstDueDate && (
          <span className="text-[11px] font-medium text-danger">{errors.firstDueDate.message}</span>
        )}
      </label>
      {approveOrder.isError && (
        <p className="mb-3 text-[11px] font-medium text-danger">{approveOrder.error.message}</p>
      )}
      <Button type="submit" disabled={approveOrder.isPending || disabled} className="w-full">
        {disabled ? 'Adicione ao menos 1 item' : approveOrder.isPending ? 'Aprovando...' : 'Aprovar Pedido'}
      </Button>
    </form>
  )
}

function CancelOrderButton({ orderId }: { orderId: string }) {
  const cancelOrder = useCancelSalesOrder(orderId)
  return (
    <div className="flex flex-col items-end gap-1">
      <Button variant="destructive" onClick={() => cancelOrder.mutate()} disabled={cancelOrder.isPending}>
        {cancelOrder.isPending ? 'Cancelando...' : 'Cancelar Pedido'}
      </Button>
      {cancelOrder.isError && (
        <p className="text-[11px] font-medium text-danger">{(cancelOrder.error as ApiError).message}</p>
      )}
    </div>
  )
}
