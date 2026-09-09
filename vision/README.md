# Control por cámara — Crazy Moto

Reemplaza al manubrio de Arduino: la webcam detecta los movimientos del jugador y
se los manda al juego. Sale $0 porque usa la cámara que ya tiene la notebook.

## Cómo se juega

Sentate frente a la cámara y subí los brazos como si agarraras un manubrio.

| Acción | Gesto |
|---|---|
| Doblar a la derecha | Tirás el brazo **derecho** hacia el cuerpo y empujás el izquierdo hacia adelante |
| Doblar a la izquierda | Al revés: tirás el **izquierdo** y empujás el derecho |
| Wheelie | Tirás **los dos brazos** hacia el cuerpo al mismo tiempo |

Es el movimiento de un manubrio de verdad. El wheelie es el tirón de los dos
puños que hacés en una moto para levantar la rueda.

## Instalación (una sola vez)

```
python -m pip install -r vision/requirements.txt
```

El archivo del modelo (`vision/modelo/pose_landmarker_lite.task`, 5,5 MB) ya está
en el repo, así que no hay que bajar nada más. Si alguna vez falta:

```
curl -L -o vision/modelo/pose_landmarker_lite.task https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task
```

Probado con Python 3.14.3 y MediaPipe 1.0.1 en Windows 11.

## Cómo se usa

1. Abrí el detector **antes** que el juego (o en cualquier momento, da igual):

   ```
   python vision/deteccion.py
   ```

2. Quedate quieto 2 segundos en posición de manubrio: eso es la **calibración**,
   que aprende cómo tenés los brazos en reposo. Después ya podés jugar.

3. En el juego, entrá a **Opciones → Probar cámara** para verificar que te está
   leyendo bien y acomodarte antes de empezar.

Opciones del script:

```
python vision/deteccion.py --camara 1        # si tenés más de una cámara
python vision/deteccion.py --puerto 5005     # puerto UDP (por defecto 5005)
python vision/deteccion.py --sin-ventana     # sin la ventana de OpenCV (para la feria)
```

Teclas dentro de la ventana: **c** recalibra la pose neutra, **q** cierra.

## Si algo no anda

| Síntoma | Qué pasa |
|---|---|
| El juego dice "SIN SEÑAL" | El script no está corriendo, o lo abriste con otro `--puerto` |
| "CONECTADA, PERO NO TE VEO" | Falta luz, o no entran los hombros y los codos en el cuadro. Alejate un poco |
| `no pude abrir la camara 0` | Otro programa la tiene abierta (Zoom, Meet, Teams), o probá `--camara 1` |
| La moto dobla sola | Recalibrá con **c** quedándote quieto y simétrico |
| Dobla muy poco / muy fuerte | Tocá `GANANCIA_DIRECCION` arriba de `deteccion.py` |
| **El wheelie no se activa** | Mirá la barra `wheelie` en la ventana: si al tirar a fondo no llega a la línea blanca, bajá `WHEELIE_DELTA` y `WHEELIE_FRACCION` |
| El wheelie se activa solo | Al revés: subí `WHEELIE_DELTA` y `WHEELIE_FRACCION` |
| Sale el aviso "calibraste con los brazos muy doblados" | Estirá más los brazos y apretá **c**. Si calibrás encogido te queda muy poco recorrido para el gesto |

**El teclado nunca deja de funcionar.** Si el script se cae, se cierra o te salís
de cuadro, el juego vuelve solo a A / D / Shift en medio segundo. Eso es a
propósito: en la feria el juego nunca puede quedarse sin control.

## Cómo funciona por dentro

Una cámara común no mide profundidad de verdad (MediaPipe da una coordenada Z
pero es estimada y tiembla mucho), así que **no se usa la Z**. Cuando tirás el
brazo hacia el cuerpo, el codo se **dobla**, y eso se ve perfecto en 2D. Entonces
se mide el ángulo del codo (hombro → codo → muñeca), que usa solo X e Y, que es
donde MediaPipe es preciso.

Con la flexión de cada brazo salen las dos señales, y son **independientes**:

```
dirección = flexión_derecha − flexión_izquierda    (la diferencia)
wheelie   = (flexión_izq + flexión_der) / 2        (el promedio)
```

Doblar cambia la diferencia y deja el promedio quieto; el wheelie cambia el
promedio y deja la diferencia quieta. Por eso un gesto no pisa al otro.

Todo se normaliza contra la pose neutra de la calibración, así funciona igual
para cualquier persona sin volver a configurar nada.

### Por qué el wheelie tiene dos umbrales

La flexión va de 0 (brazo estirado) a 1 (brazo doblado) y **se clava en 1**. Eso
trae un problema: si te sentás con los brazos ya bastante doblados, tu reposo
queda en 0.86 y sólo te quedan 0.14 de recorrido. Con un umbral fijo de 0.16 el
wheelie era **literalmente imposible**, y recalibrar en esa misma postura no
arreglaba nada (fue un bug real, corregido el 2026-09-09).

Por eso ahora hay **dos formas de activarlo, y alcanza con cumplir una**:

| Regla | Qué mide | Para quién sirve |
|---|---|---|
| `WHEELIE_DELTA` | Cuánto doblaste, en absoluto | El que se sienta con los brazos estirados y tiene recorrido de sobra |
| `WHEELIE_FRACCION` | Qué parte del recorrido que **te queda** usaste | El que se sienta encogido y en absoluto no puede doblar mucho más |

La ventana muestra una barra **`wheelie`** con qué tan cerca estás del umbral (la
línea blanca es el 100%), así se ve de una si el problema es que te falta tirar o
que hay que bajar los umbrales.

## Conexión con Unity

El script manda por UDP a `127.0.0.1:5005`, 30 veces por segundo, una línea de
texto con 3 campos:

```
lateral;wheelie;detectado        ej:  -0.350;0;1
```

- `lateral` — float −1..1 (negativo = izquierda)
- `wheelie` — 0 o 1
- `detectado` — 0 o 1 (hay alguien en cuadro y ya terminó de calibrar)

Del lado de Unity lo recibe `Assets/Scripts/EntradaCamara.cs`, y
`PlayerController.LeerLateral()` / `LeerWheelie()` lo usan si está activo.
