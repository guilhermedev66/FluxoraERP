import { createPartyResource } from '@/shared/api/partyResource'

export const fornecedoresResource = createPartyResource('suppliers')
export type { PartyDto as FornecedorDto } from '@/shared/api/partyResource'
