# Contract: Vendor Mock Commission Quote API

Simulates the external vendor per the agreed contract in the spec's Input section. Hosted
inside `CommissionQuote.Service` under the `VendorSimulation` namespace (research.md #3), but
reached only over real HTTP from the backend's vendor client — never called directly, and never
called from the browser (constitution Principle I).

## `POST /vendor/commission-quotes`

### Request headers

| Header | Required | Notes |
|---|---|---|
| `api-key` | yes | Rejected with `401` if missing or not the configured value (FR-009). Checked before the request body is parsed or any commission logic runs (Constitution Principle I). |
| `X-Vendor-Simulate` | no | `success` or `error` — deterministically forces that outcome for this request, bypassing the random failure (research.md #4). Test-only; absent in normal frontend traffic since the frontend never calls this endpoint. |

### Request body

```json
{
  "loanAmount": 250000,
  "loanTermInMonths": 180,
  "riskBand": "Medium"
}
```

### Responses

| Status | Body | When |
|---|---|---|
| `200 OK` | `{ "quoteId": string, "commissionRate": number, "totalCommission": number }` | Valid `api-key`, and either not randomly failed or `X-Vendor-Simulate: success` |
| `401 Unauthorized` | `{ "message": "Missing or invalid api-key" }` | `api-key` header missing or incorrect — no commission is calculated (FR-009) |
| `500 Internal Server Error` | `{ "message": "Simulated vendor failure" }` | Random failure roll (rate from `VendorMock:FailureRate`, default ~15%), or `X-Vendor-Simulate: error` |

### Notes

- The random-failure roll only happens after the `api-key` check passes — an unauthenticated
  request always gets `401`, never a simulated `500`, so the two failure causes stay
  distinguishable to the caller (`CommissionQuote.Service`), which is what lets it in turn
  present `AuthFailure` vs. `VendorError` to the staff member.
- This endpoint is internal to the solution's HTTP surface (constitution: "real HTTP boundary,
  not an in-process method call") but is not intended for direct external/frontend use; it has
  no CORS policy enabling browser callers.
