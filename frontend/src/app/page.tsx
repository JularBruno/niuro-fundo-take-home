import { Card } from "@/components/card";
import { ApplicationForm } from "./application-form";

export default function HomePage() {
  return (
    <Card>
      <h1 className="text-3xl font-bold tracking-tight text-emerald-900">Apply for a business loan</h1>
      <p className="mt-2 mb-8 text-slate-600">
        It takes about two minutes and you&apos;ll get a decision right away. Already applied? Use the same SSN and
        we&apos;ll update your existing application.
      </p>
      <ApplicationForm />
    </Card>
  );
}
