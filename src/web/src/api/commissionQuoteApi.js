/**
 * The single wrapper around POST /api/commission-quotes (contracts/commission-quotes-api.md).
 * Returns a result shaped by QuoteRequestState (see types.js), distinguishing every failure
 * mode the contract defines (Constitution Principle III) rather than a single generic error.
 *
 * @param {import('../types.js').GenerateQuoteRequest} request
 * @returns {Promise<
 *   | {state: 'Success', quote: import('../types.js').CommissionQuoteResponse}
 *   | {state: 'ValidationError', errors: Record<string, string[]>}
 *   | {state: 'AuthFailure' | 'VendorError' | 'TimeoutError' | 'NetworkError', message: string}
 * >}
 */
export async function generateCommissionQuote(request) {
  let response
  try {
    response = await fetch('/api/commission-quotes', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    })
  } catch {
    return {
      state: 'NetworkError',
      message: 'Unable to reach the commission quote service. Check your connection and try again.',
    }
  }

  if (response.ok) {
    const quote = await response.json()
    return { state: 'Success', quote }
  }

  if (response.status === 400) {
    const body = await response.json()
    return { state: 'ValidationError', errors: body.errors }
  }

  const body = await response.json()

  if (response.status === 504) {
    return { state: 'TimeoutError', message: body.message }
  }

  if (response.status === 500) {
    return { state: 'AuthFailure', message: body.message }
  }

  // 502 (contracts/commission-quotes-api.md): the vendor's own simulated failure, relayed as-is.
  return { state: 'VendorError', message: body.message }
}
