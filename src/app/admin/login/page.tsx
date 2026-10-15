import { redirect } from "next/navigation";
import { getSession } from "@/lib/admin-auth";
import { loginAction } from "@/app/admin/actions";
import { AdminFlash } from "@/components/AdminFlash";

export default async function LoginPage({
  searchParams
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  if (await getSession()) redirect("/admin");
  const { error } = await searchParams;

  return (
    <div className="mx-auto mt-10 max-w-sm rounded border border-slate-200 bg-white p-6 shadow-sm">
      <h1 className="text-2xl font-black">Ingresar al panel</h1>
      <p className="mt-1 text-sm text-slate-600">Solo para administradores de Tecnotics.</p>
      <div className="mt-6">
        <AdminFlash error={error} />
      </div>
      <form action={loginAction} className="grid gap-4">
        <label className="grid gap-1 text-sm font-semibold">
          Correo
          <input
            name="email"
            type="email"
            required
            autoComplete="username"
            className="rounded border border-slate-300 px-3 py-2 font-normal"
          />
        </label>
        <label className="grid gap-1 text-sm font-semibold">
          Contraseña
          <input
            name="password"
            type="password"
            required
            autoComplete="current-password"
            className="rounded border border-slate-300 px-3 py-2 font-normal"
          />
        </label>
        <button
          type="submit"
          className="rounded bg-circuit px-4 py-2.5 font-bold text-graphite transition hover:bg-signal"
        >
          Entrar
        </button>
      </form>
    </div>
  );
}
