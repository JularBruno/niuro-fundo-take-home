# Frontend

Next.js 16 app for the loan application form. See the [root README](../README.md) for how to run everything, and [ARCHITECTURE.md](../ARCHITECTURE.md) for design decisions.

```bash
npm install
npm run dev   # http://localhost:3000, expects the API on http://localhost:5009 (override with API_URL in .env.local)
```

| Path | What it is |
|---|---|
| `src/app/page.tsx` + `application-form.tsx` | The form (client component with `useActionState`) |
| `src/app/actions.ts` | The Server Action: validates, calls the API, then redirects or returns field errors |
| `src/lib/application.ts` | Field list and validation rules (mirror the API's rules) |
| `src/lib/loan-api.ts` | Server-side client for `POST /api/applications` |
| `src/app/approved`, `src/app/denied` | Result pages |
