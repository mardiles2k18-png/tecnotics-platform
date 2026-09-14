import { NextResponse } from "next/server";
import { syncCatalog } from "@/lib/digitalcode";
import { getDb } from "@/lib/db";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export const maxDuration = 60;

function isAuthorized(request: Request) {
  const secret = process.env.CRON_SYNC_SECRET;
  if (!secret) return false;

  const header = request.headers.get("authorization");
  return header === `Bearer ${secret}`;
}

export async function GET(request: Request) {
  if (!isAuthorized(request)) {
    return NextResponse.json({ error: "unauthorized" }, { status: 401 });
  }

  const db = getDb();
  if (!db) {
    return NextResponse.json({ error: "database_not_configured" }, { status: 500 });
  }

  const products = await syncCatalog();
  const updatedAt = new Date().toISOString();

  const client = await db.connect();
  try {
    await client.query("BEGIN");

    await client.query(
      `INSERT INTO products (slug, category, subcategory, name, description, source_price, our_price, updated_at)
       SELECT * FROM UNNEST(
         $1::text[], $2::text[], $3::text[], $4::text[], $5::text[], $6::int[], $7::int[], $8::timestamptz[]
       )
       ON CONFLICT (slug) DO UPDATE SET
         category = EXCLUDED.category,
         subcategory = EXCLUDED.subcategory,
         name = EXCLUDED.name,
         description = EXCLUDED.description,
         source_price = EXCLUDED.source_price,
         our_price = EXCLUDED.our_price,
         updated_at = EXCLUDED.updated_at`,
      [
        products.map((p) => p.slug),
        products.map((p) => p.category),
        products.map((p) => p.subcategory),
        products.map((p) => p.name),
        products.map((p) => p.description),
        products.map((p) => p.sourcePrice),
        products.map((p) => p.ourPrice),
        products.map(() => updatedAt)
      ]
    );

    await client.query(
      `DELETE FROM products WHERE category = ANY($1::text[]) AND NOT (slug = ANY($2::text[]))`,
      [["windows", "office"], products.map((p) => p.slug)]
    );

    await client.query("COMMIT");
  } catch (error) {
    await client.query("ROLLBACK");
    const message = error instanceof Error ? error.message : "unknown_error";
    return NextResponse.json({ error: message }, { status: 500 });
  } finally {
    client.release();
  }

  return NextResponse.json({ synced: products.length });
}
