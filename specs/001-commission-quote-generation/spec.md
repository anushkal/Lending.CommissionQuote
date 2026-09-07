# Feature Specification: Commission Quote Generation

**Feature Branch**: `001-commission-quote-generation`

**Created**: 2026-09-04

**Status**: Draft

**Input**: User description: "Context
In our Lending Platform, staff members frequently need to generate \"Commission Quotes\" based on loan applications. To do this, our system needs to
process loan details and send them to an external Vendor system, which calculates the commission and returns a quote.
Currently, the external vendor Commission Quote API is under construction and not yet available. However, the API contract and authentication
requirements have been finalized. We need to unblock our development by building our application against a mock version of this API.
The Task & Requirements
Your task is to build a web application that allows a user to input loan details and displays the generated commission quote, along with a mock version of
this Commission Quote API that simulates the external vendor.
1. Web Application Requirements
A user interface with a form to capture basic loan details (e.g., loanAmount, loanTermInMonths, riskBand).
A \"Generate Quote\" button.
A display area to show the resulting quote data when the request is successful.
Proper loading states and error messages if the quote generation fails.
2. Commission Quote API Spec
Contract: It must accept and return data based on this agreed contract:
Request Payload: loanAmount, loanTermInMonths, riskBand
Response Payload: quoteId, commissionRate, totalCommission
Security: The Commission Quote API is strictly secured. It must require an api-key header to process the request. Any request without a valid
API key should be rejected.
Simulation: To mimic real-world network conditions, your Commission Quote API must occasionally (randomly) throw an error."

## Clarifications

### Session 2026-09-04

- Q: What message should the staff member see when the Commission Quote API rejects a request because of a missing or invalid credential? → A: Show "Error connecting to the external commission quote API" — a generic connectivity-style message rather than any vendor-specific error text, since a credential problem is not something the staff member can act on.
- Q: How long should the system wait for a response from the Commission Quote API before treating the request as timed out? → A: 10 seconds.
- Q: Should the "Generate Quote" button be disabled while a request is in flight, to prevent duplicate submissions? → A: Yes — disable the button/form while loading, so a second click cannot start a duplicate request.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate a commission quote (Priority: P1)

A staff member enters the basic details of a loan application and requests a commission quote so
they can share the expected commission with the applicant or their manager.

**Why this priority**: This is the core value of the feature — without it there is nothing to
build. It must work end-to-end before anything else matters.

**Independent Test**: Can be fully tested by entering valid loan details, submitting the request,
and confirming a quote (quote ID, commission rate, total commission) is displayed on screen.

**Acceptance Scenarios**:

1. **Given** the quote form is empty, **When** the staff member enters a valid loan amount, loan
   term, and risk band and selects "Generate Quote", **Then** a loading indicator is shown while
   the request is processed.
2. **Given** a quote request that the vendor accepts and processes successfully, **When** the
   response is received, **Then** the quote ID, commission rate, and total commission are
   displayed to the staff member.

---

### User Story 2 - See a clear error when quote generation fails (Priority: P2)

A staff member submits a valid set of loan details, but the request fails because the vendor
service is unavailable or returns a simulated error. The staff member needs to understand that
something went wrong on the service side (not their input) and be able to try again.

**Why this priority**: The vendor is explicitly simulated as unreliable, so handling failure
gracefully is essential to the feature being usable in practice, not just in the happy path.

**Independent Test**: Can be fully tested by triggering a simulated vendor failure (or a request
denied for an invalid/missing credential) and confirming a distinct, specific error message is
shown instead of a blank result, a generic message, or a silent failure.

**Acceptance Scenarios**:

1. **Given** valid loan details, **When** the vendor request fails with a simulated service
   error, **Then** the staff member sees a message indicating the quote service failed and that
   they may retry.
2. **Given** a previous failed attempt, **When** the staff member selects "Generate Quote" again,
   **Then** the system attempts the request again and shows the loading state, success state, or
   a new error state based on the outcome.

---

### User Story 3 - Get validation feedback for invalid loan details (Priority: P3)

A staff member enters incomplete or out-of-range loan details (for example, a missing loan
amount or an unrecognized risk band) and needs to know what to fix before a request is sent.

**Why this priority**: Prevents wasted requests and confusing failures, but the feature is still
usable without it as long as the vendor's own rejection is surfaced clearly (covered by User
Story 2); this refines the experience rather than enabling it.

**Independent Test**: Can be fully tested by leaving a required field blank or entering an
out-of-range value and confirming the form shows field-level validation feedback without
attempting to generate a quote.

**Acceptance Scenarios**:

1. **Given** the quote form, **When** the staff member selects "Generate Quote" with a required
   field empty, **Then** the form highlights the missing field and no request is sent.
2. **Given** the quote form, **When** the staff member enters a loan amount or term that is zero,
   negative, or otherwise out of range, **Then** the form shows a validation message and no
   request is sent.

---

### Edge Cases

- What happens when the loan amount is zero, negative, non-numeric, or unreasonably large?
- What happens when the loan term is zero, negative, or unreasonably large?
- What happens when the risk band value is missing or not one of the accepted categories?
- When the Commission Quote API request is rejected for a missing or invalid credential, the
  staff member sees "Error connecting to the external commission quote API" rather than any
  vendor-specific error text.
- While a quote request is in flight, the "Generate Quote" button is disabled so the staff
  member cannot start a duplicate request before the first one completes.
- When a request to the Commission Quote API does not receive a response within 10 seconds, the
  system treats it as timed out and shows a distinct timeout failure state, separate from a
  simulated vendor error response.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a form for staff to enter loan details: loan amount, loan term
  (in months), and risk band.
- **FR-002**: System MUST provide a "Generate Quote" action that submits the entered loan details
  to request a commission quote.
- **FR-003**: System MUST validate loan detail inputs before submission and show field-level
  feedback when a value is missing or out of range, without sending a request to the Commission
  Quote API.
- **FR-004**: System MUST show a loading state to the staff member while a quote request is in
  progress, and MUST disable the "Generate Quote" action during that time so a second submission
  cannot start a duplicate request.
- **FR-005**: Upon a successful quote response, System MUST display the quote ID, commission
  rate, and total commission to the staff member.
- **FR-006**: System MUST show a distinct, specific, user-visible message for each failure mode
  (input validation failure, rejected/missing credential, simulated vendor error, timeout or
  network failure) rather than a generic or silent failure.
- **FR-007**: System MUST allow the staff member to retry generating a quote after a failure.
- **FR-008**: The Commission Quote API MUST accept a request containing loan amount, loan term
  in months, and risk band, and, when successful, MUST return a quote ID, commission rate, and
  total commission.
- **FR-009**: The Commission Quote API MUST require a valid API key credential on every request
  and MUST reject any request that lacks one or presents an invalid one, without calculating a
  commission.
- **FR-010**: The Commission Quote API MUST simulate real-world vendor instability by randomly
  failing a portion of otherwise-valid, correctly-authenticated requests.
- **FR-011**: System MUST NOT expose the Commission Quote API's credential to the staff member or
  within the browser-based interface.
- **FR-012**: When the Commission Quote API rejects a request due to a missing or invalid
  credential, System MUST display the message "Error connecting to the external commission
  quote API" to the staff member, rather than the vendor's own error text.
- **FR-013**: If a Commission Quote API request does not receive a response within 10 seconds,
  System MUST treat it as timed out and show the staff member a timeout failure state distinct
  from a simulated vendor error response.

### Key Entities

- **Loan Detail Input**: The information a staff member submits to request a quote — loan
  amount, loan term in months, and risk band.
- **Commission Quote**: The result of a successful quote request — a quote ID, a commission
  rate, and a total commission amount — corresponding to one Loan Detail Input submission.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A staff member can go from an empty form to seeing a generated quote in under 30
  seconds, excluding any time spent correcting invalid input.
- **SC-002**: 100% of Commission Quote API requests that lack a valid credential are rejected
  before any commission is calculated.
- **SC-003**: 100% of failed quote attempts (validation, credential rejection, simulated vendor
  error, timeout) display a distinct, specific on-screen message — none result in a blank
  screen, a generic message, or no visible response.
- **SC-004**: A staff member can retry a failed quote request without reloading the page or
  re-entering loan details that were already valid.

## Assumptions

- Risk band is selected from a small, fixed set of categories (e.g., Low, Medium, High); the
  exact category list is a business detail to be confirmed during planning, not a blocker to
  specifying this feature.
- The Commission Quote API's credential is a single, pre-shared value issued to this
  application, not a per-user or per-staff-member credential — consistent with it authenticating
  the calling application to the vendor, not the individual staff member.
- The simulated random failure occurs at a moderate rate (roughly 1 in 5 to 1 in 10 requests)
  intended to be noticeable during normal use and testing, rather than rare or dominant.
- Staff members are already authenticated into the Lending Platform by some existing mechanism;
  this feature does not introduce a new staff login step.
- This feature covers generating and viewing a single quote per submission; saving, listing, or
  comparing historical quotes is out of scope for this feature.
