import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { TruncationNotice } from './TruncationNotice'

describe('TruncationNotice', () => {
  it('renders nothing when the count is below the page size', () => {
    const { container } = render(<TruncationNotice count={10} pageSize={25} />)
    expect(container).toBeEmptyDOMElement()
  })

  it('warns that results may be truncated when the count hits the page size', () => {
    render(<TruncationNotice count={25} pageSize={25} />)
    expect(screen.getByText(/Mostrando os primeiros 25 resultados/)).toBeInTheDocument()
  })
})
