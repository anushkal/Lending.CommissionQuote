/**
 * Displays a successfully generated commission quote (FR-005).
 * @param {{ quote: import('../types.js').CommissionQuoteResponse }} props
 */
function QuoteResult({ quote }) {
  return (
    <div data-testid="quote-result">
      <dl>
        <dt>Quote ID</dt>
        <dd>{quote.quoteId}</dd>
        <dt>Commission Rate</dt>
        <dd>{quote.commissionRate}</dd>
        <dt>Total Commission</dt>
        <dd>{quote.totalCommission}</dd>
      </dl>
    </div>
  )
}

export default QuoteResult
