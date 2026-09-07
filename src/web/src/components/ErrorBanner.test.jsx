import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import ErrorBanner from './ErrorBanner.jsx'
import QuoteForm from './QuoteForm.jsx'
import { generateCommissionQuote } from '../api/commissionQuoteApi.js'

vi.mock('../api/commissionQuoteApi.js', () => ({
  generateCommissionQuote: vi.fn(),
}))

describe('ErrorBanner', () => {
  it.each([
    ['AuthFailure', 'Error connecting to the external commission quote API'],
    ['VendorError', 'The commission quote service returned an error. Please try again.'],
    ['TimeoutError', 'The commission quote service did not respond in time. Please try again.'],
    ['NetworkError', 'Unable to reach the commission quote service. Check your connection and try again.'],
  ])('renders a distinct message for %s', (state, expectedMessage) => {
    render(<ErrorBanner state={state} />)

    expect(screen.getByRole('alert')).toHaveTextContent(expectedMessage)
  })

  it.each(['AuthFailure', 'VendorError', 'TimeoutError', 'NetworkError'])(
    'renders a different message for %s than the other failure states',
    (state) => {
      const { unmount } = render(<ErrorBanner state={state} />)
      const thisMessage = screen.getByRole('alert').textContent
      unmount()

      const otherStates = ['AuthFailure', 'VendorError', 'TimeoutError', 'NetworkError'].filter((s) => s !== state)
      for (const other of otherStates) {
        const { unmount: unmountOther } = render(<ErrorBanner state={other} />)
        expect(screen.getByRole('alert').textContent).not.toEqual(thisMessage)
        unmountOther()
      }
    },
  )
})

describe('QuoteForm retry after failure', () => {
  beforeEach(() => {
    generateCommissionQuote.mockReset()
  })

  it.each(['AuthFailure', 'VendorError', 'TimeoutError', 'NetworkError'])(
    're-enables "Generate Quote" after a %s',
    async (state) => {
      generateCommissionQuote.mockResolvedValue({ state, message: 'irrelevant for this test' })

      render(<QuoteForm />)

      fireEvent.change(screen.getByLabelText(/loan amount/i), { target: { value: '250000' } })
      fireEvent.change(screen.getByLabelText(/loan term/i), { target: { value: '180' } })
      fireEvent.change(screen.getByLabelText(/risk band/i), { target: { value: 'Medium' } })
      fireEvent.click(screen.getByRole('button', { name: /generate quote/i }))

      await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
      expect(screen.getByRole('button', { name: /generate quote/i })).not.toBeDisabled()

      // Field values are preserved across the failure so the user can retry without retyping (SC-004)
      expect(screen.getByLabelText(/loan amount/i)).toHaveValue(250000)
      expect(screen.getByLabelText(/loan term/i)).toHaveValue(180)
    },
  )
})
