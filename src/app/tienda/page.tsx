import type { Metadata } from "next";
import Link from "next/link";
import { AppWindow, ArrowLeft, FileSpreadsheet, KeyRound, ShieldCheck } from "lucide-react";
import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";
import { SectionHeading } from "@/components/SectionHeading";
import { site } from "@/lib/site";
import { getSupabaseAdmin } from "@/lib/supabase";

export const metadata: Metadata = {
  title: "Tienda",
  description: "Licencias originales de Windows y Office para Vallenar."
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

const categoryIcon = { windows: AppWindow, office: FileSpreadsheet } as const;

function getProductBlurb(product: Product): string {
  const name = product.name.toLowerCase();

  if (product.category === "windows") {
    if (name.includes("enterprise")) return "Edicion empresarial: BitLocker, Hyper-V y gestion avanzada de seguridad.";
    if (name.includes("home")) return "Edicion hogar: funciones esenciales para uso personal.";
    return "Incluye BitLocker, Hyper-V y herramientas para uso profesional.";
  }

  if (name.includes("365")) {
    return "Suscripcion con Word, Excel, PowerPoint, Outlook, OneDrive en la nube y actualizaciones constantes.";
  }
  if (name.includes("home & business") || name.includes("home and business")) {
    return "Word, Excel, PowerPoint y Outlook. Licencia perpetua con derecho a uso comercial.";
  }
  if (name.includes("professional plus") || name.includes("standard")) {
    return "Word, Excel, PowerPoint, Outlook, Publisher y Access en una sola licencia perpetua.";
  }
  return "Suite ofimatica: Word, Excel, PowerPoint y mas, licencia perpetua.";
}

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
              Licencias originales para tu equipo.
            </h1>
            <p className="mt-5 text-lg leading-8 text-slate-700">
              Windows incluye la clave de activacion lista para usar (la instalacion o
              formateo se cotiza aparte). Office incluye instalacion y activacion en tu
              equipo. Coordinamos todo por WhatsApp.
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
                <SectionHeading eyebrow="Catalogo" title="Windows" description="Clave de activacion original de Windows para tu equipo." />
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
      {products.map((product) => {
        const CategoryIcon = categoryIcon[product.category];
        return (
          <article key={product.slug} className="flex flex-col rounded border border-slate-200 bg-white p-6 shadow-sm">
            <div className="mb-3 flex items-center gap-3">
              <span className="grid h-10 w-10 shrink-0 place-items-center rounded bg-circuit text-graphite">
                <CategoryIcon size={20} />
              </span>
              <span className="text-xs font-bold uppercase tracking-[0.12em] text-circuit">
                {product.subcategory ?? product.category}
              </span>
            </div>
            <h3 className="text-lg font-bold text-graphite">{product.name}</h3>
            {product.description && (
              <p className="mt-1 text-xs font-semibold uppercase tracking-wide text-slate-400">
                {product.description}
              </p>
            )}
            <p className="mt-2 text-sm leading-6 text-slate-600">{getProductBlurb(product)}</p>
            <div className="mt-4 flex items-center gap-2 text-sm font-semibold text-slate-700">
              {product.category === "windows" ? (
                <>
                  <KeyRound size={16} className="text-signal" />
                  Incluye clave de activacion
                </>
              ) : (
                <>
                  <ShieldCheck size={16} className="text-signal" />
                  Incluye instalacion y activacion
                </>
              )}
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
        );
      })}
    </div>
  );
}
