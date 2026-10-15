import { requireAdmin } from "@/lib/admin-auth";
import { changePasswordAction } from "@/app/admin/actions";
import { AdminFlash } from "@/components/AdminFlash";

const field = "rounded border border-slate-300 px-3 py-2 font-normal";

export default async function ChangePasswordPage({
  searchParams
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  await requireAdmin({ allowPasswordChange: true });
  const { error } = await searchParams;

  return (
    <div className="mx-auto max-w-sm rounded border border-slate-200 bg-white p-6 shadow-sm">
      <h1 className="text-2xl font-black">Cambiar contraseña</h1>
      <p className="mt-1 text-sm text-slate-600">
        Elige una contraseña nueva de al menos 10 caracteres para seguir usando el panel.
      </p>
      <div className="mt-6">
        <AdminFlash error={error} />
      </div>
      <form action={changePasswordAction} className="grid gap-4">
        <label className="grid gap-1 text-sm font-semibold">
          Contraseña actual
          <input name="current" type="password" required autoComplete="current-password" className={field} />
        </label>
        <label className="grid gap-1 text-sm font-semibold">
          Contraseña nueva
          <input name="next" type="password" required minLength={10} autoComplete="new-password" className={field} />
        </label>
        <label className="grid gap-1 text-sm font-semibold">
          Repite la contraseña nueva
          <input name="confirm" type="password" required minLength={10} autoComplete="new-password" className={field} />
        </label>
        <button
          type="submit"
          className="rounded bg-circuit px-4 py-2.5 font-bold text-graphite transition hover:bg-signal"
        >
          Guardar contraseña
        </button>
      </form>
    </div>
  );
}
