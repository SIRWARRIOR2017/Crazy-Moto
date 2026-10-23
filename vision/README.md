# Control por cámara — Crazy Moto

Reemplaza al manubrio de Arduino: la webcam detecta los movimientos del jugador y
se los manda al juego. Sale $0 porque usa la cámara que ya tiene la notebook.

## Cómo se juega

Sentate frente a la cámara y agarrá un **manubrio imaginario** con las dos manos,
delante del pecho.

| Acción | Gesto |
|---|---|
| Doblar a la derecha | Girás el manubrio a la derecha: **bajás la mano derecha** y subís la izquierda |
| Doblar a la izquierda | Al revés: bajás la **izquierda** y subís la derecha |
| Wheelie | Levantás **las dos manos** por encima de los hombros |

No hace falta calibrar nada: cada uno se sienta y juega. Te podés correr de
costado, acercarte o alejarte de la cámara y el gesto se lee igual.

## Instalación (una sola vez)

```
python -m pip install -r vision/requirements.txt
```

El archivo del modelo (`vision/modelo/pose_landmarker_lite.task`, 5,5 MB) ya está
en el repo, así que no hay que bajar nada más. Si alguna vez falta:

```
curl -L -o vision/modelo/pose_landmarker_lite.task https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task
```

Probado con Python 3.14 y MediaPipe 1.0.1 / 1.1.1 en Windows 11.

## Cómo se usa

1. Abrí el detector **antes** que el juego (o en cualquier momento, da igual):

   ```
   python vision/deteccion.py
   ```

2. Ponete de modo que se vean **tus hombros y tus dos manos**. Cuando la ventana
   dice `OK - te estoy viendo`, ya podés jugar.

3. En el juego, entrá a **Opciones → Probar cámara** para verificar que te está
   leyendo bien y acomodarte antes de empezar.

Opciones del script:

```
python vision/deteccion.py --camara 1        # si tenés más de una cámara
python vision/deteccion.py --puerto 5005     # puerto UDP (por defecto 5005)
python vision/deteccion.py --sin-ventana     # sin la ventana de OpenCV (para la feria)
```

Teclas dentro de la ventana: **c** centra el manubrio, **q** cierra.

La ventana dibuja el manubrio (la línea entre tus manos), una línea blanca a la
altura de los hombros que marca dónde se activa el wheelie, y los **fps** del
detector: si bajan de 20 el control se siente con retraso (cerrá otros programas).

## Si algo no anda

| Síntoma | Qué pasa |
|---|---|
| El juego dice "SIN SEÑAL" | El script no está corriendo, o lo abriste con otro `--puerto` |
| "CONECTADA, PERO NO TE VEO" | Falta luz, o no entran los hombros y las manos en el cuadro |
| La ventana dice "NO VEO TUS MANOS" | Estás muy cerca y las manos quedan abajo, fuera del cuadro. Alejate un poco o subí las manos al pecho |
| `no pude abrir la camara 0` | Otro programa la tiene abierta (Zoom, Meet, Teams), o probá `--camara 1` |
| La moto dobla sola | La cámara está torcida: poné las manos derechas y apretá **c** |
| Dobla muy poco / muy fuerte | Tocá `GRADOS_GIRO_MAXIMO` arriba de `deteccion.py` (más chico = más sensible) |
| El wheelie cuesta / se activa solo | Tocá `WHEELIE_ENTRA` (más bajo = más fácil). El número `manos:` de la ventana muestra dónde estás |
| Agarra a otra persona | Se juega con la persona más cercana a la cámara. Que no haya nadie más cerca que el jugador |

**El teclado nunca deja de funcionar.** Si el script se cae, se cierra o te salís
de cuadro, el juego vuelve solo a A / D / Shift. Eso es a propósito: en la feria
el juego nunca puede quedarse sin control.

## Cómo funciona por dentro

De todos los puntos del cuerpo que detecta MediaPipe se usan solo **cuatro**: los
dos hombros y las dos muñecas. Son los que mejor se ven de frente.

```
dirección = inclinación de la línea que une las dos muñecas
wheelie   = altura de la mano más baja respecto de los hombros
```

- La **inclinación** es un ángulo: no cambia si te corrés de costado ni si te
  acercás o te alejás de la cámara. Con 30° (`GRADOS_GIRO_MAXIMO`) se dobla a
  fondo, y abajo de ~4° se considera que vas derecho (zona muerta).
- La **altura** se mide en *anchos de hombro* (0 = a la altura de los hombros,
  positivo = más arriba). Así da lo mismo estar a 1 m o a 2 m: cuanto más lejos,
  más chicos se ven los hombros y la distancia de las manos, en la misma
  proporción. Con las manos en el manubrio estás en −0,8 más o menos; el wheelie
  entra al pasar de 0 y sale recién al bajar de −0,3 (histéresis, para que no
  titile en el borde). Se mira la mano **más baja** para que haga falta levantar
  las dos.
- Las manos se ordenan por su posición en la imagen, así no depende de que
  MediaPipe acierte cuál es la izquierda y cuál la derecha.

### Por qué se cambió (2026-10-23)

La primera versión medía el **ángulo del codo** (tirar un brazo y empujar el
otro). Con una sola cámara no se puede medir la profundidad, y la idea era que
al tirar el brazo el codo se dobla. El problema: en la posición de manubrio el
antebrazo apunta **hacia la cámara**, así que en la imagen el codo y la muñeca
quedan casi uno encima del otro y el ángulo saltaba con cualquier movimiento
chico. Encima dependía de una calibración: si te acomodabas distinto después de
calibrar, dejaba de responder. Por eso se pasó al manubrio en el aire.

### Robustez para la feria

- **Varias personas:** se detectan hasta 3 y se juega con la más cercana (la de
  hombros más anchos en la imagen). Alguien que pasa por atrás no roba el control.
- **Parpadeos:** si se pierden las manos menos de 0,6 s, se mantiene la última
  lectura en vez de cortar.
- **Manos muy juntas o cruzadas:** la inclinación no es confiable y se mantiene
  la dirección anterior.
- **Retraso:** la cámara se abre con un buffer de un solo cuadro, así no se
  acumulan imágenes viejas.

## Conexión con Unity

El script manda por UDP a `127.0.0.1:5005`, una vez por cuadro de la cámara, una
línea de texto con 3 campos:

```
lateral;wheelie;detectado        ej:  -0.350;0;1
```

- `lateral` — float −1..1 (negativo = izquierda)
- `wheelie` — 0 o 1
- `detectado` — 0 o 1 (se ven los hombros y las dos manos del jugador)

Del lado de Unity lo recibe `Assets/Scripts/EntradaCamara.cs`, y
`PlayerController.LeerLateral()` / `LeerWheelie()` lo usan si está activo.
