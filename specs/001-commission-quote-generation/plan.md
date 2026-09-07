# Implementation Plan: Commission Quote Generation

**Branch**: `001-commission-quote-generation` | **Date**: 2026-09-04 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-commission-quote-generation/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Staff members enter loan details (loan amount, loan term, risk band) in a React form and
request a commission quote. The `CommissionQuote.Service` backend validates the input, then
calls a mock vendor Commission Quote API over real HTTP — hosted inside the same backend
project, secured with an `api-key` header, and randomly failing to simulate real-world vendor
instability — and relays the result (or a distinct, specific failure state) back to the
frontend. The vendor credential never reaches the browser.

## Technical Context

**Language/Version**: C# on .NET 10 for the backend; JavaScript for the frontend
(research.md #1, #2)

**Primary Dependencies**: ASP.NET Core Web API (backend, incl. `Microsoft.AspNetCore.Mvc.Testing`
for `WebApplicationFactory`); React + Vite (frontend)

**Storage**: N/A — no persistence for this feature (constitution: "Persistence: none by
default"); quotes exist only for the lifetime of a request/response

**Testing**: xUnit + `WebApplicationFactory` (backend, both `CommissionQuote.Service` and its
`VendorSimulation` endpoints); Vitest + React Testing Library (frontend)

**Target Platform**: Web — ASP.NET Core Web API backend (cross-platform .NET runtime) served to
a browser-based React frontend

**Project Type**: Web application (frontend + backend), monorepo (constitution: `src/*`, one
repo)

**Performance Goals**: No throughput/concurrency target is specified by the spec; the only
timing requirement is user-facing — a quote appears within ~30 seconds of form completion
(spec SC-001), bounded by a 10-second vendor-call timeout (spec FR-013 / research.md #5)

**Constraints**: Vendor `api-key` MUST be attached server-side only (Constitution Principle I);
simulated vendor randomness MUST be deterministically overridable for tests (Constitution
Principle II); every failure mode MUST map to a distinct, user-visible state (Constitution
Principle III)

**Scale/Scope**: Internal staff tool; single quote request/response flow; no defined concurrent-
user target in the spec

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Constraint | Gate | Status |
|---|---|---|
| I. Security Is Non-Negotiable | `api-key` check happens before request parsing/commission logic; frontend never sees or sends the vendor credential | **PASS** — vendor client lives only in `CommissionQuote.Service`; contracts (`contracts/commission-quotes-api.md`) confirm the staff-facing endpoint takes no vendor credential from the browser |
| II. Deterministic Testability | Simulated random vendor failure overridable via header/query/config; tests never depend on true randomness | **PASS** — `X-Vendor-Simulate` header + `VendorMock:FailureRate` config (research.md #4) |
| III. Explicit Error Handling | Every failure mode maps to a distinct, user-visible state; no generic/silent failures | **PASS** — `QuoteRequestState` enumerates 5 distinct failure states plus `Success`/`Loading`/`Idle` (data-model.md) |
| IV. Comprehension Over Generation | N/A at plan time — applies during implementation review, not a design gate | **N/A** (tracked, not a blocker) |
| Technical Constraints: monorepo layout, naming | `src/CommissionQuote.Service`, `src/CommissionQuote.Service.Tests`, `src/web`; `CommissionQuotesController`; `GenerateQuoteRequest`/`CommissionQuoteResponse`; `ICommissionCalculator` | **PASS** — see Project Structure below |
| Technical Constraints: vendor mock is a real HTTP boundary | Mock reached via `HttpClient`, not a direct method call | **PASS** — research.md #3 |
| Technical Constraints: persistence none by default | No storage introduced | **PASS** — no requirement in spec demands it |

No violations — Complexity Tracking is intentionally empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-commission-quote-generation/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── commission-quotes-api.md
│   └── vendor-mock-api.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
CommissionQuote.sln

src/
├── CommissionQuote.Service/
│   ├── CommissionQuote.Service.csproj
│   ├── Program.cs                               # also: public partial Program, for WebApplicationFactory<Program>
│   ├── Controllers/
│   │   └── CommissionQuotesController.cs        # POST /api/commission-quotes (staff-facing)
│   ├── Contracts/
│   │   ├── GenerateQuoteRequest.cs
│   │   └── CommissionQuoteResponse.cs
│   ├── Services/
│   │   ├── IVendorQuoteClient.cs
│   │   ├── VendorQuoteClient.cs                 # calls VendorSimulation over HttpClient
│   │   └── VendorQuoteOutcome.cs                # Success/AuthFailure/VendorError/Timeout
│   └── VendorSimulation/
│       ├── VendorCommissionQuotesController.cs  # POST /vendor/commission-quotes (mock vendor)
│       ├── VendorApiKeyAuthorizationFilter.cs   # api-key check before model binding (Principle I)
│       ├── VendorMockOptions.cs                 # ApiKey, FailureRate (bound from appsettings)
│       ├── VendorQuoteRequest.cs
│       └── VendorQuoteResponse.cs
│
├── CommissionQuote.Service.Tests/
│   ├── CommissionQuote.Service.Tests.csproj
│   ├── CommissionQuoteWebApplicationFactory.cs  # redirects the "VendorMock" client to the TestServer
│   ├── CommissionQuotesControllerTests.cs       # covers contracts/commission-quotes-api.md
│   └── VendorSimulationTests.cs                 # covers contracts/vendor-mock-api.md
│
└── web/
    ├── package.json
    └── src/
        ├── components/
        │   ├── QuoteForm.jsx
        │   ├── QuoteForm.test.jsx
        │   ├── QuoteResult.jsx
        │   ├── QuoteResult.test.jsx
        │   ├── ErrorBanner.jsx
        │   └── ErrorBanner.test.jsx
        ├── api/
        │   └── commissionQuoteApi.js            # single wrapper around POST /api/commission-quotes
        └── types.js                             # mirrors GenerateQuoteRequest / CommissionQuoteResponse (JSDoc typedefs)
```

**Structure Decision**: Web application (frontend + backend) per the constitution's monorepo
layout — `src/CommissionQuote.Service` (API + vendor mock, one project) and `src/web` (React
frontend), not the template's generic `backend/`/`frontend/` split. The vendor mock stays inside
`CommissionQuote.Service` under `VendorSimulation` (constitution default), reached over real
HTTP rather than split into a separate project, since no specific reason to expose that boundary
has come up (research.md #3).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No violations — this section is intentionally empty.*
