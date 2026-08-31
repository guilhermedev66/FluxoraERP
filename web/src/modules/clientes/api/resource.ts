import { createPartyResource } from '@/shared/api/partyResource'

export const clientesResource = createPartyResource('customers')
export type { PartyDto as ClienteDto, CreatePartyRequest, UpdatePartyRequest } from '@/shared/api/partyResource'
