export function AdminFlash({ ok, error }: { ok?: string; error?: string }) {
  if (!ok && !error) return null;
  const isError = Boolean(error);
  return (
    <p
      role={isError ? "alert" : "status"}
      className={`mb-6 rounded border px-4 py-3 text-sm font-semibold ${
        isError ? "border-red-200 bg-red-50 text-red-800" : "border-emerald-200 bg-emerald-50 text-emerald-800"
      }`}
    >
      {(error ?? ok ?? "").slice(0, 200)}
    </p>
  );
}
