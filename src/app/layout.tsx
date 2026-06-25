import type { Metadata } from "next";
import "./globals.css";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  metadataBase: new URL("https://tecnotics.cl"),
  title: {
    default: "Tecnotics | Tecnologia, servicio tecnico e innovacion en Vallenar",
    template: "%s | Tecnotics"
  },
  description: site.description,
  keywords: [
    "Tecnotics",
    "servicio tecnico Vallenar",
    "reparacion computadores",
    "soporte tecnologico",
    "Provincia del Huasco",
    "inteligencia artificial",
    "arcade"
  ],
  openGraph: {
    title: "Tecnotics Platform",
    description: site.description,
    locale: "es_CL",
    type: "website",
    images: ["/images/brand/tecnotics-card.jpg"]
  }
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es-CL">
      <body>{children}</body>
    </html>
  );
}
