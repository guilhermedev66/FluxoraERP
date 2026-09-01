import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/shared/api/client'
import type { ApiError } from '@/shared/api/errors'
import type { CreateProductRequest, ProductDto, ProductListFilters } from './types'

export function useProductsList(filters: ProductListFilters) {
  return useQuery<ProductDto[], ApiError>({
    queryKey: ['products', 'list', filters],
    queryFn: () => api.get<ProductDto[]>('products', { search: filters.search, isActive: filters.isActive }),
  })
}

export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation<ProductDto, ApiError, CreateProductRequest>({
    mutationFn: (request) => api.post<ProductDto>('products', request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products', 'list'] }),
  })
}

export function useSetProductActive() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, { id: string; active: boolean }>({
    mutationFn: ({ id, active }) => api.postNoContent(`products/${id}/${active ? 'activate' : 'deactivate'}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products', 'list'] }),
  })
}
