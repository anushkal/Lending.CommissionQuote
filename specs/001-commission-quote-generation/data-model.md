# Phase 1 Data Model: Commission Quote Generation

No persistence is introduced for this feature (constitution: "Persistence: none by default").
Everything below describes request/response shapes and transient client-side state, not stored
records.

## LoanDetailInput

The data a staff member submits to request a quote (spec: Key Entities).

| Field | Type | Rules |
|---|---|---|
| `loanAmount` | number | Required. > 0 and <= 100,000,000 (research.md #6). |
| `loanTermInMonths` | integer | Required. Whole number, >= 1 and <= 480 (research.md #6). |
| `riskBand` | enum string | Required. One of `Low`, `Medium`, `High` (research.md #6 / spec Assumptions). |

Validation failures for any field are surfaced together (FR-003) and block submission — no
request reaches the Commission Quote API contract below until all three pass.

## CommissionQuote

The result of a successful quote request (spec: Key Entities), returned 1:1 for one
`LoanDetailInput` submission.

| Field | Type | Notes |
|---|---|---|
| `quoteId` | string | Vendor-assigned identifier for this quote. Opaque to this system. |
| `commissionRate` | number | Rate applied to produce `totalCommission`. Vendor-calculated; not recomputed client-side. |
| `totalCommission` | number | The commission amount for the submitted loan. Vendor-calculated. |

Not persisted: a quote exists only for the lifetime of the request/response and the screen
showing it (spec Assumptions: "saving, listing, or comparing historical quotes is out of
scope").

## VendorQuoteRequest / VendorQuoteResponse

The same shape as `LoanDetailInput` / `CommissionQuote`, carried over the internal HTTP call
from `CommissionQuote.Service` to its `VendorSimulation` endpoint (research.md #3). Kept as a
distinct pair of types (not reused directly) per the constitution's DTO-naming rule, so a
future divergence between "what our API accepts" and "what the vendor contract requires" doesn't
force a breaking change to both at once.

## QuoteRequestState (frontend, transient UI state)

Not a persisted entity — the finite set of states the quote form/result area can be in, per
FR-004/FR-006/FR-012/FR-013 and research.md #5. Exactly one is active at a time.

| State | Entered when | User sees |
|---|---|---|
| `Idle` | Initial load, or after a result is cleared | Empty form, "Generate Quote" enabled |
| `Loading` | "Generate Quote" selected with valid input | Loading indicator; "Generate Quote" disabled (FR-004) |
| `Success` | Backend returns a `CommissionQuote` | `quoteId`, `commissionRate`, `totalCommission` (FR-005) |
| `ValidationError` | Client-side validation fails before submit | Field-level messages (FR-003); no request sent |
| `AuthFailure` | Backend's call to the vendor is rejected for a missing/invalid credential | "Error connecting to the external commission quote API" (FR-012) |
| `VendorError` | Backend's call to the vendor returns its simulated random failure | Distinct vendor-error message (FR-006), separate from `AuthFailure` and `TimeoutError` |
| `TimeoutError` | Backend's call to the vendor does not respond within 10s | Distinct timeout message (FR-013) |
| `NetworkError` | The browser cannot reach `CommissionQuote.Service` at all | Distinct network-error message (Constitution Principle III) |

Every non-`Idle`, non-`Loading`, non-`Success` state allows returning to `Loading` via retry
(FR-007, SC-004) without losing the already-valid form input.
