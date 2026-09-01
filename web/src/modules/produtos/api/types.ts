/** Mirrors Fluxora.Application.Catalog.ProductDto. */
export interface ProductDto {
  id: string
  sku: string
  name: string
  price: number
  category: string | null
  isActive: boolean
  createdAtUtc: string
}

export interface CreateProductRequest {
  sku: string
  name: string
  price: number
  category?: string
}

export interface ProductListFilters {
  search?: string
  isActive?: boolean
}
