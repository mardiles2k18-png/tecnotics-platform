# Arquitectura inicial

## Capas

- `src/app`: rutas, metadata SEO y composicion de paginas.
- `src/components`: UI reutilizable para marketing, navegacion y secciones.
- `src/lib`: contenido compartido, configuracion del sitio e integraciones futuras.
- `public`: imagenes y assets estaticos.

## Principios

- Mantener el sitio publico desacoplado del futuro panel administrativo.
- Preparar contenido como datos tipados para poder migrar a CMS o base de datos.
- Usar Supabase solo cuando existan entidades operativas reales: clientes, ordenes, inventario y usuarios.
- Priorizar SEO local para Vallenar, Provincia del Huasco y servicios tecnologicos.

## Rutas v0.1

- `/`: landing corporativa.
- `/servicios`: catalogo inicial de servicios.

## Rutas futuras sugeridas

- `/galeria`
- `/blog`
- `/tienda`
- `/admin`
- `/clientes`
- `/ordenes`
- `/inventario`
- `/arcade`
- `/ia-tecnotics`
