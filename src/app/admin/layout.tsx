import type { Metadata } from "next";
import Link from "next/link";
import { getSession } from "@/lib/admin-auth";
import { logoutAction } from "@/app/admin/actions";

export const metadata: Metadata = {
  title: "Panel",
  robots: { index: false, follow: false }
};

export const dynamic = "force-dynamic";

export default async function AdminLayout({ children }: { children: React.ReactNode }) {
  const session = await getSession();

  return (
    <div className="min-h-screen bg-panel text-graphite">
      <header className="border-b border-slate-200 bg-graphite text-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
          <Link href="/admin" className="font-bold">
            Tecnotics · Panel
          </Link>
          {session ? (
            <nav className="flex flex-wrap items-center gap-1 text-sm">
              <Link href="/admin" className="rounded px-3 py-2 hover:bg-white/10">
                Productos
              </Link>
              <Link href="/admin/ventas" className="rounded px-3 py-2 hover:bg-white/10">
                Ventas
              </Link>
              <Link href="/tienda" className="rounded px-3 py-2 text-white/70 hover:bg-white/10">
                Ver tienda
              </Link>
              <form action={logoutAction}>
                <button type="submit" className="rounded px-3 py-2 text-white/70 hover:bg-white/10">
                  Cerrar sesión
                </button>
              </form>
            </nav>
          ) : null}
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">{children}</main>
    </div>
  );
}
