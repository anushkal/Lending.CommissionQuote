---

description: "Task list template for feature implementation"
---

# Tasks: Commission Quote Generation

**Input**: Design documents from `/specs/001-commission-quote-generation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md,
`.specify/memory/constitution.md` (v1.1.0)

**Tests**: Included. The constitution's Technical Constraints mandate xUnit/equivalent coverage
of the edge cases documented in the spec, and Principle II (Deterministic Testability) requires
tests that force every vendor outcome deterministically — so tests are treated as explicitly
requested for this project, not optional.

**Organization**: Tasks are grouped by user story (spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task includes its exact file path(s)

## Path Conventions

Per plan.md's Project Structure — a monorepo, not the template's generic single-project/backend-frontend split:

- Backend: `src/CommissionQuote.Service/` (API + `VendorSimulation`), `src/CommissionQuote.Service.Tests/`
- Frontend: `src/web/src/`
- Solution file: `CommissionQuote.sln` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization per plan.md's Project Structure

- [X] T001 Create solution and project skeleton: `CommissionQuote.sln`; `src/CommissionQuote.Service` (ASP.NET Core Web API, target `net10.0` per actual build machine, research.md #1); `src/CommissionQuote.Service.Tests` (xUnit, referencing `Microsoft.AspNetCore.Mvc.Testing` for `WebApplicationFactory`) — at repository root
- [X] T002 [P] Scaffold frontend app (Vite + React, JavaScript template per constitution v1.1.0; Vitest + React Testing Library + jsdom configured in `vite.config.js`, research.md #2) in `src/web` — hand-authored (npm/Node.js not installed on the build machine; `npm install` still required before `npm run dev`/`npm test` will work)
- [X] T003 Configure backend options and logging: bind a `VendorMock` options section (`ApiKey`, `FailureRate` default ~15%, research.md #4/#6) via `IOptions`, and set default `ILogger<T>` console logging levels — in `src/CommissionQuote.Service/appsettings.json` and `src/CommissionQuote.Service/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The backend round-trip and frontend API wrapper that every user story builds on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 [P] Create `GenerateQuoteRequest` and `CommissionQuoteResponse` DTOs (data-model.md: `LoanDetailInput`/`CommissionQuote`) in `src/CommissionQuote.Service/Contracts/`
- [X] T005 [P] Create `VendorQuoteRequest` and `VendorQuoteResponse` DTOs (data-model.md) in `src/CommissionQuote.Service/VendorSimulation/`
- [X] T006 Implement `VendorCommissionQuotesController` (`POST /vendor/commission-quotes`, contracts/vendor-mock-api.md): checks the `api-key` header before parsing the body or running any commission logic (Constitution Principle I), honors an `X-Vendor-Simulate: success|error` override, otherwise rolls a random failure at `VendorMock:FailureRate` (Constitution Principle II) — in `src/CommissionQuote.Service/VendorSimulation/VendorCommissionQuotesController.cs`. The api-key check is a `VendorApiKeyAuthorizationFilter` (`IAsyncAuthorizationFilter`), since MVC authorization filters run before model binding parses the body — an ordinary in-action check would not satisfy Principle I.
- [X] T007 Implement `IVendorQuoteClient`/`VendorQuoteClient`: a named `HttpClient` that calls the `VendorSimulation` endpoint over real HTTP, attaches the vendor `api-key` server-side only (Constitution Principle I, FR-011), enforces a 10-second timeout (FR-013), and maps the vendor's `200`/`401`/`500`/timeout into an internal outcome — in `src/CommissionQuote.Service/Services/IVendorQuoteClient.cs` and `VendorQuoteClient.cs`. Also added, as necessary supporting infrastructure discovered while implementing this task: `CommissionQuoteWebApplicationFactory` in the Tests project (redirects the "VendorMock" named client's handler to the TestServer, since the vendor is reached over a real self-referencing HTTP call that a real socket can't resolve inside a test host), and forwarding of an incoming `X-Vendor-Simulate` header onto the outbound vendor call, so `POST /api/commission-quotes` itself can be driven deterministically in tests (verified via a throwaway smoke test, then removed — T012/T017 add the permanent tests).
- [X] T008 Implement `CommissionQuotesController` (`POST /api/commission-quotes`, contracts/commission-quotes-api.md): wires `IVendorQuoteClient` and maps its outcome to `200`/`500` (auth failure)/`502` (vendor error)/`504` (timeout) — in `src/CommissionQuote.Service/Controllers/CommissionQuotesController.cs`. (Auth failure was originally `502`; changed to `500` post-implementation since a rejected vendor credential is our own deployment's misconfiguration, not the vendor being unhealthy — see contracts/commission-quotes-api.md's Notes.)
- [X] T009 Add `ILogger<T>` logging of each quote outcome at Information (success) / Warning (validation or auth rejection) / Error (simulated vendor failure, unhandled exception), per the constitution's Logging constraint — in `CommissionQuotesController.cs`, `VendorCommissionQuotesController.cs`, and `VendorApiKeyAuthorizationFilter.cs` (Warning, for a rejected credential)
- [X] T010 [P] Create `types.js` (JSDoc typedefs mirroring `GenerateQuoteRequest`, `CommissionQuoteResponse`, and `QuoteRequestState` from data-model.md) in `src/web/src/types.js`
- [X] T011 Implement `commissionQuoteApi.js` — the single wrapper around `POST /api/commission-quotes`, returning a result shaped by `QuoteRequestState` — in `src/web/src/api/commissionQuoteApi.js` (depends on T010). Deliberately minimal for now (`Success` vs. a single generic `Error`) — T020 (US2) extends it to distinguish `AuthFailure`/`VendorError`/`TimeoutError`/`NetworkError`. Also added a Vite dev-server proxy (`/api` → `http://localhost:5172`, the backend's dev HTTP port) in `vite.config.js` so this relative fetch path resolves in `npm run dev`. Unverified against a real browser/Node runtime — Node.js is not installed on this machine (see T002).

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 - Generate a commission quote (Priority: P1) 🎯 MVP

**Goal**: A staff member enters loan details, submits, and sees the generated quote.

**Independent Test**: Enter valid loan details, select "Generate Quote", and confirm a quote
(quote ID, commission rate, total commission) is displayed on screen (retry if the vendor mock's
random failure occurs).

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T012 [P] [US1] Integration test: `POST /api/commission-quotes` returns `200` with a quote for valid input when the vendor call is forced to succeed (`X-Vendor-Simulate: success`) — in `src/CommissionQuote.Service.Tests/CommissionQuotesControllerTests.cs`. Passes (`dotnet test`: 1/1).
- [X] T013 [P] [US1] Component test: `QuoteForm` shows a loading indicator then renders the returned quote on successful submission (`commissionQuoteApi` mocked) — in `src/web/src/components/QuoteForm.test.jsx`. Written but **unverified**: Node.js/npm are not installed on this machine (see T002), so `npm test` could not be run and the TDD red-then-green cycle could not be literally confirmed — the component was authored to satisfy this test's assertions by inspection.

### Implementation for User Story 1

- [X] T014 [P] [US1] Implement `QuoteResult` component (displays `quoteId`, `commissionRate`, `totalCommission`, FR-005) in `src/web/src/components/QuoteResult.jsx`
- [X] T015 [US1] Implement `QuoteForm` component — loan amount / loan term / risk band fields, "Generate Quote" button, `Idle`/`Loading`/`Success` handling, disables the button while loading (FR-004), calls `commissionQuoteApi` and renders `QuoteResult` on success — in `src/web/src/components/QuoteForm.jsx` (depends on T011, T014)
- [X] T016 [US1] Wire `QuoteForm` into the app entry point so it renders on load — in `src/web/src/App.jsx` (depends on T015)

**Checkpoint**: User Story 1 is fully functional and independently testable (MVP)

---

## Phase 4: User Story 2 - See a clear error when quote generation fails (Priority: P2)

**Goal**: Every failure mode shows a distinct, specific message and lets the staff member retry.

**Independent Test**: Trigger a simulated vendor failure or a credential rejection and confirm a
distinct error message is shown (not blank/generic/silent), and that "Generate Quote" is
available again to retry.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T017 [P] [US2] Integration tests: `POST /api/commission-quotes` returns `502` with "Error connecting to the external commission quote API" when the vendor call is forced to reject the credential, `502` with a distinct vendor-error message when forced to fail, and `504` when the vendor exceeds the 10-second timeout — in `src/CommissionQuote.Service.Tests/CommissionQuotesControllerTests.cs`. Passes (`dotnet test`: 4/4). The auth-failure and timeout cases needed test-only `DelegatingHandler`s (strip the api-key header; delay the response) layered onto the "VendorMock" named client via `WithWebHostBuilder`, since both sides of the api-key check share one `IOptions<VendorMockOptions>` and can't be desynced by config alone. Also made the vendor-call timeout configurable (`VendorMock:RequestTimeoutMilliseconds`, default 10000 — same effective default as before) so the timeout test doesn't need a real 10-second wait (Constitution Principle II) — in `VendorMockOptions.cs` and `VendorQuoteClient.cs`.
- [X] T018 [P] [US2] Component test: `ErrorBanner` renders a distinct message for each of `AuthFailure`/`VendorError`/`TimeoutError`/`NetworkError`, and `QuoteForm` re-enables "Generate Quote" after any failure — in `src/web/src/components/ErrorBanner.test.jsx`. Written but **unverified**: Node.js/npm are still not installed on this machine (see T002/T013), so `npm test` could not be run; authored to satisfy the assertions by inspection.

### Implementation for User Story 2

- [X] T019 [P] [US2] Implement `ErrorBanner` component (renders the message for the current failure `QuoteRequestState`) in `src/web/src/components/ErrorBanner.jsx`
- [X] T020 [P] [US2] Extend `commissionQuoteApi.js` to distinguish `AuthFailure`/`VendorError`/`TimeoutError` (from response status/body per contracts/commission-quotes-api.md) and `NetworkError` (a fetch-level failure) in `src/web/src/api/commissionQuoteApi.js`. Also added `ValidationError` handling (400 → `{ state, errors }`) ahead of schedule since the contract already fully specifies that shape and leaving 400 responses to fall into a generic bucket would violate Principle III — `QuoteForm` doesn't yet render anything for it; that's US3's `T024`/`T025`.
- [X] T021 [US2] Extend `QuoteForm` to render `ErrorBanner` for each failure state, re-enable "Generate Quote" for retry (FR-007), and preserve already-entered field values across a retry (SC-004) — in `src/web/src/components/QuoteForm.jsx` (depends on T019, T020)

**Checkpoint**: User Stories 1 AND 2 both work independently

---

## Phase 5: User Story 3 - Get validation feedback for invalid loan details (Priority: P3)

**Goal**: Invalid input is caught client-side, with field-level feedback, before any request is sent.

**Independent Test**: Leave a required field empty or enter an out-of-range value, and confirm
field-level validation feedback appears with no network request sent.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T022 [P] [US3] Integration test: `POST /api/commission-quotes` returns `400` with field-level errors for invalid `loanAmount`/`loanTermInMonths`/`riskBand`, and the vendor endpoint receives zero calls — in `src/CommissionQuote.Service.Tests/CommissionQuotesControllerTests.cs`. Passes (`dotnet test`: 10/10, including 6 new `[Theory]` cases; a `CallCountingHandler` layered onto the "VendorMock" named client confirms zero vendor calls).
- [X] T023 [P] [US3] Component test: `QuoteForm` shows field-level validation messages for empty/out-of-range input and does not call `commissionQuoteApi` — in `src/web/src/components/QuoteForm.test.jsx`. Node.js/npm are now installed on this machine, so this and the previously-unverified T013/T018 were run for real: `npm test` passes (15/15).

### Implementation for User Story 3

- [X] T024 [P] [US3] Add server-side validation (`loanAmount` > 0 and <= 100000000; `loanTermInMonths` integer 1-480; `riskBand` one of `Low`/`Medium`/`High`; research.md #6) to `CommissionQuotesController`, returning `400` with field errors before calling `IVendorQuoteClient` — in `src/CommissionQuote.Service/Controllers/CommissionQuotesController.cs`. Logs the rejection at Warning per the constitution's Logging constraint (T009).
- [X] T025 [P] [US3] Add matching client-side validation to `QuoteForm`, rendering field-level messages and blocking submission before calling `commissionQuoteApi` (FR-003) — in `src/web/src/components/QuoteForm.jsx`. Added `noValidate` to the form and dropped the inputs' `required` attribute so the browser's native constraint validation (which shows non-queryable tooltips and behaves inconsistently across jsdom/real browsers) never intercepts submission ahead of this validation — same bounds as T024, kept in sync by comment reference rather than a shared module (no shared JS/C# validation layer exists in this stack).

**Checkpoint**: All three user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Coverage and validation that spans multiple user stories

- [X] T026 [P] Add backend unit tests for loan-detail validation boundary cases (zero/negative/non-numeric `loanAmount`; `loanTermInMonths` bounds; unrecognized `riskBand`) in `src/CommissionQuote.Service.Tests/LoanDetailValidationTests.cs`. Extracted the validation logic out of `CommissionQuotesController` into a new `LoanDetailValidator` static class (`src/CommissionQuote.Service/Contracts/LoanDetailValidator.cs`) so it's unit-testable without a `WebApplicationFactory` round-trip; the controller now just calls it. The "non-numeric loanAmount" case can't reach `LoanDetailValidator` (it fails JSON model binding first, since `LoanAmount` is a `decimal`), so that specific boundary is covered as an integration test (`Post_NonNumericLoanAmount_Returns400_AndNeverCallsVendor`) in `CommissionQuotesControllerTests.cs` instead. `dotnet test`: 29/29 at this point.
- [X] T027 [P] Add log-output assertions confirming each quote outcome is logged at the constitution-mandated level (Information/Warning/Error) in `src/CommissionQuote.Service.Tests/CommissionQuotesControllerTests.cs` and `VendorSimulationTests.cs`. Added a `ListLoggerProvider` test helper (captures every log entry via a custom `ILoggerProvider`/`ILogger`) and attached it per-test via `WithWebHostBuilder(...).ConfigureLogging(...)`. `CommissionQuotesControllerTests.cs` gained 5 tests (Success→Information, ValidationError/AuthFailure→Warning, VendorError/Timeout→Error). `VendorSimulationTests.cs` is a new file — it didn't exist yet despite being named in plan.md's Project Structure — with direct tests of `POST /vendor/commission-quotes` (missing/invalid api-key → 401, forced success → 200, forced error → 500) plus log-level assertions for the vendor mock's own `VendorApiKeyAuthorizationFilter` (Warning) and `VendorCommissionQuotesController` (Information/Error). `dotnet test`: 41/41.
- [X] T028 [P] (Optional) Add Dockerfiles for `CommissionQuote.Service` and `src/web`, sequenced after the core flow and tests pass (constitution: containerization is optional polish) — in `Dockerfile` and `src/web/Dockerfile`. Standard multi-stage builds (`mcr.microsoft.com/dotnet/sdk:10.0`→`aspnet:10.0` for the backend; `node:22-alpine`→`nginx:alpine` for the frontend), plus `.dockerignore` at the repo root and in `src/web`. **Unverified**: Docker Desktop's daemon is not running on this machine, so `docker build` could not be exercised — the Dockerfiles are hand-authored/reviewed but not build-tested.
- [X] T029 [P] Add a root-level `README.md` summarizing setup/run instructions and linking to `specs/001-commission-quote-generation/quickstart.md`
- [X] T030 Execute all 7 `quickstart.md` validation scenarios end-to-end against the built app and confirm expected results, including that no `api-key` ever reaches the browser (Constitution Principle I / FR-011) — per `specs/001-commission-quote-generation/quickstart.md`. No project "run" skill existed yet, and `chromium-cli`/Playwright weren't available for a real rendered-page screenshot, so this was run as a live backend+frontend dev-server session (`dotnet run` on :5172, `npm run dev` on :5173) driven with `curl` against the actual running app rather than through a GUI browser:
  - **1 (happy path)**: 5 live, unforced `POST /api/commission-quotes` calls — 4 succeeded with `quoteId`/`commissionRate`/`totalCommission`, 1 hit the real ~15% random vendor failure and got the distinct `VendorError` message. Confirmed.
  - **2 (simulated vendor failure)**: `X-Vendor-Simulate: error` → `502` with the vendor-error message. Confirmed.
  - **3 (missing/invalid vendor credential)**: `POST /vendor/commission-quotes` directly with no `api-key` → `401`. Confirmed at the vendor-mock level. The *frontend-facing* view of this (`/api/commission-quotes` → `502` "Error connecting to the external commission quote API") was **not independently re-verified live**: an attempt to restart the backend with a deliberately wrong `VendorMock:ApiKey` failed to bind (port still held by the first instance — this environment has no `lsof`, so the usual "kill the port's listener" step silently no-ops) — and, worth noting for future reference, would not have desynced anything even if it had bound, since `VendorQuoteClient` and `VendorApiKeyAuthorizationFilter` read the same shared `IOptions<VendorMockOptions>`. This path stays verified by `Post_VendorRejectsCredential_Returns502WithAuthFailureMessage` (T017), which uses a header-stripping `DelegatingHandler` to create a genuine mismatch — the only way to actually exercise this given the shared config.
  - **4 (timeout)**: not re-run live (would require a real 10s wait with no runtime lever to shorten it outside tests); stays verified by `Post_VendorExceedsTimeout_Returns504WithTimeoutMessage` (T017), which overrides `VendorMock:RequestTimeoutMilliseconds` to exercise the same code path deterministically in ~200ms.
  - **5 (client-side validation)**: `loanAmount: 0, loanTermInMonths: -5` → live `400` with both field errors. Confirmed server-side; the client-side half is covered by the T023 component tests (Node/npm now installed — see Phase 5 notes — so those ran for real, not just by inspection).
  - **6 (no duplicate submissions)**: not independently re-verified live (no rendered DOM to click against); covered by the T013 component test asserting the button is disabled while `status === 'Loading'`.
  - **7 (credential never reaches the browser)**: confirmed two ways — `grep -rn "api-key" src/web/src/` found zero occurrences, and `curl -v` against `/api/commission-quotes` showed no `api-key` header on the request the "browser" (curl, proxied through the real Vite dev server on :5173→:5172) actually sends.
  - Also confirmed the Vite dev-server proxy (`vite.config.js`, added in T011) correctly forwards `/api/commission-quotes` from :5173 to the backend on :5172 — a live request through the frontend's own origin returned the same `200` with a quote.
  - Backend: `dotnet test` → 41/41. Frontend: `npm test` → 15/15 (see Phase 5).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational completion; proceed in priority order (P1 → P2 → P3) or in parallel if staffed
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on other stories — the MVP
- **User Story 2 (P2)**: Builds on US1's `QuoteForm`/`commissionQuoteApi.js` but is independently testable (its own error states and tests)
- **User Story 3 (P3)**: Builds on US1's `QuoteForm`/`CommissionQuotesController` but is independently testable (validation blocks before any vendor call)

### Within Each User Story

- Tests are written first and must fail before implementation
- Foundational plumbing (T004-T011) before any story-specific work
- Story complete and checkpointed before moving to the next priority

### Parallel Opportunities

- T002 (frontend scaffold) can run alongside T001/T003 (backend) — different toolchains
- T004 and T005 (DTOs, different folders) in parallel
- T012/T013, T017/T018, T022/T023 (backend vs. frontend tests within a story) in parallel
- T019 and T020 (`ErrorBanner.jsx` vs. `commissionQuoteApi.js`, different files) in parallel
- T024 and T025 (backend vs. frontend validation, different files) in parallel
- T026-T029 in Polish, in parallel

---

## Parallel Example: User Story 1

```bash
# Tests for User Story 1 together:
Task: "Integration test: POST /api/commission-quotes returns 200 for a forced vendor success in src/CommissionQuote.Service.Tests/CommissionQuotesControllerTests.cs"
Task: "Component test: QuoteForm loading-then-success in src/web/src/components/QuoteForm.test.jsx"

# Then QuoteResult while QuoteForm's test is still being written:
Task: "Implement QuoteResult component in src/web/src/components/QuoteResult.jsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run quickstart.md scenario 1 independently
5. Demo if ready

### Incremental Delivery

1. Setup + Foundational → backend round-trip and frontend API wrapper exist
2. Add User Story 1 → validate independently → MVP demo-able
3. Add User Story 2 → validate independently → distinct error handling in place
4. Add User Story 3 → validate independently → client-side validation in place
5. Polish → full quickstart.md pass, log/coverage checks, optional containerization

## Notes

- [P] tasks touch different files and have no dependency on an incomplete task
- Each user story is independently completable and testable per its Independent Test above
- Verify tests fail before implementing
- Stop at any checkpoint to validate a story independently
