# Phase 0 Research: Commission Quote Generation

This document resolves every technical unknown left after `/speckit-specify` and
`/speckit-clarify`, so Phase 1 design can proceed without open questions. The constitution
(`.specify/memory/constitution.md`) already fixes the language, framework, repository layout,
and naming conventions; the decisions below fill in the remaining specifics needed to build.

## 1. .NET target version

**Decision**: .NET 10.

**Rationale**: The constitution mandates C# / ASP.NET Core Web API but doesn't pin a version.
The original plan picked .NET 8 (LTS) to minimize the risk of an SDK mismatch. During
`/speckit-implement` (Setup phase, T001), `dotnet --list-sdks`/`--list-runtimes` on the actual
build machine showed only the .NET 10 SDK and runtime installed — no .NET 8 — so `dotnet new`
targeted `net10.0` by default and the project was kept on that target rather than forcing an
LTS version this machine cannot run. This supersedes the original decision: matching the
environment that will actually build and run the code takes priority over the LTS-preference
rationale once they conflict.

**Alternatives considered**: Installing the .NET 8 SDK side-by-side to honor the original
decision — rejected as unnecessary friction for a single-environment exercise; .NET 10 has no
feature gap that would affect this feature.

**Alternatives considered**: A newer non-LTS release — rejected, since this feature has no
requirement that depends on it, and LTS reduces environment-compatibility risk for no cost.

## 2. Frontend tooling

**Decision**: React + JavaScript, built with Vite; Vitest + React Testing Library for
component tests.

**Rationale**: The constitution's frontend structure explicitly names `.js`/`.jsx` files
(`commissionQuoteApi.js`, `types.js`, `QuoteForm.test.jsx`) — plain JavaScript, no TypeScript
compiler step. Vite gives a fast dev server and a minimal, standard config for a small
single-purpose app, and works with plain JS/JSX out of the box. Vitest reuses Vite's config and
transform pipeline (no separate Babel/webpack setup) and pairs naturally with React Testing
Library for colocated component tests, matching the constitution's "tests colocated next to the
component they cover" rule.

**Alternatives considered**: TypeScript — rejected per the constitution (Technical Constraints,
amended v1.1.0), which specifies plain `.js`/`.jsx` for the frontend. Create React App —
deprecated/unmaintained, rejected. Jest — works, but duplicates config Vite already provides and
is slower to set up for a Vite project; rejected in favor of Vitest's drop-in compatibility with
the Jest API.

## 3. Vendor mock: real HTTP boundary within one project

**Decision**: The vendor mock is a second set of controller endpoints inside
`CommissionQuote.Service`, under the `VendorSimulation` namespace, reached by the backend over
real outbound HTTP (a named `HttpClient`) rather than a direct in-process method call — even
though both run in the same process and are deployed as one project.

**Rationale**: The constitution requires the mock to live in the same project by default *and*
requires it to be a real HTTP boundary (the api-key header check and randomized failure "only
make sense as things that happen at a network boundary"). Hosting a second controller in the
same ASP.NET Core app, called via `HttpClient` against its own base URL, satisfies both: no
second deployable, but a real request/response cycle with real headers, status codes, and
timeout behavior — not a shortcut method call that would let the security check or the random
failure be bypassed accidentally.

**Alternatives considered**: A genuinely separate vendor-mock project/process — rejected as
the default per the constitution's explicit preference, reserved only if a concrete reason to
expose that boundary shows up later. An in-process interface with two implementations (real
vendor client vs. fake) — rejected because it would let request/response, headers, and timeout
handling be skipped in a way the constitution says would defeat the point of the exercise.

## 4. Deterministic override for the simulated vendor failure

**Decision**: The vendor mock endpoint accepts an optional `X-Vendor-Simulate` request header
with values `success` or `error`. When present, it deterministically forces that outcome
instead of rolling randomly. Application configuration also exposes a `VendorMock:FailureRate`
setting (default ~15%) so the ambient random-failure rate itself is not hard-coded.

**Rationale**: Constitution Principle II requires the simulated randomness to be overridable
through a header, query param, or config flag, and requires tests to never depend on true
randomness. A request header lets integration tests force both the success and failure paths
deterministically; the config value lets the ambient (non-overridden) behavior be tuned or
disabled without a code change.

**Alternatives considered**: A query parameter instead of a header — rejected only because a
header keeps the parameter out of the request payload/URL and off any logs that capture query
strings; either would have satisfied the constitution. Seeding a fixed random generator —
rejected because it makes "force success" and "force error" less explicit and harder to read in
a test than a named override.

## 5. Failure-mode → user-visible state mapping

**Decision**: Six mutually exclusive request states beyond `Idle`/`Loading`/`Success`:
`ValidationError`, `AuthFailure` (vendor rejected the credential), `VendorError` (simulated
random vendor failure), `TimeoutError` (no vendor response within 10s), and `NetworkError`
(the browser could not reach our own backend at all).

**Rationale**: Constitution Principle III names five distinct failure modes (validation error,
auth failure, simulated vendor error, timeout, network failure) that must each map to a
distinct, user-visible state. The spec's clarifications pin the exact user-visible text for the
auth-failure case ("Error connecting to the external commission quote API") and the timeout
threshold (10 seconds), which this mapping incorporates directly.

**Alternatives considered**: Collapsing auth-failure and vendor-error into one generic
"vendor problem" state — rejected because it would violate Principle III's explicit five-mode
list and the spec's requirement that each failure mode be distinguishable.

## 6. Loan detail validation bounds

**Decision**: `loanAmount` must be a positive number greater than 0 and no greater than
100,000,000; `loanTermInMonths` must be a positive integer between 1 and 480 (40 years);
`riskBand` must be one of `Low`, `Medium`, `High`.

**Rationale**: The spec's Assumptions section documents these as reasonable, non-blocking
defaults (risk-band category list) and flags loan amount/term bounds as still open at a
low-impact level. Concrete bounds are needed for FR-003's validation behavior to be testable;
these values are generous enough not to constrain realistic commercial/consumer loan scenarios
while still catching obvious bad input (zero, negative, or absurd values) per the spec's edge
cases.

**Alternatives considered**: Leaving bounds unenforced beyond "positive" — rejected because the
spec explicitly calls out "unreasonably large" as an edge case requiring a validation response,
which needs a concrete threshold to be testable.
