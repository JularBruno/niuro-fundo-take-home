"use server";

import { redirect } from "next/navigation";
import { type ApplicationValues, type FieldErrors, parseAmount, readValues, validate } from "@/lib/application";
import { type SubmitResult, submitApplication } from "@/lib/loan-api";

export type FormState = {
  values: ApplicationValues;
  errors: FieldErrors;
  message?: string;
};

export async function submitApplicationAction(_previous: FormState, formData: FormData): Promise<FormState> {
  const values = readValues(formData);

  const errors = validate(values);
  if (Object.keys(errors).length > 0) return { values, errors };

  let result: SubmitResult;
  try {
    result = await submitApplication(values);
  } catch {
    return {
      values,
      errors: {},
      message: "We couldn't reach our servers. Your information is still here — please try again in a moment.",
    };
  }

  if (result.kind === "invalid") {
    return { values, errors: result.errors, message: "Please fix the highlighted fields." };
  }

  // redirect() throws to navigate, so it stays outside the try/catch above.
  if (result.kind === "denied") redirect(`/denied?reason=${result.reason}`);

  const query = new URLSearchParams({
    id: result.applicationId,
    amount: String(parseAmount(values.requestedAmount)),
    ...(result.isReturningCustomer && { returning: "1" }),
  });
  redirect(`/approved?${query}`);
}
