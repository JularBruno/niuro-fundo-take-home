import Link from "next/link";
import { Suspense } from "react";
import { Card, primaryLinkClass } from "@/components/card";

// The blacklist reason is deliberately shown as a generic message: telling someone their SSN is
// blacklisted leaks sensitive information.
const MESSAGES = {
  StateNotSupported: {
    title: "We can't offer loans in New York yet",
    body: "We're not currently able to lend to businesses located in New York. We're sorry for the inconvenience.",
  },
  generic: {
    title: "We're unable to approve your application",
    body: "After reviewing your information, we can't approve this application at this time.",
  },
};

export default function DeniedPage({ searchParams }: PageProps<"/denied">) {
  return (
    <Card>
      <Suspense fallback={<p className="text-slate-500">Loading your decision…</p>}>
        <DeniedDetails searchParams={searchParams} />
      </Suspense>
      <div className="mt-8">
        <Link href="/" className={primaryLinkClass}>
          Back to the application
        </Link>
      </div>
    </Card>
  );
}

async function DeniedDetails({ searchParams }: Pick<PageProps<"/denied">, "searchParams">) {
  const { reason } = await searchParams;
  const message = reason === "StateNotSupported" ? MESSAGES.StateNotSupported : MESSAGES.generic;

  return (
    <>
      <div className="mb-4 flex size-12 items-center justify-center rounded-full bg-slate-100 text-2xl text-slate-600" aria-hidden>
        ✕
      </div>
      <h1 className="text-3xl font-bold tracking-tight text-slate-900">{message.title}</h1>
      <p className="mt-2 text-slate-600">{message.body}</p>
    </>
  );
}
