import Image from "next/image";
import { MapPin } from "lucide-react";
import { site } from "@/lib/site";

export function Footer() {
  return (
    <footer id="contacto" className="bg-graphite text-white">
      <div className="mx-auto grid max-w-7xl gap-8 px-4 py-12 sm:px-6 lg:grid-cols-[0.95fr_1.05fr] lg:px-8">
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
        </div>

        <div className="grid gap-4">
          <div>
            <div className="mb-2 flex items-center gap-2 text-sm font-bold text-circuit">
              <MapPin size={18} />
              Ubicacion
            </div>
            <p className="text-sm leading-6 text-white/78">{site.address}</p>
            <a
              href={site.mapsUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-3 inline-flex items-center justify-center rounded bg-circuit px-4 py-2 text-sm font-bold text-graphite transition hover:bg-signal"
            >
              Abrir en Google Maps
            </a>
          </div>
          <div className="overflow-hidden rounded border border-white/16 bg-black">
            <iframe
              title={`Mapa de ${site.name}`}
              src={site.mapsEmbedUrl}
              className="h-72 w-full"
              loading="lazy"
              referrerPolicy="no-referrer-when-downgrade"
            />
          </div>
        </div>
      </div>
    </footer>
  );
}
