# Quickstart: Commission Quote Generation

Validates the feature end-to-end once implementation is complete. See
[data-model.md](data-model.md) for field/state definitions and
[contracts/](contracts/) for exact request/response shapes.

## Prerequisites

- .NET 10 SDK
- Node.js (LTS) with npm
- Repository built at `CommissionQuote.sln` (backend) and `src/web` (frontend)

## Setup

```bash
# Backend
dotnet restore CommissionQuote.sln
dotnet build CommissionQuote.sln

# Frontend
cd src/web
npm install
```

## Run

```bash
# Terminal 1 — backend (serves both /api and /vendor endpoints)
dotnet run --project src/CommissionQuote.Service

# Terminal 2 — frontend
cd src/web
npm run dev
```

Open the frontend's dev URL in a browser.

## Validation scenarios

1. **Happy path (User Story 1)**
   Enter a valid loan amount, loan term, and risk band; select "Generate Quote".
   Expect: a loading state, then `quoteId`, `commissionRate`, and `totalCommission` displayed.
   Repeat a few times — since the vendor mock fails ~15% of requests, expect an occasional
   failure state instead; retry until a success is observed.

2. **Simulated vendor failure (User Story 2)**
   Call the vendor mock directly with `X-Vendor-Simulate: error` (see
   [contracts/vendor-mock-api.md](contracts/vendor-mock-api.md)), or simply retry the form
   until the ~15% random failure occurs.
   Expect: a distinct on-screen message for a vendor error (not the auth-failure or timeout
   text), and the "Generate Quote" button re-enabled so the staff member can retry.

3. **Missing/invalid vendor credential (FR-012)**
   Temporarily misconfigure the backend's vendor `api-key` (or call
   `POST /vendor/commission-quotes` directly without an `api-key` header).
   Expect: the frontend shows exactly "Error connecting to the external commission quote API",
   distinct from the vendor-error and timeout messages.

4. **Timeout (FR-013)**
   Introduce an artificial delay in the vendor mock beyond 10 seconds (or use
   `X-Vendor-Simulate` plumbing / a debug delay flag if implemented for this purpose).
   Expect: a distinct timeout message after ~10 seconds, not a stuck loading spinner.

5. **Client-side validation (User Story 3)**
   Leave a required field empty, or enter a loan amount of `0` or a loan term of `-5`.
   Expect: field-level validation messages and no network request sent (verify via browser dev
   tools that `POST /api/commission-quotes` was not called).

6. **No duplicate submissions (FR-004)**
   Click "Generate Quote" and immediately try clicking it again while loading.
   Expect: the button is disabled during the request; only one request is sent.

7. **Credential never reaches the browser (Constitution Principle I / FR-011)**
   With browser dev tools open to the Network tab, generate a quote and inspect the request to
   `POST /api/commission-quotes`.
   Expect: no `api-key` header or value appears anywhere in the request the browser sends —
   the vendor credential is attached only inside `CommissionQuote.Service`.

## Automated checks

- Backend: `dotnet test CommissionQuote.sln` — unit tests for validation and commission
  handling, plus `WebApplicationFactory`-based integration tests covering every response in
  [contracts/commission-quotes-api.md](contracts/commission-quotes-api.md) and
  [contracts/vendor-mock-api.md](contracts/vendor-mock-api.md), using `X-Vendor-Simulate` to
  force each outcome deterministically (Constitution Principle II).
- Frontend: `npm test` in `src/web` — Vitest + React Testing Library coverage of each
  `QuoteRequestState` in [data-model.md](data-model.md).
