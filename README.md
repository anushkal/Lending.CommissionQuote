# Lending Commission Quote

An internal staff tool for generating lending commission quotes. A React frontend collects loan
details and requests a quote from the `CommissionQuote.Service` backend, which validates the
input and calls a mock external vendor Commission Quote API over real HTTP.

See [specs/001-commission-quote-generation/](specs/001-commission-quote-generation/) for the
full feature specification, plan, and design artifacts, and
[specs/001-commission-quote-generation/quickstart.md](specs/001-commission-quote-generation/quickstart.md)
for detailed setup, run, and manual validation steps.

## AI Usage

This project uses spec-driven development with Claude Code.

## Prerequisites

- .NET 10 SDK
- Node.js (LTS) with npm

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

## Test

```bash
# Backend
dotnet test CommissionQuote.sln

# Frontend
cd src/web
npm test
```

## Project structure

```text
CommissionQuote.sln
src/
├── CommissionQuote.Service/        # ASP.NET Core Web API: staff-facing endpoint + vendor mock
├── CommissionQuote.Service.Tests/  # xUnit integration/unit tests
└── web/                            # React (Vite) frontend
```

## Containers (optional)

Dockerfiles are provided for both services (`Dockerfile` for the backend, `src/web/Dockerfile`
for the frontend) as optional polish — not required to run or test the feature locally.
