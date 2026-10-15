import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { AppWindow, ArrowLeft, ArrowRight, FileSpreadsheet, KeyRound, ShieldCheck } from "lucide-react";
import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";
import { SectionHeading } from "@/components/SectionHeading";
import { StockBadge } from "@/components/StockBadge";
import { site } from "@/lib/site";
import { getDb } from "@/lib/db";
import { listPhysicalProducts, type StoreProduct } from "@/lib/store";

export const metadata: Metadata = {
  title: "Tienda",
  description: "Productos tecnológicos y licencias originales de Windows y Office, en Vallenar."
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
  const db = getDb();
  if (!db) return [];

  const { rows } = await db.query<Product>(
    `SELECT slug, category, subcategory, name, description, our_price
     FROM products
     WHERE kind = 'digital' AND active
     ORDER BY category ASC, our_price ASC`
  );

  return rows;
}

const priceFormatter = new Intl.NumberFormat("es-CL", {
  style: "currency",
  currency: "CLP",
  maximumFractionDigits: 0
});

const categoryIcon = { windows: AppWindow, office: FileSpreadsheet } as const;

function getFamilyLabel(product: Product): string {
  const match = product.name.match(/Windows\s*(?:11|10|8\.1)|Office\s*(?:2024|2021|2019|2016|2013|2010)|Microsoft\s*365/i);
  return match ? match[0] : product.category === "windows" ? "Windows" : "Office";
}

function getEditionLabel(name: string): string {
  const n = name.toLowerCase();
  if (n.includes("enterprise")) return "Enterprise";
  if (n.includes("professional plus")) return "Professional Plus";
  if (n.includes("home & business") || n.includes("home and business")) return "Home & Business";
  if (n.includes("standard")) return "Standard";
  if (n.includes("professional")) return "Professional";
  if (n.includes("home")) return "Home";
  if (n.includes("365")) return "Suscripcion";
  return "";
}

function getCoverColorClass(name: string): string {
  const n = name.toLowerCase();
  if (n.includes("enterprise")) return "bg-graphite text-white";
  if (n.includes("365")) return "bg-signal text-graphite";
  if (n.includes("home") && !n.includes("business")) return "bg-panel text-graphite border border-slate-200";
  return "bg-circuit text-graphite";
}

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
  const [products, physical] = await Promise.all([getProducts(), listPhysicalProducts()]);
  const windows = products.filter((product) => product.category === "windows");
  const office = products.filter((product) => product.category === "office");
  const physicalGroups = Array.from(new Set(physical.map((product) => product.category))).map((category) => ({
    category,
    items: physical.filter((product) => product.category === category)
  }));

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
              Tecnologia y licencias para tu equipo.
            </h1>
            <p className="mt-5 text-lg leading-8 text-slate-700">
              Productos tecnologicos para tu PC y licencias digitales originales. En las licencias,
              Windows incluye la clave de activacion lista para usar (la instalacion o
              formateo se cotiza aparte). Office incluye instalacion y activacion en tu
              equipo. Coordinamos todo por WhatsApp.
            </p>
          </div>
        </div>
      </section>

      {physicalGroups.map(({ category, items }) => (
        <section key={category} className="border-b border-slate-200 bg-white py-20">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <SectionHeading eyebrow="Productos tecnologicos" title={category} />
            <PhysicalGrid products={items} />
          </div>
        </section>
      ))}

      {products.length === 0 && physical.length === 0 ? (
        <section className="py-20">
          <div className="mx-auto max-w-7xl px-4 text-center text-slate-600 sm:px-6 lg:px-8">
            Catalogo en preparacion. Escribenos por WhatsApp para cotizar.
          </div>
        </section>
      ) : (
        <>
          {windows.length > 0 && (
            <section className="py-20">
              <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
                <SectionHeading eyebrow="Licencias digitales" title="Windows" description="Clave de activacion original de Windows para tu equipo." />
                <ProductGrid products={windows} />
              </div>
            </section>
          )}
          {office.length > 0 && (
            <section className="bg-panel py-20">
              <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
                <SectionHeading eyebrow="Licencias digitales" title="Office" description="Office y Microsoft 365, instalados y activados en tu equipo." />
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

function PhysicalGrid({ products }: { products: StoreProduct[] }) {
  return (
    <div className="flex flex-wrap justify-center gap-4">
      {products.map((product) => {
        const soldOut = product.stock === 0;
        const waMessage = encodeURIComponent(
          soldOut
            ? `Hola Tecnotics, quiero que me avisen cuando vuelva el ${product.name}.`
            : `Hola Tecnotics, me interesa el ${product.name}. ¿Me das mas informacion?`
        );
        return (
          <article
            key={product.slug}
            className="flex w-full flex-col overflow-hidden rounded border border-slate-200 bg-white shadow-sm sm:w-[calc(50%-0.5rem)] lg:w-[calc(33.333%-0.7rem)]"
          >
            <div className={`relative bg-graphite ${product.video ? "aspect-video" : "aspect-[4/3]"}`}>
              {product.image ? (
                <Image
                  src={product.image}
                  alt={product.name}
                  fill
                  sizes="(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
                  className={`object-cover ${soldOut ? "opacity-60" : ""}`}
                />
              ) : null}
              {product.video ? (
                <video
                  className={`absolute inset-0 h-full w-full object-cover motion-reduce:hidden ${soldOut ? "opacity-60" : ""}`}
                  src={product.video}
                  poster={product.image ?? undefined}
                  autoPlay
                  muted
                  loop
                  playsInline
                  preload="metadata"
                  aria-hidden="true"
                />
              ) : null}
            </div>
            <div className="flex flex-1 flex-col p-6">
              <div className="mb-3">
                <StockBadge stock={product.stock} />
              </div>
              <h3 className="text-lg font-bold text-graphite">{product.name}</h3>
              {product.description ? (
                <p className="mt-2 text-sm leading-6 text-slate-600">{product.description}</p>
              ) : null}
              <div className="mt-auto flex items-center justify-between gap-3 pt-5">
                <span className="text-2xl font-black text-graphite">{priceFormatter.format(product.our_price)}</span>
                {product.detail_path ? (
                  <Link
                    href={product.detail_path}
                    className="inline-flex items-center justify-center gap-2 rounded bg-circuit px-4 py-2 text-sm font-bold text-graphite transition hover:bg-signal"
                  >
                    Ver producto
                    <ArrowRight size={16} />
                  </Link>
                ) : (
                  <a
                    href={`${site.whatsappUrl}?text=${waMessage}`}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center justify-center rounded bg-circuit px-4 py-2 text-sm font-bold text-graphite transition hover:bg-signal"
                  >
                    {soldOut ? "Avisame" : "Solicitar"}
                  </a>
                )}
              </div>
            </div>
          </article>
        );
      })}
    </div>
  );
}

function ProductGrid({ products }: { products: Product[] }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {products.map((product) => {
        const CategoryIcon = categoryIcon[product.category];
        const edition = getEditionLabel(product.name);
        return (
          <article key={product.slug} className="flex flex-col overflow-hidden rounded border border-slate-200 bg-white shadow-sm">
            <div className={`flex h-32 flex-col items-center justify-center gap-1 ${getCoverColorClass(product.name)}`}>
              <CategoryIcon size={30} />
              <span className="text-base font-black">{getFamilyLabel(product)}</span>
              {edition && (
                <span className="text-xs font-bold uppercase tracking-[0.1em] opacity-80">{edition}</span>
              )}
            </div>
            <div className="flex flex-1 flex-col p-6">
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
            </div>
          </article>
        );
      })}
    </div>
  );
}
