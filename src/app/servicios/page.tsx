import type { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft, CheckCircle2 } from "lucide-react";
import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";
import { SectionHeading } from "@/components/SectionHeading";
import { services } from "@/lib/site";

export const metadata: Metadata = {
  title: "Servicios",
  description:
    "Catalogo de servicios Tecnotics: servicio tecnico, hardware, redes, respaldo, IA y proyectos arcade en Vallenar."
};

export default function ServicesPage() {
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
              Servicios Tecnotics
            </p>
            <h1 className="text-4xl font-black leading-tight text-graphite sm:text-5xl">
              Tecnologia clara, cercana y preparada para resolver.
            </h1>
            <p className="mt-5 text-lg leading-8 text-slate-700">
              Esta es la primera version del catalogo publico. En futuras etapas se
              conectara con ordenes de trabajo, clientes, inventario y ecommerce.
            </p>
          </div>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <SectionHeading
            eyebrow="Catalogo"
            title="Areas de servicio iniciales"
            description="Servicios pensados para usuarios domesticos, empresas, gamers, estudiantes, adultos mayores e instituciones."
          />
          <div className="grid gap-5 md:grid-cols-2">
            {services.map((service) => {
              const Icon = service.icon;
              return (
                <article key={service.title} className="rounded border border-slate-200 p-6">
                  <div className="mb-5 flex items-center gap-4">
                    <span className="grid h-11 w-11 place-items-center rounded bg-graphite text-circuit">
                      <Icon size={22} />
                    </span>
                    <h2 className="text-xl font-bold text-graphite">{service.title}</h2>
                  </div>
                  <p className="text-sm leading-6 text-slate-600">{service.description}</p>
                  <div className="mt-5 flex items-center gap-2 text-sm font-semibold text-graphite">
                    <CheckCircle2 size={18} className="text-signal" />
                    Disponible para planificacion v0.1
                  </div>
                </article>
              );
            })}
          </div>
        </div>
      </section>
      <Footer />
    </main>
  );
}
