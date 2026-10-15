import { site } from "@/lib/site";

// Contenido de la pagina del producto. El precio y el stock viven en la base de datos
// (se editan desde /admin).
export const monitorSistema = {
  slug: "monitor-de-sistema",
  path: "/tienda/monitor-de-sistema",
  name: 'Monitor de Sistema Tecnotics 3,5"',
  tagline: "Tu PC bajo control, en tiempo real.",
  summary:
    "Una pantalla de 3,5 pulgadas que muestra el estado de tu computador con la app Tecnotics: procesador, tarjeta grafica, memoria, disco, red y temperaturas, sin ocupar tu pantalla principal.",
  image: "/images/products/monitor-sistema/interfaz-tecnotics.jpg",
  video: "/videos/monitor-sistema.mp4",
  poster: "/videos/monitor-sistema-poster.jpg"
};

export const monitorWhatsappUrl = `${site.whatsappUrl}?text=${encodeURIComponent(
  `Hola Tecnotics, me interesa el ${monitorSistema.name}. ¿Me das mas informacion?`
)}`;

export const monitorWaitlistUrl = `${site.whatsappUrl}?text=${encodeURIComponent(
  `Hola Tecnotics, quiero que me avisen cuando vuelva el ${monitorSistema.name}.`
)}`;
