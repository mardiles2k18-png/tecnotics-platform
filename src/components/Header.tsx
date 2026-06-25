import Link from "next/link";
import Image from "next/image";
import { Menu } from "lucide-react";
import { navItems, site } from "@/lib/site";

export function Header() {
  return (
    <header className="sticky top-0 z-40 border-b border-white/15 bg-graphite/90 text-white backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        <Link href="/" className="flex items-center gap-3" aria-label="Tecnotics inicio">
          <span className="grid h-11 w-11 place-items-center">
            <Image
              src="/images/brand/tecnotics-logo-stacked-transparent.png"
              alt=""
              width={44}
              height={44}
              className="h-full w-full object-contain"
            />
          </span>
          <span className="leading-tight">
            <strong className="block text-base">{site.name}</strong>
            <span className="block text-xs text-white/68">{site.location}</span>
          </span>
        </Link>

        <nav className="hidden items-center gap-1 md:flex" aria-label="Principal">
          {navItems.map((item) => (
            <Link
              key={item.label}
              href={item.href}
              className="rounded px-3 py-2 text-sm text-white/78 transition hover:bg-white/10 hover:text-white"
            >
              {item.label}
            </Link>
          ))}
        </nav>

        <a
          href={site.whatsappUrl}
          target="_blank"
          rel="noreferrer"
          className="hidden rounded bg-white px-4 py-2 text-sm font-semibold text-graphite transition hover:bg-circuit md:inline-flex"
        >
          Solicitar soporte
        </a>

        <button
          className="grid h-10 w-10 place-items-center rounded border border-white/18 text-white md:hidden"
          aria-label="Abrir menu"
          type="button"
        >
          <Menu size={20} />
        </button>
      </div>
    </header>
  );
}
