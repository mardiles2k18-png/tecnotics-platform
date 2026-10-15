import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import {
  ArrowLeft,
  ArrowRight,
  Cable,
  Cpu,
  HardDrive,
  Layers,
  MemoryStick,
  MessageCircle,
  Monitor,
  Network,
  Palette,
  Thermometer,
  Zap
} from "lucide-react";
import { Footer } from "@/components/Footer";
import { Header } from "@/components/Header";
import { SectionHeading } from "@/components/SectionHeading";
import { monitorSistema, monitorWhatsappUrl } from "@/lib/monitor-sistema";

export const metadata: Metadata = {
  title: 'Monitor de Sistema 3,5" para PC',
  description:
    "Pantalla de 3,5 pulgadas que muestra en tiempo real el uso de CPU, GPU, RAM, disco, red y temperaturas de tu PC con Windows. Con app Tecnotics para elegir y personalizar tu interfaz.",
  alternates: { canonical: monitorSistema.path },
  openGraph: {
    title: 'Monitor de Sistema Tecnotics 3,5"',
    description: monitorSistema.tagline,
    images: [monitorSistema.image]
  }
};

const priceFormatter = new Intl.NumberFormat("es-CL", {
  style: "currency",
  currency: "CLP",
  maximumFractionDigits: 0
});

const metrics = [
  { icon: Cpu, title: "Procesador", text: "Uso, frecuencia, consumo y temperatura de tu CPU." },
  { icon: Monitor, title: "Tarjeta grafica", text: "Uso de tu GPU, memoria de video y consumo mientras juegas." },
  { icon: MemoryStick, title: "Memoria RAM", text: "Cuanta memoria estas usando, en GB y en porcentaje." },
  { icon: HardDrive, title: "Disco", text: "Espacio ocupado de tu almacenamiento de un vistazo." },
  { icon: Network, title: "Red", text: "Velocidad de subida y bajada en tiempo real." },
  { icon: Thermometer, title: "Temperaturas", text: "Controla el calor de tu equipo antes de que se vuelva un problema." }
];

const steps = [
  {
    icon: Cable,
    title: "Conectalo por USB",
    text: "Enchufalo a un puerto USB de tu PC con Windows."
  },
  {
    icon: Palette,
    title: "Elige tu interfaz",
    text: "Desde la app Tecnotics escoges el diseno que mas te guste: futurista, gaming, minimalista y mas."
  },
  {
    icon: Layers,
    title: "Funciona en segundo plano",
    text: "La app trabaja sola y la pantalla queda siempre a la vista, sin ocupar tu monitor principal."
  }
];

const gallery = [
  {
    src: "/images/products/monitor-sistema/interfaz-tecnotics.jpg",
    alt: "Monitor de Sistema Tecnotics mostrando CPU, GPU, RAM, disco y red",
    caption: "Interfaz Tecnotics"
  },
  {
    src: "/images/products/monitor-sistema/vista-angulo.jpg",
    alt: "Monitor de sistema de 3,5 pulgadas visto en angulo",
    caption: "Pantalla de 3,5 pulgadas"
  },
  {
    src: "/images/products/monitor-sistema/tema-futurista.jpg",
    alt: "Monitor de sistema con un tema futurista",
    caption: "Temas futuristas"
  },
  {
    src: "/images/products/monitor-sistema/tema-planeta.jpg",
    alt: "Monitor de sistema con un tema minimalista de planeta",
    caption: "Y mucho mas por elegir"
  }
];

const specs = [
  ["Pantalla", "3,5 pulgadas"],
  ["Conexion", "USB"],
  ["Compatibilidad", "PC con Windows"],
  ["Software", "App Tecnotics para controlar y personalizar"],
  ["Datos en pantalla", "CPU, GPU, RAM, disco, red y temperaturas"],
  ["Uso", "Segundo plano, sin ocupar tu pantalla principal"]
];

const faqs = [
  {
    q: "¿Necesito instalar algo en mi PC?",
    a: "Si, la app Tecnotics, que es la que envia los datos a la pantalla y te deja elegir tu interfaz. Si tienes dudas con la instalacion, escribenos por WhatsApp."
  },
  {
    q: "¿Funciona con cualquier computador?",
    a: "Esta pensado para PC con Windows. Algunos sensores (por ejemplo la temperatura de ciertas tarjetas graficas) dependen del hardware de cada equipo."
  },
  {
    q: "¿Me quita espacio en mi monitor?",
    a: "No. Es una pantalla aparte: tu monitor principal queda libre para trabajar o jugar."
  }
];

export default function MonitorSistemaPage() {
  return (
    <main className="bg-white">
      <Header />

      <section className="relative overflow-hidden bg-graphite text-white">
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,rgba(105,199,216,0.18),transparent_55%)]" />
        <div className="relative mx-auto max-w-7xl px-4 py-14 sm:px-6 lg:px-8 lg:py-20">
          <Link
            href="/tienda"
            className="mb-8 inline-flex items-center gap-2 text-sm font-semibold text-white/72 transition hover:text-circuit"
          >
            <ArrowLeft size={16} />
            Volver a la tienda
          </Link>

          <div className="grid items-center gap-10 lg:grid-cols-[0.95fr_1.05fr]">
            <div>
              <span className="mb-4 inline-flex items-center gap-2 rounded bg-ember px-3 py-1 text-xs font-bold uppercase tracking-[0.14em] text-graphite">
                <Zap size={14} />
                Nuevo
              </span>
              <h1 className="text-4xl font-black leading-tight sm:text-5xl">
                Monitor de Sistema <span className="text-circuit">3,5&quot;</span>
              </h1>
              <p className="mt-3 text-xl font-semibold text-signal">{monitorSistema.tagline}</p>
              <p className="mt-5 max-w-xl text-base leading-7 text-white/78">
                Mira en una pantalla propia lo que pasa dentro de tu computador mientras juegas o
                trabajas: uso de procesador, tarjeta grafica, memoria RAM, disco, red y
                temperaturas, siempre a la vista y sin ocupar tu monitor principal.
              </p>

              <ul className="mt-6 grid gap-2 text-sm text-white/84 sm:grid-cols-2">
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-signal" />
                  Pantalla de 3,5 pulgadas
                </li>
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-signal" />
                  Conexion USB
                </li>
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-signal" />
                  Compatible con PC con Windows
                </li>
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-signal" />
                  App Tecnotics para personalizar
                </li>
              </ul>

              <div className="mt-8">
                {monitorSistema.price ? (
                  <p className="text-4xl font-black">{priceFormatter.format(monitorSistema.price)}</p>
                ) : (
                  <p className="text-lg font-semibold text-white/84">Consulta el precio por WhatsApp</p>
                )}
                <div className="mt-4 flex flex-col gap-3 sm:flex-row">
                  <a
                    href={monitorWhatsappUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center justify-center gap-2 rounded bg-circuit px-6 py-3 font-bold text-graphite transition hover:bg-signal"
                  >
                    <MessageCircle size={18} />
                    Lo quiero
                    <ArrowRight size={18} />
                  </a>
                  <a
                    href="#como-funciona"
                    className="inline-flex items-center justify-center rounded border border-white/22 px-6 py-3 font-bold text-white transition hover:bg-white/12"
                  >
                    Como funciona
                  </a>
                </div>
              </div>
            </div>

            <div className="overflow-hidden rounded border border-white/20 bg-black shadow-glow">
              <video
                className="aspect-video w-full"
                src={monitorSistema.video}
                poster={monitorSistema.poster}
                controls
                playsInline
                preload="metadata"
              >
                Tu navegador no soporta video HTML5.
              </video>
            </div>
          </div>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <SectionHeading
            eyebrow="En tiempo real"
            title="Todo lo importante de tu PC, de un vistazo"
            description="Deja de abrir programas para saber si tu equipo esta forzado. La informacion que necesitas esta siempre frente a ti."
          />
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {metrics.map((metric) => {
              const Icon = metric.icon;
              return (
                <div key={metric.title} className="rounded border border-slate-200 bg-white p-6 shadow-sm">
                  <span className="mb-4 grid h-11 w-11 place-items-center rounded bg-circuit text-graphite">
                    <Icon size={22} />
                  </span>
                  <h3 className="text-lg font-bold text-graphite">{metric.title}</h3>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{metric.text}</p>
                </div>
              );
            })}
          </div>
          <p className="mt-6 text-center text-xs text-slate-500">
            Los sensores disponibles pueden variar segun el hardware de cada equipo.
          </p>
        </div>
      </section>

      <section id="como-funciona" className="scroll-mt-20 bg-panel py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <SectionHeading
            eyebrow="Como funciona"
            title="Listo en tres pasos"
            description="Lo conectas, eliges como quieres verlo y te olvidas: la app trabaja en segundo plano."
          />
          <div className="grid gap-4 md:grid-cols-3">
            {steps.map((step, index) => {
              const Icon = step.icon;
              return (
                <div key={step.title} className="rounded border border-slate-200 bg-white p-6">
                  <div className="mb-4 flex items-center gap-3">
                    <span className="grid h-9 w-9 place-items-center rounded-full bg-ember text-base font-black text-graphite">
                      {index + 1}
                    </span>
                    <Icon className="text-circuit" size={24} />
                  </div>
                  <h3 className="text-lg font-bold text-graphite">{step.title}</h3>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{step.text}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <SectionHeading
            eyebrow="Disenos"
            title="Una interfaz para cada estilo"
            description="Cambia el diseno cuando quieras desde la app Tecnotics: gaming, futurista, minimalista y mucho mas."
          />
          <div className="grid gap-4 sm:grid-cols-2">
            {gallery.map((item) => (
              <figure key={item.src} className="overflow-hidden rounded border border-slate-200 bg-graphite">
                <div className="relative aspect-[4/3]">
                  <Image
                    src={item.src}
                    alt={item.alt}
                    fill
                    sizes="(min-width: 640px) 50vw, 100vw"
                    className="object-cover"
                  />
                </div>
                <figcaption className="px-4 py-3 text-sm font-semibold text-white">{item.caption}</figcaption>
              </figure>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-panel py-20">
        <div className="mx-auto grid max-w-7xl gap-10 px-4 sm:px-6 lg:grid-cols-2 lg:px-8">
          <div>
            <p className="mb-3 text-xs font-bold uppercase tracking-[0.18em] text-circuit">
              Ficha tecnica
            </p>
            <h2 className="text-3xl font-bold text-graphite sm:text-4xl">Lo que necesitas saber</h2>
            <dl className="mt-6 divide-y divide-slate-200 rounded border border-slate-200 bg-white">
              {specs.map(([label, value]) => (
                <div key={label} className="grid gap-1 px-5 py-3 sm:grid-cols-[10rem_1fr]">
                  <dt className="text-sm font-bold text-graphite">{label}</dt>
                  <dd className="text-sm text-slate-600">{value}</dd>
                </div>
              ))}
            </dl>
          </div>
          <div>
            <p className="mb-3 text-xs font-bold uppercase tracking-[0.18em] text-circuit">
              Preguntas frecuentes
            </p>
            <h2 className="text-3xl font-bold text-graphite sm:text-4xl">Antes de pedirlo</h2>
            <div className="mt-6 grid gap-3">
              {faqs.map((faq) => (
                <details key={faq.q} className="group rounded border border-slate-200 bg-white px-5 py-4">
                  <summary className="cursor-pointer list-none font-bold text-graphite">{faq.q}</summary>
                  <p className="mt-3 text-sm leading-6 text-slate-600">{faq.a}</p>
                </details>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-graphite py-16 text-white">
        <div className="mx-auto flex max-w-4xl flex-col items-center gap-5 px-4 text-center sm:px-6">
          <h2 className="text-3xl font-bold sm:text-4xl">Dale cara a tu PC</h2>
          <p className="max-w-xl text-white/72">
            Escribenos por WhatsApp, te contamos disponibilidad y coordinamos todo por ahi.
          </p>
          <a
            href={monitorWhatsappUrl}
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center justify-center gap-2 rounded bg-circuit px-7 py-3 font-bold text-graphite transition hover:bg-signal"
          >
            <MessageCircle size={18} />
            Pedir por WhatsApp
          </a>
        </div>
      </section>

      <Footer />
    </main>
  );
}
