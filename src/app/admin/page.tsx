import Link from "next/link";
import { requireAdmin } from "@/lib/admin-auth";
import { getDb } from "@/lib/db";
import { formatClp, listPhysicalProducts } from "@/lib/store";
import { AdminFlash } from "@/components/AdminFlash";
import { StockBadge } from "@/components/StockBadge";

export default async function AdminHomePage({
  searchParams
}: {
  searchParams: Promise<{ ok?: string; error?: string }>;
}) {
  await requireAdmin();
  const { ok, error } = await searchParams;

  const products = await listPhysicalProducts({ includeInactive: true });
  const db = getDb();
  const digital = db
    ? (await db.query<{ count: string }>("SELECT count(*) FROM products WHERE kind = 'digital' AND active")).rows[0].count
    : "0";

  return (
    <div>
      <AdminFlash ok={ok} error={error} />

      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-black">Productos tecnológicos</h1>
          <p className="text-sm text-slate-600">Aquí controlas precio y stock de los productos físicos.</p>
        </div>
        <Link
          href="/admin/productos/nuevo"
          className="rounded bg-circuit px-4 py-2.5 text-sm font-bold text-graphite transition hover:bg-signal"
        >
          Agregar producto
        </Link>
      </div>

      <div className="overflow-x-auto rounded border border-slate-200 bg-white">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-4 py-3">Producto</th>
              <th className="px-4 py-3">Categoría</th>
              <th className="px-4 py-3">Precio</th>
              <th className="px-4 py-3">Stock</th>
              <th className="px-4 py-3">Estado</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {products.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-slate-500">
                  Aún no hay productos tecnológicos.
                </td>
              </tr>
            ) : (
              products.map((product) => (
                <tr key={product.slug}>
                  <td className="px-4 py-3 font-semibold">{product.name}</td>
                  <td className="px-4 py-3 text-slate-600">{product.category}</td>
                  <td className="px-4 py-3">{formatClp(product.our_price)}</td>
                  <td className="px-4 py-3">
                    <span className="mr-2 font-bold">{product.stock}</span>
                    <StockBadge stock={product.stock} />
                  </td>
                  <td className="px-4 py-3 text-slate-600">{product.active ? "Publicado" : "Oculto"}</td>
                  <td className="whitespace-nowrap px-4 py-3 text-right">
                    <Link
                      href={`/admin/ventas?producto=${product.slug}`}
                      className="mr-3 font-semibold text-emerald-700 hover:underline"
                    >
                      Vender
                    </Link>
                    <Link href={`/admin/productos/${product.slug}`} className="font-semibold text-sky-700 hover:underline">
                      Editar
                    </Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <p className="mt-6 text-sm text-slate-600">
        Las licencias digitales ({digital} publicadas) se sincronizan solas desde el proveedor y no llevan control de stock.
      </p>
    </div>
  );
}
