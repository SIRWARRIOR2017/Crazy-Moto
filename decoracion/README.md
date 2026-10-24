# Decoración del stand — Crazy Moto

Carteles para la feria (entrega del 5 de noviembre, issue #17), con el estilo
"Ruta de noche" del juego: los mismos colores, la fuente Chakra Petch y el sol
retro del menú.

| Archivo | Tamaño | Para qué |
|---|---|---|
| [`render/poster-a3.pdf`](render/poster-a3.pdf) | A3 vertical | Pegarlo en el stand. Se lee de lejos: título, una captura con dos autos de frente, "¡Manejá con tu cuerpo!" y dos QR (jugarlo en itch.io y ver el video) |
| [`render/instrucciones-a4.pdf`](render/instrucciones-a4.pdf) | A4 vertical | Al lado de la compu: los tres pasos para jugar con la cámara (pararse, doblar, wheelie) y qué hacer si no te detecta |

## Cómo imprimirlos

- Los dos están a **300 dpi**, en color.
- **Póster A3:** en una fotocopiadora o imprenta, pedí "**A3 a color, tamaño
  real (100 %)**". Si se puede, en papel un poco más grueso (ilustración de
  150 g o más) para que no se arrugue.
- **Instrucciones A4:** en cualquier impresora a color, con la opción **"Tamaño
  real"** (no "Ajustar a la página", que achica todo).
- El fondo es oscuro hasta el borde. Las impresoras comunes dejan un margen
  blanco de unos milímetros: es normal, todo el texto está lejos del borde.
- **Probá los QR con el celular** antes de imprimir (se pueden escanear de la
  pantalla).

## Armado del stand (propuesta)

Con lo que hay (2 mesas, proyector, silla y teclado):

```
              PARED / PANTALLA
     ┌──────────────────────────────┐
     │   juego proyectado (grande)  │
     └──────────────────────────────┘

   [ mesa 1: notebook + webcam ]  [ mesa 2: proyector ]
        │ cartel A4 al lado
        │
        │  1 a 2 metros
        ▼
      jugador (parado, de frente a la webcam)

   póster A3 en el frente de la mesa, mirando al pasillo
```

- **La webcam, a la altura del pecho o la cara** del jugador y mirándolo de
  frente. Tienen que entrar en cuadro los hombros y las dos manos, también
  cuando las levanta para el wheelie.
- **El jugador a 1 o 2 metros**, mejor parado que sentado: con los brazos libres
  el gesto del manubrio sale más natural. La silla, para quien la necesite.
- **La luz, de frente al jugador**, no detrás (una ventana o una lámpara a sus
  espaldas lo deja a contraluz y la detección falla).
- **El juego proyectado grande** atrás de la notebook, así el que juega mira la
  pantalla grande y la gente del pasillo también lo ve.
- Si pasa gente por detrás no pasa nada: el detector juega con la persona más
  cercana a la cámara.
- El teclado queda a mano por si la cámara falla (A / D y Shift).

## Cómo se generan

```
py decoracion/carteles.py
```

Usa el motor del video (`video/estilo.py`) y la captura `img/contramano.jpg`,
que sale del clip del choque: es el cuadro justo antes de que los dos autos
lleguen, con el puntaje recortado y el puntero del mouse borrado. Además de los
PDF deja una vista previa liviana de cada uno en `vistas/`, que es la que se
muestra en el README del proyecto.
