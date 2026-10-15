import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAdmin } from "@/lib/admin-auth";
import { getProduct, listPhysicalCategories } from "@/lib/store";
import { saveProductAction } from "@/app/admin/actions";
import { AdminFlash } from "@/components/AdminFlash";

const field = "rounded border border-slate-300 px-3 py-2 font-normal";

export default async function ProductFormPage({
  params,
  searchParams
}: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ error?: string }>;
}) {
  await requireAdmin();
  const { slug } = await params;
  const { error } = await searchParams;

  const isNew = slug === "nuevo";
  const product = isNew ? null : await getProduct(slug);
  if (!isNew && (!product || product.kind !== "fisico")) notFound();

  const categories = await listPhysicalCategories();

  return (
    <div className="max-w-2xl">
      <Link href="/admin" className="text-sm font-semibold text-sky-700 hover:underline">
        ← Volver a productos
      </Link>
      <h1 className="mb-6 mt-3 text-2xl font-black">{isNew ? "Agregar producto" : `Editar: ${product?.name}`}</h1>
      <AdminFlash error={error} />

      <form action={saveProductAction} className="grid gap-4 rounded border border-slate-200 bg-white p-6">
        {product ? <input type="hidden" name="slug" value={product.slug} /> : null}

        <label className="grid gap-1 text-sm font-semibold">
          Nombre
          <input name="name" required defaultValue={product?.name} className={field} />
        </label>

        <label className="grid gap-1 text-sm font-semibold">
          Categoría
          <input
            name="category"
            required
            list="categorias"
            defaultValue={product?.category}
            placeholder="Ej: Monitores de sistema"
            className={field}
          />
          <datalist id="categorias">
            {categories.map((category) => (
              <option key={category} value={category} />
            ))}
          </datalist>
          <span className="text-xs font-normal text-slate-500">
            Elige una existente o escribe una nueva para crear la categoría.
          </span>
        </label>

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="grid gap-1 text-sm font-semibold">
            Precio (CLP)
            <input name="price" required inputMode="numeric" defaultValue={product?.our_price} className={field} />
          </label>
          <label className="grid gap-1 text-sm font-semibold">
            Stock (unidades)
            <input name="stock" required inputMode="numeric" defaultValue={product?.stock ?? 0} className={field} />
          </label>
        </div>

        <label className="grid gap-1 text-sm font-semibold">
          Descripción corta
          <textarea name="description" rows={3} defaultValue={product?.description ?? ""} className={field} />
        </label>

        <label className="grid gap-1 text-sm font-semibold">
          Imagen (ruta, ej: /images/products/mi-producto.jpg)
          <input name="image" defaultValue={product?.image ?? ""} className={field} />
        </label>

        <label className="grid gap-1 text-sm font-semibold">
          Video corto para la tarjeta (opcional, ej: /videos/mi-producto.mp4)
          <input name="video" defaultValue={product?.video ?? ""} className={field} />
          <span className="text-xs font-normal text-slate-500">
            Se reproduce en bucle y sin sonido en la tienda; la imagen se usa mientras carga.
          </span>
        </label>

        <label className="grid gap-1 text-sm font-semibold">
          Página del producto (opcional, ej: /tienda/monitor-de-sistema)
          <input name="detail_path" defaultValue={product?.detail_path ?? ""} className={field} />
        </label>

        <label className="flex items-center gap-2 text-sm font-semibold">
          <input type="checkbox" name="active" defaultChecked={product ? product.active : true} />
          Publicado en la tienda
        </label>

        <button
          type="submit"
          className="w-fit rounded bg-circuit px-6 py-2.5 font-bold text-graphite transition hover:bg-signal"
        >
          Guardar
        </button>
      </form>
    </div>
  );
}
