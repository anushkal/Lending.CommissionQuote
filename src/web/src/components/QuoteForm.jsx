import { useState } from 'react'
import { generateCommissionQuote } from '../api/commissionQuoteApi.js'
import QuoteResult from './QuoteResult.jsx'
import ErrorBanner from './ErrorBanner.jsx'

const RISK_BANDS = ['Low', 'Medium', 'High']

// States ErrorBanner has a distinct message for (data-model.md). ValidationError gets its own
// field-level UI below, not a banner.
const ERROR_BANNER_STATES = ['AuthFailure', 'VendorError', 'TimeoutError', 'NetworkError']

/**
 * Mirrors the backend's field-level validation (research.md #6 / CommissionQuotesController),
 * so invalid input is caught before any request is sent (FR-003).
 * @returns {Record<string, string>} empty when the input is valid
 */
function validate({ loanAmount, loanTermInMonths, riskBand }) {
  const errors = {}

  const amount = Number(loanAmount)
  if (loanAmount === '' || Number.isNaN(amount) || amount <= 0 || amount > 100_000_000) {
    errors.loanAmount = 'Loan amount must be greater than 0 and no more than 100,000,000.'
  }

  const term = Number(loanTermInMonths)
  if (loanTermInMonths === '' || !Number.isInteger(term) || term < 1 || term > 480) {
    errors.loanTermInMonths = 'Loan term must be a whole number of months between 1 and 480.'
  }

  if (!RISK_BANDS.includes(riskBand)) {
    errors.riskBand = 'Risk band must be one of Low, Medium, or High.'
  }

  return errors
}

/**
 * Loan detail form + "Generate Quote" action (FR-001/FR-002). Renders a distinct ErrorBanner per
 * failure state (FR-006/FR-007) and re-enables the button for retry without losing already-
 * entered field values (SC-004) — since those fields' state is never cleared on failure.
 */
function QuoteForm() {
  const [loanAmount, setLoanAmount] = useState('')
  const [loanTermInMonths, setLoanTermInMonths] = useState('')
  const [riskBand, setRiskBand] = useState(RISK_BANDS[1])
  const [status, setStatus] = useState('Idle')
  const [quote, setQuote] = useState(null)
  const [fieldErrors, setFieldErrors] = useState({})

  const isLoading = status === 'Loading'

  async function handleSubmit(event) {
    event.preventDefault()

    const errors = validate({ loanAmount, loanTermInMonths, riskBand })
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors)
      setStatus('ValidationError')
      return
    }

    setFieldErrors({})
    setStatus('Loading')

    const result = await generateCommissionQuote({
      loanAmount: Number(loanAmount),
      loanTermInMonths: Number(loanTermInMonths),
      riskBand,
    })

    if (result.state === 'Success') {
      setQuote(result.quote)
    }
    setStatus(result.state)
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <label>
        Loan amount
        <input
          type="number"
          value={loanAmount}
          onChange={(event) => setLoanAmount(event.target.value)}
        />
      </label>
      {fieldErrors.loanAmount && <p role="alert">{fieldErrors.loanAmount}</p>}
      <label>
        Loan term (months)
        <input
          type="number"
          value={loanTermInMonths}
          onChange={(event) => setLoanTermInMonths(event.target.value)}
        />
      </label>
      {fieldErrors.loanTermInMonths && <p role="alert">{fieldErrors.loanTermInMonths}</p>}
      <label>
        Risk band
        <select value={riskBand} onChange={(event) => setRiskBand(event.target.value)}>
          {RISK_BANDS.map((band) => (
            <option key={band} value={band}>
              {band}
            </option>
          ))}
        </select>
      </label>
      {fieldErrors.riskBand && <p role="alert">{fieldErrors.riskBand}</p>}
      <button type="submit" disabled={isLoading}>
        Generate Quote
      </button>
      {isLoading && <p role="status">Generating quote…</p>}
      {status === 'Success' && quote && <QuoteResult quote={quote} />}
      {ERROR_BANNER_STATES.includes(status) && <ErrorBanner state={status} />}
    </form>
  )
}

export default QuoteForm
