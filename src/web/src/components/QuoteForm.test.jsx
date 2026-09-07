import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import QuoteForm from './QuoteForm.jsx'
import { generateCommissionQuote } from '../api/commissionQuoteApi.js'

vi.mock('../api/commissionQuoteApi.js', () => ({
  generateCommissionQuote: vi.fn(),
}))

describe('QuoteForm', () => {
  beforeEach(() => {
    generateCommissionQuote.mockReset()
  })

  it('shows a loading indicator, disables the button, then renders the quote on success', async () => {
    let resolveRequest
    generateCommissionQuote.mockReturnValue(
      new Promise((resolve) => {
        resolveRequest = resolve
      }),
    )

    render(<QuoteForm />)

    fireEvent.change(screen.getByLabelText(/loan amount/i), { target: { value: '250000' } })
    fireEvent.change(screen.getByLabelText(/loan term/i), { target: { value: '180' } })
    fireEvent.change(screen.getByLabelText(/risk band/i), { target: { value: 'Medium' } })
    fireEvent.click(screen.getByRole('button', { name: /generate quote/i }))

    expect(screen.getByRole('status')).toHaveTextContent(/generating quote/i)
    expect(screen.getByRole('button', { name: /generate quote/i })).toBeDisabled()

    resolveRequest({
      state: 'Success',
      quote: { quoteId: 'q-123', commissionRate: 0.02, totalCommission: 5000 },
    })

    await waitFor(() => expect(screen.getByTestId('quote-result')).toBeInTheDocument())
    expect(screen.getByText('q-123')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /generate quote/i })).not.toBeDisabled()
  })

  it('shows field-level validation messages for empty input and does not call the API', () => {
    render(<QuoteForm />)

    fireEvent.click(screen.getByRole('button', { name: /generate quote/i }))

    expect(screen.getAllByRole('alert').length).toBeGreaterThanOrEqual(2)
    expect(generateCommissionQuote).not.toHaveBeenCalled()
  })

  it('shows field-level validation messages for out-of-range input and does not call the API', () => {
    render(<QuoteForm />)

    fireEvent.change(screen.getByLabelText(/loan amount/i), { target: { value: '-100' } })
    fireEvent.change(screen.getByLabelText(/loan term/i), { target: { value: '9999' } })
    fireEvent.change(screen.getByLabelText(/risk band/i), { target: { value: 'Medium' } })
    fireEvent.click(screen.getByRole('button', { name: /generate quote/i }))

    expect(screen.getByText(/loan amount must be greater than 0/i)).toBeInTheDocument()
    expect(screen.getByText(/loan term must be a whole number/i)).toBeInTheDocument()
    expect(generateCommissionQuote).not.toHaveBeenCalled()
  })
})
