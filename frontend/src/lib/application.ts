import { isUsState } from "./us-states";

export const FIELDS = [
  "firstName", "lastName", "companyName", "street", "city", "state", "zipCode", "ssn", "requestedAmount",
] as const;

export type Field = (typeof FIELDS)[number];
export type ApplicationValues = Record<Field, string>;
export type FieldErrors = Partial<Record<Field, string>>;

export const emptyValues = Object.fromEntries(FIELDS.map((field) => [field, ""])) as ApplicationValues;

export function readValues(formData: FormData): ApplicationValues {
  return Object.fromEntries(
    FIELDS.map((field) => [field, String(formData.get(field) ?? "").trim()]),
  ) as ApplicationValues;
}

/** "25,000.50" / "$25000" → 25000.5; NaN when not a number. */
export const parseAmount = (value: string) => Number(value.replace(/[$,\s]/g, "") || NaN);

export const ssnDigits = (value: string) => value.replace(/\D/g, "");

/** Same rules as the API; the API still has the final word. */
export function validate(values: ApplicationValues): FieldErrors {
  const errors: FieldErrors = {};
  const required: [Field, string][] = [
    ["firstName", "Enter your first name."],
    ["lastName", "Enter your last name."],
    ["companyName", "Enter your company name."],
    ["street", "Enter your street address."],
    ["city", "Enter your city."],
  ];
  for (const [field, message] of required) if (!values[field]) errors[field] = message;

  if (!isUsState(values.state)) errors.state = "Select your state.";
  if (!/^\d{5}(-\d{4})?$/.test(values.zipCode)) errors.zipCode = "Enter a 5-digit ZIP code.";
  if (ssnDigits(values.ssn).length !== 9) errors.ssn = "Enter your 9-digit SSN (123-45-6789).";

  const amount = parseAmount(values.requestedAmount);
  if (!Number.isFinite(amount) || amount <= 0) errors.requestedAmount = "Enter an amount greater than $0.";

  return errors;
}
