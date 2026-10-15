import { site } from "@/lib/site";

export const monitorSistema = {
  slug: "monitor-de-sistema",
  path: "/tienda/monitor-de-sistema",
  name: 'Monitor de Sistema Tecnotics 3,5"',
  tagline: "Tu PC bajo control, en tiempo real.",
  summary:
    "Una pantalla de 3,5 pulgadas que muestra el estado de tu computador con la app Tecnotics: procesador, tarjeta grafica, memoria, disco, red y temperaturas, sin ocupar tu pantalla principal.",
  // Precio en CLP. Con null se muestra "Consulta el precio por WhatsApp".
  price: null as number | null,
  image: "/images/products/monitor-sistema/interfaz-tecnotics.jpg",
  video: "/videos/monitor-sistema.mp4",
  poster: "/videos/monitor-sistema-poster.jpg"
};

export const monitorWhatsappUrl = `${site.whatsappUrl}?text=${encodeURIComponent(
  `Hola Tecnotics, me interesa el ${monitorSistema.name}. ¿Me das mas informacion?`
)}`;
