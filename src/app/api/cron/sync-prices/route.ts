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

  try {
    const products = await syncCatalog();

    const { error } = await supabase.from("products").upsert(
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

    if (error) {
      return NextResponse.json({ error: error.message }, { status: 500 });
    }

    return NextResponse.json({ synced: products.length });
  } catch (err) {
    const stack = err instanceof Error ? err.stack : String(err);
    return NextResponse.json({ error: "sync_threw", stack }, { status: 500 });
  }
}
