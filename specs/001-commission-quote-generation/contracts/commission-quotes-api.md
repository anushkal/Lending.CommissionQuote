# Contract: Commission Quotes API (staff-facing)

Exposed by `CommissionQuote.Service`. This is the only endpoint the React frontend calls
(constitution: the frontend "talks exclusively to the backend").

## `POST /api/commission-quotes`

### Request

No credential required from the browser — this endpoint is reached by staff already
authenticated into the Lending Platform by an existing mechanism (spec Assumptions); it is not
the vendor-facing credential described below.

```json
{
  "loanAmount": 250000,
  "loanTermInMonths": 180,
  "riskBand": "Medium"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `loanAmount` | number | yes | > 0, <= 100000000 |
| `loanTermInMonths` | integer | yes | >= 1, <= 480 |
| `riskBand` | string | yes | one of `Low`, `Medium`, `High` |

### Responses

| Status | Body | `QuoteRequestState` | When |
|---|---|---|---|
| `200 OK` | `{ "quoteId": string, "commissionRate": number, "totalCommission": number }` | `Success` | Vendor returned a quote |
| `400 Bad Request` | `{ "errors": { "<field>": string[] } }` | `ValidationError` | One or more fields fail validation (FR-003) — vendor is never called |
| `500 Internal Server Error` | `{ "message": "Error connecting to the external commission quote API" }` | `AuthFailure` | Backend's own call to the vendor was rejected for a missing/invalid credential (FR-012) — an internal misconfiguration on our side, not the vendor being unhealthy, so it is not reported as a gateway/upstream status |
| `502 Bad Gateway` | `{ "message": "The commission quote service returned an error. Please try again." }` | `VendorError` | Vendor responded with its simulated random failure (FR-010) |
| `504 Gateway Timeout` | `{ "message": "The commission quote service did not respond in time. Please try again." }` | `TimeoutError` | No vendor response within 10s (FR-013) |

A `NetworkError` state (browser cannot reach this endpoint at all) has no server-side response
by definition — the frontend detects it client-side when the request itself fails.

### Notes

- `400` is the only outcome that does not attempt to reach the vendor — validation happens
  first, per FR-003.
- `AuthFailure`, `VendorError`, and `TimeoutError` each get their own distinct status
  (`500`/`502`/`504`) as well as their own message, so the frontend can tell them apart from the
  status code alone without needing to inspect the message body.
