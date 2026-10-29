import type { ReactNode } from "react";

type FieldProps = {
  id: string;
  label: string;
  error?: string;
  hint?: string;
  className?: string;
  children: (props: { id: string; name: string; "aria-invalid": boolean; "aria-describedby"?: string }) => ReactNode;
};

/** Label + control + hint/error, wired for screen readers. */
export function Field({ id, label, error, hint, className = "", children }: FieldProps) {
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined;
  return (
    <div className={className}>
      <label htmlFor={id} className="mb-1 block text-sm font-medium text-slate-800">
        {label}
      </label>
      {children({ id, name: id, "aria-invalid": Boolean(error), "aria-describedby": describedBy })}
      {error ? (
        <p id={`${id}-error`} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      ) : hint ? (
        <p id={`${id}-hint`} className="mt-1 text-sm text-slate-500">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

export const inputClass =
  "block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 shadow-sm outline-none " +
  "placeholder:text-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-600/20 " +
  "aria-invalid:border-red-600 aria-invalid:focus:ring-red-600/20";
