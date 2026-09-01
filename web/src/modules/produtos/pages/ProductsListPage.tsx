import { zodResolver } from '@hookform/resolvers/zod'
import { Package, Plus } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import type { ApiError } from '@/shared/api/errors'
import { Button } from '@/shared/ui/Button'
import { CurrencyInput } from '@/shared/ui/CurrencyInput'
import { EmptyState } from '@/shared/ui/EmptyState'
import { ErrorState } from '@/shared/ui/ErrorState'
import { StatusBadge } from '@/shared/ui/StatusBadge'
import { TableSkeleton } from '@/shared/ui/Skeleton'
import { formatBRL } from '@/shared/lib/formatters'
import { useCreateProduct, useProductsList } from '../api/queries'

const productFormSchema = z.object({
  sku: z.string().min(1, 'SKU é obrigatório.'),
  name: z.string().min(1, 'Nome é obrigatório.'),
  price: z.number().min(0.01, 'Informe um preço.'),
  category: z.string().optional(),
})
type ProductFormValues = z.infer<typeof productFormSchema>

export function ProductsListPage() {
  const [search, setSearch] = useState('')
  const [isFormOpen, setIsFormOpen] = useState(false)
  const { data: products, isLoading, isError, error, refetch } = useProductsList({ search })

  return (
    <div>
      <header className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary">Produtos</h1>
          <p className="text-sm text-text-muted">Catálogo usado nos pedidos de venda e compra.</p>
        </div>
        <Button onClick={() => setIsFormOpen((open) => !open)}>
          <Plus className="h-4 w-4" />
          Novo Produto
        </Button>
      </header>

      {isFormOpen && <NewProductForm onDone={() => setIsFormOpen(false)} />}

      <div className="mb-4">
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Buscar por nome ou SKU..."
          className="input h-9 w-72"
          aria-label="Buscar por nome ou SKU"
        />
      </div>

      <div aria-live="polite" className="sr-only">
        {!isLoading && !isError && products && (
          search
            ? `${products.length} produto${products.length === 1 ? '' : 's'} encontrado${products.length === 1 ? '' : 's'} para "${search}".`
            : `${products.length} produto${products.length === 1 ? '' : 's'} no catálogo.`
        )}
      </div>

      {isLoading && <TableSkeleton columns={4} />}
      {isError && <ErrorState message={error?.message} onRetry={() => refetch()} />}

      {!isLoading && !isError && products && products.length === 0 && (
        <EmptyState
          icon={Package}
          title="Nenhum produto encontrado"
          description={search ? 'Nenhum resultado para esta busca.' : 'Cadastre o primeiro produto do catálogo.'}
          action={
            !search && (
              <Button onClick={() => setIsFormOpen(true)}>
                <Plus className="h-4 w-4" />
                Novo Produto
              </Button>
            )
          }
        />
      )}

      {!isLoading && !isError && products && products.length > 0 && (
        <div className="overflow-x-auto rounded border border-border">
          <table className="w-full text-left text-[13px]">
            <thead className="bg-surface-muted text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              <tr>
                <th className="px-3 py-2.5 font-mono">SKU</th>
                <th className="px-3 py-2.5">Nome</th>
                <th className="px-3 py-2.5">Categoria</th>
                <th className="px-3 py-2.5 text-right">Preço</th>
                <th className="w-[110px] px-3 py-2.5 text-right">Status</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.id} className="h-[42px] border-t border-border">
                  <td className="px-3 py-2 font-mono text-xs text-text-muted">{product.sku}</td>
                  <td className="px-3 py-2 font-medium text-text-primary">{product.name}</td>
                  <td className="px-3 py-2 text-text-secondary">{product.category ?? '—'}</td>
                  <td className="px-3 py-2 text-right font-mono tabular-nums">{formatBRL(product.price)}</td>
                  <td className="px-3 py-2 text-right">
                    <StatusBadge
                      variant={product.isActive ? 'success' : 'neutral'}
                      label={product.isActive ? 'Ativo' : 'Inativo'}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function NewProductForm({ onDone }: { onDone: () => void }) {
  const createProduct = useCreateProduct()
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<ProductFormValues>({ resolver: zodResolver(productFormSchema) })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createProduct.mutateAsync({
        sku: values.sku,
        name: values.name,
        price: values.price,
        category: values.category || undefined,
      })
      onDone()
    } catch (err) {
      const apiError = err as ApiError
      if (apiError.kind === 'validation' && apiError.fieldErrors) {
        for (const [field, messages] of Object.entries(apiError.fieldErrors)) {
          setError(field.toLowerCase() as keyof ProductFormValues, { message: messages[0] })
        }
      } else if (apiError.kind === 'conflict') {
        setError('sku', { message: apiError.message })
      }
    }
  })

  return (
    <form onSubmit={onSubmit} className="mb-6 rounded border border-border bg-surface p-4">
      <div className="grid grid-cols-2 gap-3">
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">SKU</span>
          <input {...register('sku')} className="input font-mono" />
          {errors.sku && <span className="text-[11px] font-medium text-danger">{errors.sku.message}</span>}
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">Nome</span>
          <input {...register('name')} className="input" />
          {errors.name && <span className="text-[11px] font-medium text-danger">{errors.name.message}</span>}
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">Preço</span>
          <CurrencyInput name="price" control={control} />
          {errors.price && <span className="text-[11px] font-medium text-danger">{errors.price.message}</span>}
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-text-secondary">Categoria</span>
          <input {...register('category')} className="input" />
        </label>
      </div>
      <div className="mt-4 flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onDone}>
          Cancelar
        </Button>
        <Button type="submit" disabled={createProduct.isPending}>
          {createProduct.isPending ? 'Salvando...' : 'Salvar Produto'}
        </Button>
      </div>
    </form>
  )
}
