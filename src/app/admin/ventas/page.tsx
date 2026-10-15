import { requireAdmin } from "@/lib/admin-auth";
import { formatClp, formatDate, listPhysicalProducts, listSales } from "@/lib/store";
import { registerSaleAction, voidSaleAction } from "@/app/admin/actions";
import { AdminFlash } from "@/components/AdminFlash";

const field = "rounded border border-slate-300 px-3 py-2 font-normal";

export default async function SalesPage({
  searchParams
}: {
  searchParams: Promise<{ ok?: string; error?: string; producto?: string }>;
}) {
  await requireAdmin();
  const { ok, error, producto } = await searchParams;

  const [products, sales] = await Promise.all([listPhysicalProducts({ includeInactive: true }), listSales(50)]);

  return (
    <div className="grid gap-8">
      <div>
        <AdminFlash ok={ok} error={error} />
        <h1 className="text-2xl font-black">Registrar venta</h1>
        <p className="mb-4 text-sm text-slate-600">Al guardar, el stock del producto baja automáticamente.</p>

        <form action={registerSaleAction} className="grid gap-4 rounded border border-slate-200 bg-white p-6 sm:grid-cols-2">
          <label className="grid gap-1 text-sm font-semibold sm:col-span-2">
            Producto
            <select name="slug" required defaultValue={producto ?? ""} className={field}>
              <option value="" disabled>
                Elige un producto
              </option>
              {products.map((product) => (
                <option key={product.slug} value={product.slug} disabled={product.stock === 0}>
                  {product.name} · {formatClp(product.our_price)} · stock {product.stock}
                </option>
              ))}
            </select>
          </label>
          <label className="grid gap-1 text-sm font-semibold">
            Cantidad
            <input name="quantity" required inputMode="numeric" defaultValue={1} className={field} />
          </label>
          <label className="grid gap-1 text-sm font-semibold">
            Precio por unidad (opcional)
            <input name="unit_price" inputMode="numeric" placeholder="Vacío = precio actual" className={field} />
          </label>
          <label className="grid gap-1 text-sm font-semibold sm:col-span-2">
            Nota (cliente, forma de pago…)
            <input name="note" className={field} />
          </label>
          <button
            type="submit"
            className="w-fit rounded bg-circuit px-6 py-2.5 font-bold text-graphite transition hover:bg-signal"
          >
            Registrar venta
          </button>
        </form>
      </div>

      <div>
        <h2 className="mb-3 text-xl font-black">Historial</h2>
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Fecha</th>
                <th className="px-4 py-3">Producto</th>
                <th className="px-4 py-3">Cant.</th>
                <th className="px-4 py-3">Total</th>
                <th className="px-4 py-3">Nota</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {sales.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-slate-500">
                    Todavía no hay ventas registradas.
                  </td>
                </tr>
              ) : (
                sales.map((sale) => (
                  <tr key={sale.id} className={sale.voided_at ? "text-slate-400 line-through" : ""}>
                    <td className="whitespace-nowrap px-4 py-3">{formatDate(sale.created_at)}</td>
                    <td className="px-4 py-3 font-semibold">{sale.product_name}</td>
                    <td className="px-4 py-3">{sale.quantity}</td>
                    <td className="px-4 py-3">{formatClp(sale.total)}</td>
                    <td className="px-4 py-3">{sale.note}</td>
                    <td className="px-4 py-3 text-right">
                      {sale.voided_at ? (
                        <span className="text-xs font-bold uppercase no-underline">Anulada</span>
                      ) : (
                        <form action={voidSaleAction}>
                          <input type="hidden" name="id" value={sale.id} />
                          <button type="submit" className="font-semibold text-red-700 hover:underline">
                            Anular
                          </button>
                        </form>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        <p className="mt-3 text-xs text-slate-500">
          Anular una venta devuelve las unidades al stock.
        </p>
      </div>
    </div>
  );
}
