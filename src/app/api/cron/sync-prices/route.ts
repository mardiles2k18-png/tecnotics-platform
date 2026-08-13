import { NextResponse } from "next/server";
import { syncCatalog } from "@/lib/digitalcode";
import { getSupabaseAdmin } from "@/lib/supabase";

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

  const supabase = getSupabaseAdmin();
  if (!supabase) {
    return NextResponse.json({ error: "supabase_not_configured" }, { status: 500 });
  }

  const products = await syncCatalog();

  const { error: upsertError } = await supabase.from("products").upsert(
    products.map((product) => ({
      slug: product.slug,
      category: product.category,
      subcategory: product.subcategory,
      name: product.name,
      description: product.description,
      source_price: product.sourcePrice,
      our_price: product.ourPrice,
      updated_at: new Date().toISOString()
    })),
    { onConflict: "slug" }
  );

  if (upsertError) {
    return NextResponse.json({ error: upsertError.message }, { status: 500 });
  }

  const currentSlugs = products.map((product) => `"${product.slug}"`).join(",");
  const { error: deleteError } = await supabase
    .from("products")
    .delete()
    .in("category", ["windows", "office"])
    .not("slug", "in", `(${currentSlugs})`);

  if (deleteError) {
    return NextResponse.json({ error: deleteError.message }, { status: 500 });
  }

  return NextResponse.json({ synced: products.length });
}
