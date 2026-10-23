# Mezclador de volumen (Windows)

Controla el volumen de cada programa sin abrir el mezclador de Windows. Pensado para
el caso típico: una película en una pantalla y un juego en la otra, y ajustar cada uno
en un segundo.

- **Un solo `.exe` de ~90 KB**, sin instalar nada (usa .NET Framework 4.8, que ya viene en Windows 10/11).
- Vive en la **bandeja del sistema** (junto al reloj).
- **Atajos globales** que funcionan aunque estés dentro de un juego.
- Un **indicador en pantalla** muestra qué app cambió y a qué volumen, sin quitarle el foco al juego.

## Uso

| Atajo | Qué hace |
| --- | --- |
| `Ctrl+Alt+V` | Abre / cierra el mezclador |
| `Ctrl+Alt+RePág` / `Ctrl+Alt+AvPág` | Sube / baja el volumen de la **app activa** (la del juego, si estás jugando) |
| `Ctrl+Alt+Fin` | Silencia / reactiva la app activa |
| `Ctrl+Alt+Shift+RePág` / `AvPág` | Sube / baja el volumen de la **app bajo el mouse** (deja el mouse sobre la película, sin hacer clic) |
| `Ctrl+Alt+Shift+Fin` | Silencia la app bajo el mouse |

Mantener presionado el atajo de subir/bajar repite el ajuste.

En el mezclador (clic en el icono de la bandeja o `Ctrl+Alt+V`):

- **Rueda del mouse** sobre una fila: ±5 % (con `Shift`: ±1 %).
- **Arrastrar** la barra para fijar el volumen.
- **Clic derecho** o clic en el icono: silenciar. **Clic con la rueda**: 100 %.
- Teclado: `↑` `↓` eligen fila, `←` `→` ajustan, `M` silencia, `1`–`9` = 10–90 %, `0` = 100 %, `Esc` cierra.
- La barra verde bajo cada app indica si está sonando en ese momento.
- Se muestra cada dispositivo de salida (p. ej. parlantes y el HDMI del proyector/TV) con sus apps.

## Configuración

Clic derecho en el icono de la bandeja → **Editar configuración** abre
`%APPDATA%\TecnoticsVolumeMixer\settings.ini`. Ahí se cambian los atajos, el tamaño del paso
y si se muestra el indicador en pantalla. Al guardar el archivo los cambios se aplican solos.

En el mismo menú está **Iniciar con Windows**.

## Compilar

Con el [SDK de .NET](https://dotnet.microsoft.com/download) instalado:

```bat
tools\volume-mixer\build.cmd
```

El ejecutable queda en `tools\volume-mixer\bin\Release\net48\VolumeMixer.exe`.
También lo compila el workflow `Build Volume Mixer` de GitHub Actions (artefacto `VolumeMixer`)
en cada cambio dentro de `tools/volume-mixer/`.

## Notas

- El indicador en pantalla no se ve sobre juegos en **pantalla completa exclusiva**; en modo
  *ventana sin bordes* sí. Los atajos funcionan en ambos casos.
- Los navegadores reproducen el audio desde un proceso aparte; la app los agrupa por programa,
  así que "Google Chrome" es una sola fila.
