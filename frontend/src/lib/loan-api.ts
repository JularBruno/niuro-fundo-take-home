// Server-side only: imported by the Server Action, never by client components (keeps API_URL off the client).
import { type ApplicationValues, type Field, type FieldErrors, parseAmount } from "./application";

const API_URL = process.env.API_URL ?? "http://localhost:5009";

export type DenialReason = "StateNotSupported" | "SsnBlacklisted";

export type SubmitResult =
  | { kind: "approved"; applicationId: string; isReturningCustomer: boolean }
  | { kind: "denied"; reason: DenialReason }
  | { kind: "invalid"; errors: FieldErrors };

// API validation keys ("Address.State", "Ssn") → form field names.
const apiFieldToFormField: Record<string, Field> = {
  firstname: "firstName", lastname: "lastName", companyname: "companyName",
  "address.street": "street", "address.city": "city", "address.state": "state", "address.zipcode": "zipCode",
  ssn: "ssn", requestedamount: "requestedAmount",
};

/** Throws when the API is unreachable or fails unexpectedly. */
export async function submitApplication(values: ApplicationValues): Promise<SubmitResult> {
  const response = await fetch(`${API_URL}/api/applications`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      firstName: values.firstName,
      lastName: values.lastName,
      companyName: values.companyName,
      address: { street: values.street, city: values.city, state: values.state, zipCode: values.zipCode },
      ssn: values.ssn,
      requestedAmount: parseAmount(values.requestedAmount),
    }),
  });

  if (response.status === 400) {
    const problem: { errors?: Record<string, string[]> } = await response.json();
    const errors: FieldErrors = {};
    for (const [key, messages] of Object.entries(problem.errors ?? {})) {
      const field = apiFieldToFormField[key.toLowerCase()];
      if (field) errors[field] = messages[0];
    }
    return { kind: "invalid", errors };
  }

  if (!response.ok) throw new Error(`Loan API responded ${response.status}`);

  const body: { approved: boolean; applicationId?: string; isReturningCustomer?: boolean; denialReason?: DenialReason } =
    await response.json();

  return body.approved
    ? { kind: "approved", applicationId: body.applicationId!, isReturningCustomer: body.isReturningCustomer ?? false }
    : { kind: "denied", reason: body.denialReason! };
}
