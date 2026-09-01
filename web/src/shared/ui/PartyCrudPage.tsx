import { zodResolver } from '@hookform/resolvers/zod'
import type { LucideIcon } from 'lucide-react'
import { Plus } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import type { ApiError } from '@/shared/api/errors'
import type { createPartyResource } from '@/shared/api/partyResource'
import { PermissionGate } from '@/shared/auth/PermissionGate'
import type { Role } from '@/shared/auth/roles'
import { Button } from './Button'
import { EmptyState } from './EmptyState'
import { ErrorState } from './ErrorState'
import { StatusBadge } from './StatusBadge'
import { TableSkeleton } from './Skeleton'

const partyFormSchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório.'),
  document: z.string().min(11, 'Informe um CPF ou CNPJ válido.'),
  email: z.string().email('E-mail inválido.').optional().or(z.literal('')),
  phone: z.string().optional(),
})

type PartyFormValues = z.infer<typeof partyFormSchema>

interface PartyCrudPageProps {
  resource: ReturnType<typeof createPartyResource>
  title: string
  subtitle: string
  entityLabel: string
  icon: LucideIcon
  createRoles: readonly Role[]
}

/** Generic list+create screen for Customer/Supplier-shaped resources (shared/api/partyResource.ts). */
export function PartyCrudPage({ resource, title, subtitle, entityLabel, icon, createRoles }: PartyCrudPageProps) {
  const [search, setSearch] = useState('')
  const [isFormOpen, setIsFormOpen] = useState(false)

  const { data: parties, isLoading, isError, error, refetch } = resource.useList({ search })

  return (
    <div className="mx-auto max-w-5xl px-6 py-8">
      <header className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary">{title}</h1>
          <p className="text-sm text-text-muted">{subtitle}</p>
        </div>
        <PermissionGate roles={createRoles}>
          <Button onClick={() => setIsFormOpen((open) => !open)}>
            <Plus className="h-4 w-4" />
            Novo {entityLabel}
          </Button>
        </PermissionGate>
      </header>

      {isFormOpen && (
        <PartyForm resource={resource} entityLabel={entityLabel} onDone={() => setIsFormOpen(false)} />
      )}

      <div className="mb-4">
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={`Buscar ${entityLabel.toLowerCase()} por nome ou documento...`}
          className="input h-9 w-72"
          aria-label={`Buscar ${entityLabel.toLowerCase()} por nome ou documento`}
        />
      </div>

      <div aria-live="polite" className="sr-only">
        {!isLoading && !isError && parties && (
          search
            ? `${parties.length} resultado${parties.length === 1 ? '' : 's'} para "${search}".`
            : `${parties.length} registro${parties.length === 1 ? '' : 's'} de ${entityLabel.toLowerCase()}.`
        )}
      </div>

      {isLoading && <TableSkeleton columns={5} />}

      {isError && <ErrorState message={error?.message} onRetry={() => refetch()} />}

      {!isLoading && !isError && parties && parties.length === 0 && (
        <EmptyState
          icon={icon}
          title={`Nenhum ${entityLabel.toLowerCase()} encontrado`}
          description={search ? 'Nenhum resultado para esta busca.' : `Cadastre o primeiro ${entityLabel.toLowerCase()} para começar.`}
          action={
            search ? (
              <Button variant="secondary" onClick={() => setSearch('')}>
                Limpar busca
              </Button>
            ) : (
              <Button onClick={() => setIsFormOpen(true)}>
                <Plus className="h-4 w-4" />
                Novo {entityLabel}
              </Button>
            )
          }
        />
      )}

      {!isLoading && !isError && parties && parties.length > 0 && (
        <div className="overflow-x-auto rounded border border-border">
          <table className="w-full text-left text-[13px]">
            <thead className="bg-surface-muted text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              <tr>
                <th className="px-3 py-2.5">Nome</th>
                <th className="px-3 py-2.5 font-mono">Documento</th>
                <th className="px-3 py-2.5">Contato</th>
                <th className="w-[110px] px-3 py-2.5 text-right">Status</th>
              </tr>
            </thead>
            <tbody>
              {parties.map((party) => (
                <tr key={party.id} className="h-[42px] border-t border-border">
                  <td className="px-3 py-2 font-medium text-text-primary">{party.name}</td>
                  <td className="px-3 py-2 font-mono text-xs text-text-muted">{party.document}</td>
                  <td className="px-3 py-2 text-text-secondary">{party.email ?? party.phone ?? '—'}</td>
                  <td className="px-3 py-2 text-right">
                    <StatusBadge
                      variant={party.isActive ? 'success' : 'neutral'}
                      label={party.isActive ? 'Ativo' : 'Inativo'}
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

function PartyForm({
  resource,
  entityLabel,
  onDone,
}: {
  resource: ReturnType<typeof createPartyResource>
  entityLabel: string
  onDone: () => void
}) {
  const createParty = resource.useCreate()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<PartyFormValues>({ resolver: zodResolver(partyFormSchema) })

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createParty.mutateAsync({
        name: values.name,
        document: values.document,
        email: values.email || undefined,
        phone: values.phone || undefined,
      })
      onDone()
    } catch (err) {
      const apiError = err as ApiError
      if (apiError.kind === 'validation' && apiError.fieldErrors) {
        for (const [field, messages] of Object.entries(apiError.fieldErrors)) {
          const key = field.toLowerCase() as keyof PartyFormValues
          setError(key, { message: messages[0] })
        }
      } else if (apiError.kind === 'conflict') {
        setError('document', { message: apiError.message })
      }
    }
  })

  return (
    <form onSubmit={onSubmit} className="mb-6 rounded border border-border bg-surface p-4">
      <div className="grid grid-cols-2 gap-3">
        <Field label="Nome" error={errors.name?.message}>
          <input {...register('name')} className="input" />
        </Field>
        <Field label="CPF/CNPJ" error={errors.document?.message}>
          <input {...register('document')} className="input font-mono" />
        </Field>
        <Field label="E-mail" error={errors.email?.message}>
          <input {...register('email')} className="input" />
        </Field>
        <Field label="Telefone" error={errors.phone?.message}>
          <input {...register('phone')} className="input" />
        </Field>
      </div>
      <div className="mt-4 flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onDone}>
          Cancelar
        </Button>
        <Button type="submit" disabled={createParty.isPending}>
          {createParty.isPending ? 'Salvando...' : `Salvar ${entityLabel}`}
        </Button>
      </div>
    </form>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return (
    <label className="flex flex-col gap-1">
      <span className="text-xs font-semibold text-text-secondary">{label}</span>
      {children}
      {error && <span className="text-[11px] font-medium text-danger">{error}</span>}
    </label>
  )
}
