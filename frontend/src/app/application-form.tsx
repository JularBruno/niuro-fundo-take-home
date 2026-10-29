"use client";

import { useActionState, useEffect, useRef } from "react";
import { Field, inputClass } from "@/components/field";
import { emptyValues } from "@/lib/application";
import { US_STATES } from "@/lib/us-states";
import { type FormState, submitApplicationAction } from "./actions";

const initialState: FormState = { values: emptyValues, errors: {} };

/** 123456789 → 123-45-6789 while typing. */
function formatSsn(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 9);
  return [digits.slice(0, 3), digits.slice(3, 5), digits.slice(5)].filter(Boolean).join("-");
}

/** 25000 → 25,000 on blur; leaves invalid input untouched so the error makes sense. */
function formatAmount(value: string) {
  const amount = Number(value.replace(/[$,\s]/g, ""));
  return value && Number.isFinite(amount) ? amount.toLocaleString("en-US", { maximumFractionDigits: 2 }) : value;
}

export function ApplicationForm() {
  const [state, formAction, pending] = useActionState(submitApplicationAction, initialState);
  const { values, errors } = state;
  const formRef = useRef<HTMLFormElement>(null);

  // Move focus to the first invalid field after a failed submit.
  useEffect(() => {
    formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus();
  }, [state]);

  return (
    <form ref={formRef} action={formAction} noValidate className="space-y-8">
      {state.message && (
        <div role="alert" className="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          {state.message}
        </div>
      )}

      <fieldset className="space-y-4">
        <legend className="mb-2 text-base font-semibold text-slate-900">About you</legend>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field id="firstName" label="First name" error={errors.firstName}>
            {(props) => <input {...props} className={inputClass} autoComplete="given-name" defaultValue={values.firstName} />}
          </Field>
          <Field id="lastName" label="Last name" error={errors.lastName}>
            {(props) => <input {...props} className={inputClass} autoComplete="family-name" defaultValue={values.lastName} />}
          </Field>
        </div>
        <Field id="ssn" label="Social Security Number" error={errors.ssn} hint="Used to verify your identity. We never display it in full.">
          {(props) => (
            <input
              {...props}
              className={`${inputClass} sm:max-w-56`}
              inputMode="numeric"
              autoComplete="off"
              placeholder="123-45-6789"
              maxLength={11}
              defaultValue={values.ssn}
              onInput={(event) => (event.currentTarget.value = formatSsn(event.currentTarget.value))}
            />
          )}
        </Field>
      </fieldset>

      <fieldset className="space-y-4">
        <legend className="mb-2 text-base font-semibold text-slate-900">Address</legend>
        <Field id="street" label="Street address" error={errors.street}>
          {(props) => <input {...props} className={inputClass} autoComplete="street-address" defaultValue={values.street} />}
        </Field>
        <div className="grid gap-4 sm:grid-cols-[2fr_1.5fr_1fr]">
          <Field id="city" label="City" error={errors.city}>
            {(props) => <input {...props} className={inputClass} autoComplete="address-level2" defaultValue={values.city} />}
          </Field>
          <Field id="state" label="State" error={errors.state}>
            {(props) => (
              // key: React won't re-apply defaultValue to a <select> after a failed submit without a remount
              <select {...props} key={values.state} className={inputClass} autoComplete="address-level1" defaultValue={values.state}>
                <option value="" disabled>
                  Select…
                </option>
                {US_STATES.map(([code, name]) => (
                  <option key={code} value={code}>
                    {name}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field id="zipCode" label="ZIP code" error={errors.zipCode}>
            {(props) => (
              <input {...props} className={inputClass} inputMode="numeric" autoComplete="postal-code" maxLength={10} defaultValue={values.zipCode} />
            )}
          </Field>
        </div>
      </fieldset>

      <fieldset className="space-y-4">
        <legend className="mb-2 text-base font-semibold text-slate-900">Your loan</legend>
        <Field id="companyName" label="Company name" error={errors.companyName}>
          {(props) => <input {...props} className={inputClass} autoComplete="organization" defaultValue={values.companyName} />}
        </Field>
        <Field id="requestedAmount" label="Requested amount" error={errors.requestedAmount}>
          {(props) => (
            <div className="relative sm:max-w-56">
              <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-slate-500">$</span>
              <input
                {...props}
                className={`${inputClass} pl-7`}
                inputMode="decimal"
                placeholder="25,000"
                defaultValue={values.requestedAmount}
                onBlur={(event) => (event.currentTarget.value = formatAmount(event.currentTarget.value))}
              />
            </div>
          )}
        </Field>
      </fieldset>

      <button
        type="submit"
        disabled={pending}
        className="w-full rounded-lg bg-blue-700 px-4 py-3 font-semibold text-white shadow-sm hover:bg-blue-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:cursor-wait disabled:opacity-70 sm:w-auto"
      >
        {pending ? "Submitting…" : "Submit application"}
      </button>
    </form>
  );
}
