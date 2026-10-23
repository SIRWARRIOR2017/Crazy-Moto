# Guion del video — Crazy Moto

Video de promoción para YouTube (entrega del 5 de noviembre). Dura alrededor de
**1 minuto 40**. La voz es la de Joaquín, en off; no aparece nadie en cámara.

## Cómo grabar la voz

- Leé cada tramo **por separado**, con una pausa de 2 segundos entre uno y otro
  (o un archivo por tramo: `voz-1.m4a`, `voz-2.m4a`…). Así cada animación se
  ajusta a lo que dura tu voz.
- Con el celular alcanza: la app **Grabadora de voz** o un **audio de WhatsApp**
  mandado a vos mismo (después lo bajás desde WhatsApp Web y lo guardás como
  `voz-1.ogg`, `voz-2.ogg`… en `video/material/voz/`).
- Grabá en un lugar chico y con cosas blandas (una pieza con cama y cortinas), no
  en un baño ni en una sala vacía: el eco se nota mucho.
- El celular a un palmo de la boca, un poco de costado para que no "sople" en las P.
- Hablá claro y con ganas, como si se lo contaras a alguien en la feria. Si te trabás, repetí la frase entera: después se corta.
- El texto es una guía: si alguna frase no te sale natural, decila con tus
  palabras.
- Cada tramo dice cuánto tiene que durar. Está calculado a ritmo de narración
  normal, con ganas pero sin apurarte. Si un tramo te queda más largo, no lo
  cortes: avisame y estiro la animación.

## Tramos

### 1 · Gancho — 0:00 a 0:06

**Tu voz dura:** unos 5 s.

**Se ve:** la grilla de neón y el sol retro del menú; el título CRAZY MOTO se
arma con un destello.

> Esto es Crazy Moto: un juego de motos que se maneja con el cuerpo.

### 2 · De qué trata — 0:06 a 0:22

**Tu voz dura:** unos 14 s.

**Se ve:** la foto del menú con un zoom lento; después un clip de una partida y
un esquema de la autopista vista desde arriba, con los tres carriles y los autos
viniendo de frente.

> Vas por una autopista en contramano, y los autos vienen de frente por tres
> carriles. En cada fila siempre queda un carril libre, pero el juego se acelera
> solo: cada vez vas más rápido y tenés menos tiempo para esquivar.

### 3 · El wheelie — 0:22 a 0:38

**Tu voz dura:** unos 15 s.

**Se ve:** la moto levanta la rueda; una barra de puntos que crece con la
velocidad (de 100 a 300 puntos por segundo) y otra de movimiento lateral que se
achica.

> El wheelie es la única forma de sumar puntos, y cuanto más rápido vas, más te
> da: de cien a trescientos puntos por segundo. Pero mientras vas en una rueda
> casi no podés moverte de costado. Ahí está el riesgo.

### 4 · El control por cámara — 0:38 a 1:05

**Tu voz dura:** unos 25 s.

**Se ve:** una silueta hecha de puntos, como la que detecta MediaPipe. Se
iluminan los hombros y las muñecas. La línea entre las dos manos se inclina como
un manubrio y la moto dobla. Después las dos manos suben por encima de la línea
de los hombros y aparece WHEELIE. Al final, el recorrido de los datos: webcam →
Python con MediaPipe → Unity.

> Un programa en Python, con MediaPipe, ve treinta y tres puntos de tu cuerpo a
> través de la webcam. Nosotros usamos solo cuatro: los hombros y las muñecas.
> Agarrás un manubrio imaginario y, si lo girás, la moto dobla para ese lado. Si
> levantás las dos manos por encima de los hombros, hacés wheelie. Y todo le llega
> al juego treinta veces por segundo, sin joystick.

### 5 · El problema que resolvimos — 1:05 a 1:20

**Tu voz dura:** unos 14 s.

**Se ve:** un brazo visto de costado y visto de frente: de frente, el antebrazo
casi desaparece. Una señal que tiembla (la del codo) al lado de otra estable (la
del manubrio).

> La primera versión medía cuánto doblabas el codo, y fallaba: vista de frente,
> la cámara casi no ve el antebrazo. Así que cambiamos el gesto por uno que la
> cámara ve siempre bien, y ahora funciona aunque te muevas.

### 6 · El resto del juego — 1:20 a 1:32

**Tu voz dura:** unos 8 s.

**Se ve:** cortes rápidos de partidas: ciudad, campo, esquivadas, las chispas
del choque, el Game Over y el ranking.

> Además tiene ciudad y campo, efectos de partículas, sonidos generados por
> código y un ranking para ver quién llega más lejos.

### 7 · Cierre — 1:32 a 1:42

**Tu voz dura:** unos 8 s.

**Se ve:** el título, los nombres, "Proyecto Feria de Ciencias", el link de
itch.io y un código QR que lleva a la página.

> Crazy Moto, de Joaquín Pastorino y Santiago Castiñeira, para la Feria de
> Ciencias. Escaneá el código y bajalo gratis.

## Clips de juego que hay que grabar

Con el juego en pantalla completa, **Win + G** → botón de grabar (o Win + Alt +
R). Jugá con el teclado. Con 5 a 10 segundos por clip alcanza:

1. El menú: pasar el mouse por los botones y entrar a Jugar.
2. Una partida en la **ciudad**, esquivando autos.
3. Una partida en el **campo**.
4. Un **wheelie** largo, con el número del puntaje en magenta.
5. Un **choque**: chispas, flash y Game Over.
6. El **ranking** del menú con algunos puntajes cargados.

Los clips quedan en `Videos\Capturas` de tu usuario. Pasámelos junto con la voz y
las fotos.
