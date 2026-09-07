/**
 * Canonical, user-visible text for each failure QuoteRequestState (data-model.md). The
 * AuthFailure/VendorError/TimeoutError text matches contracts/commission-quotes-api.md exactly;
 * NetworkError has no server response to quote from, so its wording is our own (Constitution
 * Principle III: every failure mode gets a distinct, specific message, never a generic one).
 */
const MESSAGES = {
  AuthFailure: 'Error connecting to the external commission quote API',
  VendorError: 'The commission quote service returned an error. Please try again.',
  TimeoutError: 'The commission quote service did not respond in time. Please try again.',
  NetworkError: 'Unable to reach the commission quote service. Check your connection and try again.',
}

/**
 * Displays the distinct message for the current failure state (FR-006/FR-012/FR-013).
 * @param {{ state: keyof typeof MESSAGES }} props
 */
function ErrorBanner({ state }) {
  return <p role="alert">{MESSAGES[state]}</p>
}

export default ErrorBanner
