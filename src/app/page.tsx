import Image from "next/image";
import Link from "next/link";
import {
  ArrowRight,
  CheckCircle2,
  Cpu,
  Facebook,
  Gauge,
  Instagram,
  MapPin,
  MessageCircle,
  Sparkles
} from "lucide-react";
import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";
import { SectionHeading } from "@/components/SectionHeading";
import { platformModules, services, site } from "@/lib/site";

const promises = ["20+ anos de experiencia", "Soporte cercano", "IA como apoyo tecnico"];

const contactCards = [
  {
    label: "WhatsApp",
    value: site.phoneLabel,
    href: site.whatsappUrl,
    icon: MessageCircle
  },
  {
    label: "Instagram",
    value: site.instagramLabel,
    href: site.instagramUrl,
    icon: Instagram
  },
  {
    label: "Facebook",
    value: "tecno.tics.334",
    href: site.facebookUrl,
    icon: Facebook
  }
];

export default function HomePage() {
  return (
    <main>
      <Header />

      <section className="relative min-h-[calc(100svh-65px)] overflow-hidden bg-graphite text-white">
        <Image
          src="/images/hero-tecnotics.png"
          alt="Taller tecnologico moderno de Tecnotics"
          fill
          priority
          className="object-cover object-center opacity-72"
        />
        <div className="absolute inset-0 bg-[linear-gradient(90deg,rgba(17,19,24,0.95)_0%,rgba(17,19,24,0.78)_42%,rgba(17,19,24,0.18)_100%)]" />
        <div className="relative mx-auto flex min-h-[calc(100svh-65px)] max-w-7xl items-center px-4 py-20 sm:px-6 lg:px-8">
          <div className="max-w-2xl">
            <div className="mb-6 inline-flex items-center gap-2 rounded border border-white/18 bg-white/10 px-3 py-2 text-sm text-white/82 backdrop-blur">
              <MapPin size={16} />
              {site.location}
            </div>
            <div className="mb-7 max-w-xl">
              <Image
                src="/images/brand/tecnotics-logo-horizontal-transparent.png"
                alt="Tecnotics"
                width={1450}
                height={441}
                priority
                className="h-auto w-full drop-shadow-[0_0_28px_rgba(105,199,216,0.22)]"
              />
            </div>
            <h1 className="sr-only">
              Tecnotics
            </h1>
            <p className="mt-6 max-w-xl text-xl leading-8 text-white/84">
              {site.tagline} Servicio tecnico, soluciones tecnologicas e innovacion
              aplicada para personas, empresas y proyectos especiales.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <a
                href={site.whatsappUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center justify-center gap-2 rounded bg-circuit px-5 py-3 font-bold text-graphite transition hover:bg-signal"
              >
                Solicitar diagnostico
                <ArrowRight size={18} />
              </a>
              <Link
                href="/servicios"
                className="inline-flex items-center justify-center gap-2 rounded border border-white/22 px-5 py-3 font-bold text-white transition hover:bg-white/12"
              >
                Ver servicios
              </Link>
            </div>
            <div className="mt-10 grid gap-3 sm:grid-cols-3">
              {promises.map((item) => (
                <div key={item} className="flex items-center gap-2 text-sm text-white/78">
                  <CheckCircle2 size={18} className="text-signal" />
                  {item}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <SectionHeading
            eyebrow="Servicios"
            title="Soluciones tecnologicas para el dia a dia y el trabajo"
            description="La plataforma parte mostrando la oferta publica de Tecnotics y queda preparada para convertirse en herramienta operativa del negocio."
          />
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {services.slice(0, 6).map((service) => {
              const Icon = service.icon;
              return (
                <article
                  key={service.title}
                  className="group relative min-h-[280px] overflow-hidden rounded border border-slate-200 bg-graphite p-6 text-white shadow-sm"
                >
                  <Image
                    src={service.image}
                    alt=""
                    fill
                    sizes="(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
                    className="object-cover transition duration-500 group-hover:scale-105"
                  />
                  <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(17,19,24,0.28)_0%,rgba(17,19,24,0.72)_45%,rgba(17,19,24,0.96)_100%)]" />
                  <div className="relative flex min-h-[232px] flex-col justify-end">
                    <span className="mb-5 grid h-11 w-11 place-items-center rounded bg-circuit text-graphite shadow-glow">
                      <Icon size={22} />
                    </span>
                    <h3 className="text-xl font-bold">{service.title}</h3>
                    <p className="mt-3 text-sm leading-6 text-white/78">{service.description}</p>
                  </div>
                </article>
              );
            })}
          </div>
        </div>
      </section>

      <section className="bg-panel py-20">
        <div className="mx-auto grid max-w-7xl gap-10 px-4 sm:px-6 md:grid-cols-[0.9fr_1.1fr] lg:px-8">
          <div>
            <p className="mb-3 text-xs font-bold uppercase tracking-[0.18em] text-circuit">
              Plataforma
            </p>
            <h2 className="text-3xl font-bold text-graphite sm:text-4xl">
              Una base lista para crecer mas alla de una pagina web
            </h2>
            <p className="mt-5 text-base leading-7 text-slate-600">
              La arquitectura queda preparada para sumar CRM, ordenes de trabajo,
              inventario, tienda online, dashboard, IA y Laboratorio Arcade sin rehacer
              la experiencia publica.
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            {platformModules.map((module) => {
              const Icon = module.icon;
              return (
                <div key={module.title} className="rounded border border-slate-200 bg-white p-5">
                  <Icon className="mb-4 text-circuit" size={24} />
                  <h3 className="font-bold text-graphite">{module.title}</h3>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="bg-graphite py-20 text-white">
        <div className="mx-auto grid max-w-7xl items-center gap-10 px-4 sm:px-6 md:grid-cols-[0.95fr_1.05fr] lg:px-8">
          <div>
            <p className="mb-3 text-xs font-bold uppercase tracking-[0.18em] text-circuit">
              Identidad Tecnotics
            </p>
            <h2 className="text-3xl font-bold sm:text-4xl">
              Circuitos, soporte tecnico y espiritu gamer en una sola marca
            </h2>
            <p className="mt-5 text-base leading-7 text-white/72">
              La nueva base visual toma el celeste y naranja oficiales del logo,
              con una presencia tecnologica clara para web, servicios, arcade e IA.
            </p>
          </div>
          <div className="relative min-h-[420px] overflow-hidden rounded border border-white/20 bg-black shadow-glow">
            <Image
              src="/images/brand/tecnotics-card.jpg"
              alt="Tarjeta de presentacion Tecnotics"
              fill
              sizes="(min-width: 768px) 54vw, 100vw"
              className="object-cover object-left"
            />
            <div className="absolute inset-0 bg-[linear-gradient(180deg,rgba(0,0,0,0.1)_0%,rgba(0,0,0,0.8)_46%,rgba(0,0,0,0.96)_100%)] sm:bg-[linear-gradient(90deg,rgba(0,0,0,0)_0%,rgba(0,0,0,0.18)_42%,rgba(0,0,0,0.92)_62%,rgba(0,0,0,0.96)_100%)]" />
            <div className="relative ml-auto flex min-h-[420px] w-full flex-col justify-end px-6 py-8 sm:max-w-[52%] sm:justify-center sm:px-8">
              <Image
                src="/images/brand/tecnotics-logo-stacked-transparent.png"
                alt=""
                width={120}
                height={96}
                className="mb-6 h-20 w-24 object-contain"
              />
              <div className="grid gap-3">
                {contactCards.map((contact) => {
                  const Icon = contact.icon;
                  return (
                    <a
                      key={contact.label}
                      href={contact.href}
                      target="_blank"
                      rel="noreferrer"
                      className="group flex items-center gap-3 rounded border border-white/14 bg-white/8 px-4 py-3 text-left backdrop-blur transition hover:border-circuit hover:bg-circuit/12"
                    >
                      <span className="grid h-10 w-10 shrink-0 place-items-center rounded bg-circuit text-graphite">
                        <Icon size={20} />
                      </span>
                      <span className="min-w-0">
                        <span className="block text-xs font-bold uppercase tracking-[0.12em] text-white/54">
                          {contact.label}
                        </span>
                        <span className="block break-words text-sm font-bold text-white group-hover:text-circuit sm:text-base">
                          {contact.value}
                        </span>
                      </span>
                    </a>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-4 md:grid-cols-3">
            {[
              { icon: Cpu, value: "Experiencia", label: "Mas de dos decadas trabajando con tecnologia." },
              { icon: Sparkles, value: "Innovacion", label: "IA como apoyo para investigacion y diagnostico." },
              { icon: Gauge, value: "Rendimiento", label: "Optimizacion real para equipos y procesos." }
            ].map((item) => {
              const Icon = item.icon;
              return (
                <div key={item.value} className="rounded bg-graphite p-6 text-white">
                  <Icon className="mb-5 text-circuit" size={26} />
                  <h3 className="text-xl font-bold">{item.value}</h3>
                  <p className="mt-2 text-sm leading-6 text-white/72">{item.label}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <Footer />
    </main>
  );
}
