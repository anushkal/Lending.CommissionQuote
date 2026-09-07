/**
 * @typedef {Object} GenerateQuoteRequest
 * @property {number} loanAmount
 * @property {number} loanTermInMonths
 * @property {'Low'|'Medium'|'High'} riskBand
 */

/**
 * @typedef {Object} CommissionQuoteResponse
 * @property {string} quoteId
 * @property {number} commissionRate
 * @property {number} totalCommission
 */

/**
 * Mirrors data-model.md's QuoteRequestState. Exactly one is active at a time.
 * @typedef {'Idle'|'Loading'|'Success'|'ValidationError'|'AuthFailure'|'VendorError'|'TimeoutError'|'NetworkError'} QuoteRequestStateType
 */

export {}
