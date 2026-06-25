import {
  Bot,
  Cpu,
  Gamepad2,
  HardDrive,
  Network,
  ShieldCheck,
  ShoppingBag,
  Wrench
} from "lucide-react";

export const site = {
  name: "Tecnotics",
  tagline: "El futuro de la tecnologia ya llego a Vallenar.",
  description:
    "Servicio tecnico, soluciones tecnologicas, soporte empresarial, gaming, IA y proyectos arcade en Vallenar y la Provincia del Huasco.",
  location: "Vallenar, Provincia del Huasco",
  phoneLabel: "+56 9 8731 5288",
  whatsappUrl: "https://wa.me/56987315288",
  instagramLabel: "@tecno.tics",
  instagramUrl: "https://www.instagram.com/tecno.tics",
  facebookLabel: "Tecnotics",
  facebookUrl: "https://www.facebook.com/tecno.tics.334",
  email: "contacto@tecnotics.cl"
};

export const navItems = [
  { label: "Inicio", href: "/" },
  { label: "Servicios", href: "/servicios" },
  { label: "Galeria", href: "#galeria" },
  { label: "Blog", href: "#blog" },
  { label: "Contacto", href: "#contacto" }
];

export const services = [
  {
    title: "Servicio tecnico",
    description:
      "Diagnostico, reparacion, mantencion y optimizacion de notebooks, computadores y equipos de trabajo.",
    icon: Wrench
  },
  {
    title: "Computadores y hardware",
    description:
      "Armado, actualizacion, limpieza, almacenamiento, respaldo y mejoras de rendimiento para uso diario o gaming.",
    icon: Cpu
  },
  {
    title: "Redes y empresas",
    description:
      "Soporte tecnologico, redes, continuidad operativa y asesoria para negocios e instituciones locales.",
    icon: Network
  },
  {
    title: "Seguridad y respaldo",
    description:
      "Proteccion de datos, recuperacion, copias de seguridad, buenas practicas y configuracion segura.",
    icon: ShieldCheck
  },
  {
    title: "IA Tecnotics",
    description:
      "Uso de inteligencia artificial como apoyo para diagnostico, investigacion tecnica y asesoria tecnologica.",
    icon: Bot
  },
  {
    title: "Laboratorio Arcade",
    description:
      "Proyectos especiales, restauracion, configuracion y experiencias tecnologicas orientadas al mundo arcade.",
    icon: Gamepad2
  }
];

export const platformModules = [
  { title: "Tienda online", icon: ShoppingBag },
  { title: "Inventario", icon: HardDrive },
  { title: "Ordenes de trabajo", icon: Wrench },
  { title: "IA Tecnotics", icon: Bot }
];
