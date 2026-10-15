import { getDb } from "@/lib/db";

export type Kind = "digital" | "fisico";
export type StockState = "disponible" | "ultimas" | "agotado";

export type StoreProduct = {
  slug: string;
  kind: Kind;
  category: string;
  name: string;
  description: string | null;
  our_price: number;
  stock: number | null;
  active: boolean;
  image: string | null;
  detail_path: string | null;
};

export type Sale = {
  id: number;
  product_slug: string;
  product_name: string;
  quantity: number;
  unit_price: number;
  total: number;
  note: string | null;
  created_at: Date;
  voided_at: Date | null;
};

export const LOW_STOCK_THRESHOLD = 3;

const PRODUCT_COLUMNS =
  "slug, kind, category, name, description, our_price, stock, active, image, detail_path";

const clpFormatter = new Intl.NumberFormat("es-CL", {
  style: "currency",
  currency: "CLP",
  maximumFractionDigits: 0
});

export function formatClp(value: number) {
  return clpFormatter.format(value);
}

export function formatDate(value: Date) {
  return new Intl.DateTimeFormat("es-CL", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "America/Santiago"
  }).format(value);
}

export function stockState(stock: number | null): StockState | null {
  if (stock === null) return null;
  if (stock <= 0) return "agotado";
  if (stock <= LOW_STOCK_THRESHOLD) return "ultimas";
  return "disponible";
}

export function slugify(text: string) {
  return text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);
}

export async function listPhysicalProducts(options: { includeInactive?: boolean } = {}) {
  const db = getDb();
  if (!db) return [];
  const { rows } = await db.query<StoreProduct>(
    `SELECT ${PRODUCT_COLUMNS} FROM products
     WHERE kind = 'fisico' ${options.includeInactive ? "" : "AND active"}
     ORDER BY category, name`
  );
  return rows;
}

export async function getProduct(slug: string) {
  const db = getDb();
  if (!db) return null;
  const { rows } = await db.query<StoreProduct>(
    `SELECT ${PRODUCT_COLUMNS} FROM products WHERE slug = $1`,
    [slug]
  );
  return rows[0] ?? null;
}

export async function listPhysicalCategories() {
  const db = getDb();
  if (!db) return [];
  const { rows } = await db.query<{ category: string }>(
    "SELECT DISTINCT category FROM products WHERE kind = 'fisico' ORDER BY category"
  );
  return rows.map((row) => row.category);
}

export type ProductInput = {
  slug?: string;
  name: string;
  category: string;
  price: number;
  stock: number;
  description: string | null;
  image: string | null;
  detailPath: string | null;
  active: boolean;
};

export async function saveProduct(input: ProductInput): Promise<{ ok: true; slug: string } | { ok: false; error: string }> {
  const db = getDb();
  if (!db) return { ok: false, error: "Base de datos no disponible" };

  if (input.slug) {
    const result = await db.query(
      `UPDATE products
       SET name = $2, category = $3, our_price = $4, stock = $5, description = $6,
           image = $7, detail_path = $8, active = $9, updated_at = now()
       WHERE slug = $1 AND kind = 'fisico'`,
      [input.slug, input.name, input.category, input.price, input.stock, input.description, input.image, input.detailPath, input.active]
    );
    return result.rowCount ? { ok: true, slug: input.slug } : { ok: false, error: "Producto no encontrado" };
  }

  const slug = slugify(input.name);
  if (!slug) return { ok: false, error: "El nombre no es válido" };
  const result = await db.query(
    `INSERT INTO products (slug, kind, category, name, description, our_price, stock, image, detail_path, active)
     VALUES ($1, 'fisico', $2, $3, $4, $5, $6, $7, $8, $9)
     ON CONFLICT (slug) DO NOTHING`,
    [slug, input.category, input.name, input.description, input.price, input.stock, input.image, input.detailPath, input.active]
  );
  return result.rowCount ? { ok: true, slug } : { ok: false, error: "Ya existe un producto con ese nombre" };
}

export async function registerSale(params: {
  slug: string;
  quantity: number;
  unitPrice: number | null;
  note: string | null;
  createdBy: string;
}): Promise<{ ok: true } | { ok: false; error: string }> {
  const db = getDb();
  if (!db) return { ok: false, error: "Base de datos no disponible" };

  const client = await db.connect();
  try {
    await client.query("BEGIN");
    const updated = await client.query<{ our_price: number }>(
      `UPDATE products SET stock = stock - $2, updated_at = now()
       WHERE slug = $1 AND kind = 'fisico' AND stock >= $2
       RETURNING our_price`,
      [params.slug, params.quantity]
    );
    if (updated.rowCount === 0) {
      await client.query("ROLLBACK");
      return { ok: false, error: "No hay stock suficiente para esa venta" };
    }
    await client.query(
      `INSERT INTO sales (product_slug, quantity, unit_price, note, created_by)
       VALUES ($1, $2, $3, $4, $5)`,
      [params.slug, params.quantity, params.unitPrice ?? updated.rows[0].our_price, params.note, params.createdBy]
    );
    await client.query("COMMIT");
    return { ok: true };
  } catch (error) {
    await client.query("ROLLBACK");
    return { ok: false, error: error instanceof Error ? error.message : "Error al registrar la venta" };
  } finally {
    client.release();
  }
}

export async function voidSale(id: number): Promise<{ ok: true } | { ok: false; error: string }> {
  const db = getDb();
  if (!db) return { ok: false, error: "Base de datos no disponible" };

  const client = await db.connect();
  try {
    await client.query("BEGIN");
    const voided = await client.query<{ product_slug: string; quantity: number }>(
      `UPDATE sales SET voided_at = now()
       WHERE id = $1 AND voided_at IS NULL
       RETURNING product_slug, quantity`,
      [id]
    );
    if (voided.rowCount === 0) {
      await client.query("ROLLBACK");
      return { ok: false, error: "La venta no existe o ya estaba anulada" };
    }
    await client.query(
      "UPDATE products SET stock = stock + $2, updated_at = now() WHERE slug = $1",
      [voided.rows[0].product_slug, voided.rows[0].quantity]
    );
    await client.query("COMMIT");
    return { ok: true };
  } catch (error) {
    await client.query("ROLLBACK");
    return { ok: false, error: error instanceof Error ? error.message : "Error al anular la venta" };
  } finally {
    client.release();
  }
}

export async function listSales(limit = 50) {
  const db = getDb();
  if (!db) return [];
  const { rows } = await db.query<Sale>(
    `SELECT s.id::int AS id, s.product_slug, p.name AS product_name, s.quantity, s.unit_price,
            s.total, s.note, s.created_at, s.voided_at
     FROM sales s JOIN products p ON p.slug = s.product_slug
     ORDER BY s.created_at DESC
     LIMIT $1`,
    [limit]
  );
  return rows;
}
