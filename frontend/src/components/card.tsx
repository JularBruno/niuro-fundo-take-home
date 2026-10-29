import type { ReactNode } from "react";

export function Card({ children }: { children: ReactNode }) {
  return <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">{children}</div>;
}

export const primaryLinkClass =
  "inline-block rounded-lg bg-blue-700 px-4 py-2.5 font-semibold text-white hover:bg-blue-800 " +
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700";
