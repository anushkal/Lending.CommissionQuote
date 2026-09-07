<!--
Sync Impact Report
- Version change: 1.0.0 → 1.1.0
- Rationale: MINOR bump. This amendment redefines a Technical Constraints value (frontend
  language) rather than adding a new principle, but it materially changes binding guidance
  for all future frontend work, so it is more than a non-semantic PATCH; it does not remove
  or redefine a Core Principle (I-IV), so MAJOR is not warranted either.
- Modified principles: none
- Modified sections:
  - Technical Constraints > "Frontend structure & testing" — frontend source/test file
    extensions changed from TypeScript (.ts/.tsx) to JavaScript (.js/.jsx):
    commissionQuoteApi.ts → commissionQuoteApi.js, types.ts → types.js,
    QuoteForm.test.tsx → QuoteForm.test.jsx.
- Added sections: none (see Note below)
- Removed sections: none
- Note: the on-disk file at the start of this run was missing the `## Governance` section
  and the trailing `**Version** | **Ratified** | **Last Amended**` line entirely (present in
  this constitution's original 1.0.0 draft, absent from the version this amendment started
  from). Both are required by the constitution template, so they have been restored verbatim
  from the 1.0.0 draft as part of this amendment rather than left missing.
- Templates requiring follow-up: none flagged — dependent templates and previously generated
  planning artifacts (plan.md, research.md, data-model.md, quickstart.md under
  specs/001-commission-quote-generation/, which specified TypeScript/Vitest/React Testing
  Library) are read/regenerated at runtime by other commands and were not modified by this
  command per the scope guard. Re-run `/speckit-plan` for that feature to bring its Phase 0/1
  artifacts in line with this change.
- Deferred/TODO placeholders: none.

Prior report (1.0.0, superseded):
- Version change: (unratified template) → 1.0.0
- Rationale: Initial ratification. The constitution file previously held only unfilled
  template placeholders; this was the first substantive adoption, not an amendment, so it was
  versioned as a new MAJOR baseline (1.0.0).
- Added principles: I. Security Is Non-Negotiable; II. Deterministic Testability;
  III. Explicit Error Handling; IV. Comprehension Over Generation
- Added sections: Technical Constraints (replaced template's generic "Section 2" slot)
- Removed sections: template's generic "Section 3" slot (left unpopulated, omitted)
-->

# Lending Commission Quote Constitution

## Core Principles

### I. Security Is Non-Negotiable

The api-key header check MUST happen before any request body is parsed or business logic
runs. There MUST be no code path that reaches the commission calculation without a valid
key. This is one of the few things in this project that is done rigorously rather than
minimally.

The vendor api-key MUST be attached server-side only, at the point where the backend calls
the mock vendor endpoint. The React app MUST NOT see or send the vendor's api-key — it
talks exclusively to the backend, which is the only caller of the vendor mock. A design
where the frontend calls the vendor directly is a violation of this principle regardless of
how the key is stored client-side.

### II. Deterministic Testability

Any simulated randomness (e.g., the vendor's random failure) MUST be overridable through a
header, query param, or config flag. Tests MUST NOT depend on true randomness. If a behavior
cannot be made deterministic for testing, the code MUST carry a documented reason why.

### III. Explicit Error Handling

Every failure mode (validation error, auth failure, simulated vendor error, timeout, network
failure) MUST map to a distinct, user-visible state — never a generic "something went wrong"
and never a silent failure or unhandled rejection. Loading, success, and each error state are
all first-class UI states, not afterthoughts.

### IV. Comprehension Over Generation

Any AI-generated code MUST be understood well enough to explain line-by-line, including why
it was structured that way and what it trades off. If a generated approach cannot be
explained, it MUST be rewritten or replaced, not shipped.

## Technical Constraints

- **Repository structure**: monorepo — backend and frontend live in one repo as plain
  folders (`src/CommissionQuote.Service`, `src/web`), not separate repos.
- **Backend**: C# / ASP.NET Core Web API.
- **Frontend**: React.
- **Testing**: xUnit (or equivalent) for unit and integration tests; no target coverage
  percentage — coverage MUST include the edge cases documented in SPEC.md.
- **Persistence**: none by default; only introduced if a specific requirement demands it,
  and if so, kept to the minimum needed (single table, simple repository — no premature
  abstraction).
- **Containerization**: optional polish, sequenced after the core flow and tests are
  complete and passing.
- **Logging**: built-in framework logging only — `ILogger<T>` on the backend, browser
  console on the frontend. The outcome of each quote request MUST be logged at Information
  (success), Warning (validation/auth rejection), or Error (simulated vendor failure,
  unhandled exception).
- **.NET naming**: solution CommissionQuote.sln; assembly name, root namespace, and folder name kept identical for every project (CommissionQuote.Service, CommissionQuote.Service.Tests. Vendor mock lives in the same project by default under a VendorSimulation namespace, split into its own CommissionQuote.VendorMock project only if there's a specific reason to want that boundary visible, and note the reason if so. Controllers are plural/resource-named (CommissionQuotesController); request/response DTOs are explicitly suffixed (GenerateQuoteRequest, CommissionQuoteResponse) so they're never confused with domain types; interfaces use the standard I prefix (ICommissionCalculator).
- **Frontend structure & testing**: flat structure under src/web/src/ — components/ (QuoteForm, QuoteResult, ErrorBanner), api/ (single commissionQuoteApi.js wrapping the one call to the backend), types.js mirroring the backend DTOs. Tests are colocated next to the component they cover (QuoteForm.test.jsx), not a separate __tests__ tree
- **Vendor mock architecture**: the mock needs to be a real HTTP boundary, not an in-process method call — the brief's api-key header check and randomized failure only make sense as things that happen at a network boundary
- **Vendor mock testing**: use WebApplicationFactory for testing both CommissionQuote.Service and the vendor mock


