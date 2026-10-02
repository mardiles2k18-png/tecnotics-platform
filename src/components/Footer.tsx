import Image from "next/image";
import { site } from "@/lib/site";
import { VisitCounter } from "@/components/VisitCounter";

export function Footer() {
  return (
    <footer id="contacto" className="bg-graphite text-white">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div>
          <div className="mb-4 flex items-center gap-3">
            <span className="grid h-12 w-12 place-items-center">
              <Image
                src="/images/brand/tecnotics-logo-stacked-transparent.png"
                alt=""
                width={48}
                height={48}
                className="h-full w-full object-contain"
              />
            </span>
            <strong>{site.name}</strong>
          </div>
          <p className="max-w-xl text-sm leading-6 text-white/70">
            Plataforma tecnologica profesional para servicio tecnico, soporte,
            innovacion, IA y proyectos especiales en Vallenar.
          </p>
          <div className="mt-6 grid gap-2 text-sm text-white/74">
            <a href={`mailto:${site.email}`} className="hover:text-circuit">
              {site.email}
            </a>
            <a href={site.whatsappUrl} target="_blank" rel="noreferrer" className="hover:text-circuit">
              WhatsApp {site.phoneLabel}
            </a>
            <a href={site.instagramUrl} target="_blank" rel="noreferrer" className="hover:text-circuit">
              Instagram {site.instagramLabel}
            </a>
            <a href={site.facebookUrl} target="_blank" rel="noreferrer" className="hover:text-circuit">
              Facebook {site.facebookLabel}
            </a>
          </div>
          <div className="mt-6">
            <VisitCounter />
          </div>
        </div>
      </div>
    </footer>
  );
}
