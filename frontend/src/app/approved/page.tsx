import Link from "next/link";
import { Suspense } from "react";
import { Card, primaryLinkClass } from "@/components/card";

export default function ApprovedPage({ searchParams }: PageProps<"/approved">) {
  return (
    <Card>
      {/* searchParams is request-time data: read it inside a Suspense boundary so the shell can prerender. */}
      <Suspense fallback={<p className="text-slate-500">Loading your decision…</p>}>
        <ApprovedDetails searchParams={searchParams} />
      </Suspense>
      <div className="mt-8">
        <Link href="/" className={primaryLinkClass}>
          Back to the application
        </Link>
      </div>
    </Card>
  );
}

async function ApprovedDetails({ searchParams }: Pick<PageProps<"/approved">, "searchParams">) {
  const { id, amount, returning } = await searchParams;
  const isReturning = returning === "1";
  const formattedAmount = Number(amount).toLocaleString("en-US", { style: "currency", currency: "USD" });
  const reference = typeof id === "string" ? id.slice(0, 8).toUpperCase() : null;

  return (
    <>
      <div className="mb-4 flex size-12 items-center justify-center rounded-full bg-green-100 text-2xl text-green-700" aria-hidden>
        ✓
      </div>
      <h1 className="text-2xl font-semibold tracking-tight">
        {isReturning ? "Your application was updated" : "Your application is approved"}
      </h1>
      <p className="mt-2 text-slate-600">
        {isReturning
          ? "We found your existing application and updated it with the details you just sent."
          : "Good news — your business loan application has been approved."}
      </p>
      <dl className="mt-6 grid gap-4 rounded-lg bg-slate-50 p-4 sm:grid-cols-2">
        <div>
          <dt className="text-sm text-slate-500">Requested amount</dt>
          <dd className="text-lg font-semibold">{formattedAmount}</dd>
        </div>
        {reference && (
          <div>
            <dt className="text-sm text-slate-500">Reference</dt>
            <dd className="font-mono text-lg font-semibold">{reference}</dd>
          </div>
        )}
      </dl>
      <p className="mt-6 text-sm text-slate-600">
        What&apos;s next: our team will reach out by email within one business day to finalize your loan.
      </p>
    </>
  );
}
