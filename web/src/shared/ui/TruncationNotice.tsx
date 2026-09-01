/** Every list endpoint pages at 25 by default and returns a flat array with no total count, so
 *  the frontend has no way to know if more results exist beyond the current page. A full result
 *  set of exactly this size is the only honest signal available. */
export const DEFAULT_LIST_PAGE_SIZE = 25

interface TruncationNoticeProps {
  count: number
  pageSize?: number
}

/** Warns that a list may be truncated instead of silently hiding results past the page cap. */
export function TruncationNotice({ count, pageSize = DEFAULT_LIST_PAGE_SIZE }: TruncationNoticeProps) {
  if (count < pageSize) return null
  return (
    <p className="mt-2 text-xs text-text-muted">
      Mostrando os primeiros {count} resultados. Refine a busca para ver os demais itens.
    </p>
  )
}
