import type { Metadata } from "next";
import "./globals.css";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  metadataBase: new URL(site.url),
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
  alternates: {
    canonical: "/"
  },
  openGraph: {
    title: "Tecnotics | Servicio tecnico en Vallenar",
    description: site.description,
    url: site.url,
    siteName: site.name,
    locale: "es_CL",
    type: "website",
    images: ["/images/brand/tecnotics-card.jpg"]
  }
};

const localBusinessSchema = {
  "@context": "https://schema.org",
  "@type": "LocalBusiness",
  "@id": `${site.url}/#localbusiness`,
  name: site.name,
  url: site.url,
  image: `${site.url}/images/brand/tecnotics-card.jpg`,
  description: site.description,
  telephone: site.phoneLabel.replaceAll(" ", ""),
  email: site.email,
  address: {
    "@type": "PostalAddress",
    streetAddress: site.streetAddress,
    addressLocality: site.city,
    addressRegion: site.region,
    postalCode: site.postalCode,
    addressCountry: site.country
  },
  areaServed: ["Vallenar", "Provincia del Huasco", "Atacama"],
  sameAs: [site.instagramUrl, site.facebookUrl]
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es-CL">
      <body>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(localBusinessSchema) }}
        />
        {children}
      </body>
    </html>
  );
}
