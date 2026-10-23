# Video de YouTube — Crazy Moto

Video de promoción de la entrega del 5 de noviembre (issue #16). Dura alrededor de
**1 minuto 40**: motion graphics con el estilo "Ruta de noche" del juego, clips de
partidas y la voz en off de Joaquín. El texto y lo que se ve en cada tramo está en
[`guion.md`](guion.md).

Las animaciones **se dibujan con código**, cuadro por cuadro, con los mismos
colores, la misma tipografía (Chakra Petch) y los mismos sprites del menú que usa
el juego. Así el video se ve igual que Crazy Moto y cualquier cambio se regenera
en minutos.

## Archivos

| Archivo | Qué hace |
|---|---|
| `guion.md` | El texto para grabar la voz y lo que se ve en cada tramo |
| `estilo.py` | Colores, fuentes, fondos, efecto neón y la salida a `.mp4` |
| `escenas.py` | Las 6 escenas animadas (gancho, autopista, wheelie, cámara, problema, cierre) |
| `montar.py` | Une escenas + fotos + clips + voz + música en el video final |
| `material/` | Fotos, clips y voz (no se suben al repo) |
| `render/` | Los `.mp4` que se generan (no se suben al repo) |

## Cómo se genera

Hace falta Python con `pillow`, `numpy`, `imageio-ffmpeg` (trae su propio ffmpeg)
y `qrcode`:

```
py -m pip install pillow numpy imageio-ffmpeg qrcode
```

1. Renderizar las escenas (cada una tarda entre 30 s y 3 min):

   ```
   py video/escenas.py gancho
   py video/escenas.py autopista
   py video/escenas.py wheelie
   py video/escenas.py camara
   py video/escenas.py problema
   py video/escenas.py cierre
   ```

   Para revisar rápido sin generar el video: `py video/escenas.py camara --cuadros 3 12`
   guarda esos segundos como PNG en `render/`.

2. Poner el material en `video/material/`: `fotos/menu.png`, los clips en
   `clips/` (`ciudad`, `campo`, `wheelie`, `choque`, `ranking`), la voz en `voz/`
   (`voz-1` a `voz-7`, un archivo por tramo) y, si hay, `musica.mp3`.

3. Armar el video:

   ```
   py video/montar.py
   ```

   Sale `video/render/crazy-moto.mp4`. Lo que falte del material aparece como una
   placa que dice qué va ahí.

## Detalles técnicos

- Se dibuja al **doble de resolución** y se achica a 1920×1080: Pillow no suaviza
  las líneas y así salen sin serrucho.
- El **neón** es una segunda capa con las mismas formas, más gruesas, que se
  desenfoca y se suma al fondo como luz.
- Cada forma se dibuja en un parche aparte y se mezcla con lo que hay abajo:
  dibujando directo sobre una imagen con transparencia, Pillow pisa los píxeles en
  vez de mezclarlos.
- Se probó primero con Manim, pero no tiene versión para Python 3.14.
- El QR del cierre se verificó leyéndolo con OpenCV: abre
  `https://s1rwa.itch.io/crazy-moto`.
