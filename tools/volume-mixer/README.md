# Mezclador de volumen y panel rápido (Windows)

Controla el volumen de cada programa sin abrir el mezclador de Windows. Pensado para
el caso típico: una película en una pantalla y un juego en la otra, y ajustar cada uno
en un segundo.

Como la pantalla secundaria no tiene los iconos de la bandeja, el panel también trae
**Discos** (expulsar USB y discos externos) y **Wi‑Fi** (ver la conexión, conectarse,
activar o desactivar).

- **Un solo `.exe` de ~90 KB**, sin instalar nada (usa .NET Framework 4.8, que ya viene en Windows 10/11).
- Vive en la **bandeja del sistema** (junto al reloj) y en un **botón flotante** que puedes dejar
  en cualquier pantalla, incluida la secundaria (que no tiene bandeja).
- **No le quita el foco al juego**: el botón flotante y el atajo abren el mezclador sin activarlo,
  así que el juego sigue recibiendo el teclado. No hace falta tocar la tecla Windows.
- **Atajos globales** que funcionan aunque estés dentro de un juego.
- Un **indicador en pantalla** muestra qué app cambió y a qué volumen, sin quitarle el foco al juego.

## Uso

### Botón flotante

Al abrir la app aparece un botón redondo con un parlante en la esquina inferior derecha de la
pantalla secundaria. Puedes **arrastrarlo** donde quieras (recuerda la posición).

- El botón muestra el parlante y, al lado, el estado del Wi‑Fi (tachado = apagado, gris = sin conexión).
- **Clic**: abre el panel junto al botón. Se cierra con otro clic en el botón o haciendo clic fuera.
- **Clic derecho**: menú (ocultar el botón, configuración, salir…).
- Si lo ocultas, se vuelve a mostrar desde el menú del icono de la bandeja → **Botón flotante**.

### Atajos de teclado

| Atajo | Qué hace |
| --- | --- |
| `Ctrl+Alt+V` | Abre / cierra el mezclador en la pantalla donde está el mouse, sin quitarle el foco al juego |
| `Ctrl+Alt+RePág` / `Ctrl+Alt+AvPág` | Sube / baja el volumen de la **app activa** (la del juego, si estás jugando) |
| `Ctrl+Alt+Fin` | Silencia / reactiva la app activa |
| `Ctrl+Alt+Shift+RePág` / `AvPág` | Sube / baja el volumen de la **app bajo el mouse** (deja el mouse sobre la película, sin hacer clic) |
| `Ctrl+Alt+Shift+Fin` | Silencia la app bajo el mouse |

Mantener presionado el atajo de subir/bajar repite el ajuste.

En el mezclador (clic en el icono de la bandeja o `Ctrl+Alt+V`):

- **Rueda del mouse** sobre una fila: ±5 % (con `Shift`: ±1 %).
- **Arrastrar** la barra para fijar el volumen.
- **Clic derecho** o clic en el icono: silenciar. **Clic con la rueda**: 100 %.
- Teclado (solo si lo abriste desde la bandeja, porque es el único modo en que toma el foco): `↑` `↓` eligen fila, `←` `→` ajustan, `M` silencia, `1`–`9` = 10–90 %, `0` = 100 %, `Esc` cierra.
- La barra verde bajo cada app indica si está sonando en ese momento.
- Se muestra cada dispositivo de salida (p. ej. parlantes y el HDMI del proyector/TV) con sus apps.

### Pestaña Discos

Lista las memorias USB, discos externos y tarjetas SD con su nombre, letra y espacio libre.
**Expulsar** hace lo mismo que "Quitar hardware de forma segura": cuando aparece
"Ya puedes desconectar", se puede retirar. Si un programa está usando el disco, el panel lo avisa
y el disco sigue conectado.

### Pestaña Wi‑Fi

- Arriba: la red conectada y su señal, con el botón **Desactivar** / **Activar** el Wi‑Fi.
- Debajo: las redes cercanas. Clic en una red guardada para conectarte; en la conectada, para desconectarte.
- Red nueva con contraseña: se abre una ventana para escribirla (esta ventana sí toma el teclado).
  Si la contraseña no funciona, no se guarda.
- Redes de empresa/universidad (con usuario): se abre la configuración de Windows.
- Si aparece **"Windows oculta las redes cercanas"**: Windows 11 exige permiso de ubicación para
  ver la lista de redes. El botón **Abrir** lleva a *Privacidad y seguridad → Ubicación*: activa
  *Servicios de ubicación* y *Permitir que las aplicaciones de escritorio accedan a tu ubicación*.

`Ctrl+Tab` cambia de pestaña cuando el panel tiene el foco.

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
