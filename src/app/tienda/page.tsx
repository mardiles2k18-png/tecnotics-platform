import type { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft, ShieldCheck } from "lucide-react";
import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";
import { SectionHeading } from "@/components/SectionHeading";
import { site } from "@/lib/site";
import { getSupabaseAdmin } from "@/lib/supabase";

export const metadata: Metadata = {
  title: "Tienda",
  description: "Licencias de Windows y Office con instalacion incluida, en Vallenar."
};

export const dynamic = "force-dynamic";

type Product = {
  slug: string;
  category: "windows" | "office";
  subcategory: string | null;
  name: string;
  description: string | null;
  our_price: number;
};

async function getProducts(): Promise<Product[]> {
  const supabase = getSupabaseAdmin();
  if (!supabase) return [];

  const { data } = await supabase
    .from("products")
    .select("slug, category, subcategory, name, description, our_price")
    .order("category", { ascending: true })
    .order("our_price", { ascending: true });

  return data ?? [];
}

const priceFormatter = new Intl.NumberFormat("es-CL", {
  style: "currency",
  currency: "CLP",
  maximumFractionDigits: 0
});

export default async function TiendaPage() {
  const products = await getProducts();
  const windows = products.filter((product) => product.category === "windows");
  const office = products.filter((product) => product.category === "office");

  return (
    <main className="bg-white">
      <Header />
      <section className="noise border-b border-slate-200 py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <Link href="/" className="mb-8 inline-flex items-center gap-2 text-sm font-semibold text-graphite">
            <ArrowLeft size={16} />
            Volver al inicio
          </Link>
          <div className="max-w-3xl">
            <p className="mb-3 text-xs font-bold uppercase tracking-[0.18em] text-circuit">
              Tienda Tecnotics
            </p>
            <h1 className="text-4xl font-black leading-tight text-graphite sm:text-5xl">
              Licencias con instalacion incluida.
            </h1>
            <p className="mt-5 text-lg leading-8 text-slate-700">
              Licencia original de Windows u Office mas la instalacion y activacion
              en tu equipo, coordinada por WhatsApp.
            </p>
          </div>
        </div>
      </section>

      {products.length === 0 ? (
        <section className="py-20">
          <div className="mx-auto max-w-7xl px-4 text-center text-slate-600 sm:px-6 lg:px-8">
            Catalogo en preparacion. Escribenos por WhatsApp para cotizar tu licencia.
          </div>
        </section>
      ) : (
        <>
          {windows.length > 0 && (
            <section className="py-20">
              <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
                <SectionHeading eyebrow="Catalogo" title="Windows" description="Licencias perpetuas de Windows, instaladas y activadas en tu equipo." />
                <ProductGrid products={windows} />
              </div>
            </section>
          )}
          {office.length > 0 && (
            <section className="bg-panel py-20">
              <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
                <SectionHeading eyebrow="Catalogo" title="Office" description="Office y Microsoft 365, instalados y activados en tu equipo." />
                <ProductGrid products={office} />
              </div>
            </section>
          )}
        </>
      )}
      <Footer />
    </main>
  );
}

function ProductGrid({ products }: { products: Product[] }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {products.map((product) => (
        <article key={product.slug} className="flex flex-col rounded border border-slate-200 bg-white p-6 shadow-sm">
          <span className="mb-2 text-xs font-bold uppercase tracking-[0.12em] text-circuit">
            {product.subcategory ?? product.category}
          </span>
          <h3 className="text-lg font-bold text-graphite">{product.name}</h3>
          {product.description && (
            <p className="mt-2 text-sm leading-6 text-slate-600">{product.description}</p>
          )}
          <div className="mt-4 flex items-center gap-2 text-sm font-semibold text-slate-700">
            <ShieldCheck size={16} className="text-signal" />
            Incluye instalacion y activacion
          </div>
          <div className="mt-5 flex items-center justify-between">
            <span className="text-2xl font-black text-graphite">
              {priceFormatter.format(product.our_price)}
            </span>
            <a
              href={site.whatsappUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center justify-center rounded bg-circuit px-4 py-2 text-sm font-bold text-graphite transition hover:bg-signal"
            >
              Solicitar
            </a>
          </div>
        </article>
      ))}
    </div>
  );
}
