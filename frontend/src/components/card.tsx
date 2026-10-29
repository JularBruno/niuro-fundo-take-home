import type { ReactNode } from "react";

export function Card({ children }: { children: ReactNode }) {
  return <div className="rounded-2xl border border-emerald-100 border-t-4 border-t-emerald-700 bg-white p-6 shadow-md shadow-emerald-900/5 sm:p-8">{children}</div>;
}

export const primaryLinkClass =
  "inline-block rounded-lg bg-emerald-700 px-4 py-2.5 font-semibold text-white hover:bg-emerald-800 " +
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700";
