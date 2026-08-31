import { useMutation, useQuery, useQueryClient, type UseQueryOptions } from '@tanstack/react-query'
import { api } from './client'
import type { ApiError } from './errors'

/** Shape shared by Customer and Supplier (Fluxora.Application.{Customers,Suppliers}.*Dto). */
export interface PartyDto {
  id: string
  name: string
  document: string
  email: string | null
  phone: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface CreatePartyRequest {
  name: string
  document: string
  email?: string
  phone?: string
}

export interface UpdatePartyRequest {
  name: string
  email?: string
  phone?: string
}

export interface PartyListFilters {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

/**
 * Builds the query/mutation hooks for a CRUD resource shaped like Customers or Suppliers.
 * Both modules call this independently — it's shared infrastructure, not a cross-module dependency.
 */
export function createPartyResource(resourcePath: 'customers' | 'suppliers') {
  const queryKey = {
    list: (filters: PartyListFilters) => [resourcePath, 'list', filters] as const,
    detail: (id: string) => [resourcePath, 'detail', id] as const,
  }

  function useList(
    filters: PartyListFilters,
    options?: Pick<UseQueryOptions<PartyDto[], ApiError>, 'enabled'>,
  ) {
    return useQuery<PartyDto[], ApiError>({
      queryKey: queryKey.list(filters),
      queryFn: () =>
        api.get<PartyDto[]>(resourcePath, {
          search: filters.search,
          isActive: filters.isActive,
          page: filters.page ?? 1,
          pageSize: filters.pageSize ?? 25,
        }),
      ...options,
    })
  }

  function useCreate() {
    const queryClient = useQueryClient()
    return useMutation<PartyDto, ApiError, CreatePartyRequest>({
      mutationFn: (request) => api.post<PartyDto>(resourcePath, request),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [resourcePath, 'list'] })
      },
    })
  }

  function useUpdate() {
    const queryClient = useQueryClient()
    return useMutation<PartyDto, ApiError, UpdatePartyRequest & { id: string }>({
      mutationFn: ({ id, ...request }) => api.put<PartyDto>(`${resourcePath}/${id}`, request),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [resourcePath, 'list'] })
      },
    })
  }

  function useSetActive() {
    const queryClient = useQueryClient()
    return useMutation<void, ApiError, { id: string; active: boolean }>({
      mutationFn: ({ id, active }) =>
        api.postNoContent(`${resourcePath}/${id}/${active ? 'activate' : 'deactivate'}`),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: [resourcePath, 'list'] })
      },
    })
  }

  return { queryKey, useList, useCreate, useUpdate, useSetActive }
}
