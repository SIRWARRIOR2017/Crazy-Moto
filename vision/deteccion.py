"""
Control por camara web para Crazy Moto.

Detecta la pose del jugador con MediaPipe y le manda al juego, por UDP a
localhost, cuanto esta doblando y si esta haciendo el wheelie.

La idea: el jugador agarra un MANUBRIO IMAGINARIO con las dos manos, delante
del pecho, y lo maneja como si fuera de verdad.

  - Doblar  -> girar el manubrio: bajar una mano y subir la otra. Para doblar a
               la derecha baja la mano derecha (igual que un volante).
  - Wheelie -> levantar LAS DOS manos por encima de los hombros.

Como se mide, y por que asi:
Antes se media el angulo del codo. Pero en la posicion de manubrio el antebrazo
apunta hacia la camara: en la imagen casi no se ve (el codo y la muneca quedan
uno encima del otro) y el angulo saltaba con cualquier movimiento chico. Ahora
solo se usan las MUNECAS y los HOMBROS, que de frente se ven siempre bien:

    direccion = inclinacion de la linea que une las dos munecas
    wheelie   = altura de la mano MAS BAJA respecto de los hombros

Las dos medidas son relativas al propio cuerpo: la inclinacion no cambia si te
corres de costado o te acercas a la camara, y la altura se mide en "anchos de
hombro", asi que da lo mismo estar a 1 m o a 2 m. Por eso no hace falta
calibrar: cada uno se sienta y juega.

Uso:
    python deteccion.py                 (camara 0, ventana de control abierta)
    python deteccion.py --camara 1      (si tenes mas de una camara)
    python deteccion.py --sin-ventana   (para la feria, sin ventana de OpenCV)

Teclas en la ventana:
    c = centrar el manubrio (si la moto dobla sola)      q o ESC = salir
"""

import argparse
import math
import socket
import sys
import time
from pathlib import Path

import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision

# ---------------------------------------------------------------------------
# Ajustes (si algo se siente incomodo, se toca aca)
# ---------------------------------------------------------------------------

IP_JUEGO = "127.0.0.1"
PUERTO_JUEGO = 5005

# Cuantos grados hay que girar el manubrio para doblar a fondo. Mas chico = mas
# sensible.
GRADOS_GIRO_MAXIMO = 30.0

# Zona muerta (fraccion del giro maximo): abajo de esto se considera que vas
# derecho, porque nadie tiene las dos manos perfectamente a la misma altura.
ZONA_MUERTA = 0.12

# Suavizado exponencial: 0 = no se mueve nunca, 1 = sin suavizado (tiembla).
SUAVIZADO = 0.5

# Wheelie, medido en anchos de hombro: 0 = la mano mas baja esta a la altura de
# los hombros, positivo = mas arriba, negativo = mas abajo. Con las manos en el
# manubrio, delante del pecho, se esta en mas o menos -0.8.
# Entra al pasar WHEELIE_ENTRA y sale recien al bajar de WHEELIE_SALE
# (histeresis, para que no titile justo en el borde).
WHEELIE_ENTRA = 0.0
WHEELIE_SALE = -0.30

# Si las munecas quedan mas cerca que esto (en anchos de hombro), la linea entre
# ellas es tan corta que su inclinacion no es confiable: se mantiene la
# direccion anterior.
SEPARACION_MINIMA_MANOS = 0.35

# Si la persona se ve mas chica que esto (ancho de hombros en pixeles), esta
# demasiado lejos o es alguien que pasa por atras.
ANCHO_HOMBROS_MINIMO = 40.0

# Cuanta confianza pedimos para dar por buenos los puntos del cuerpo.
VISIBILIDAD_HOMBROS = 0.5
VISIBILIDAD_MANOS = 0.35

# Si perdemos los puntos por menos tiempo que esto, mantenemos la ultima lectura
# buena en vez de cortar. Sin esto, un parpadeo de la deteccion te tiraba de
# golpe al teclado.
SEGUNDOS_DE_GRACIA = 0.6

# En la feria va a haber gente pasando por atras. Se detectan hasta estas
# personas y se juega con la mas cercana a la camara.
MAX_PERSONAS = 3

# Hasta donde se deja "centrar" el manubrio con la tecla c.
CENTRADO_MAXIMO = 20.0

# Indices de los puntos que usamos (BlazePose, 33 puntos). No importa cual es
# "izquierdo" y cual "derecho" para MediaPipe: las manos se ordenan por su
# posicion en la imagen (ver medir()).
HOMBRO_A, HOMBRO_B = 11, 12
MUNECA_A, MUNECA_B = 15, 16

RUTA_MODELO = Path(__file__).parent / "modelo" / "pose_landmarker_lite.task"


# ---------------------------------------------------------------------------
# Calculo de las senales
# ---------------------------------------------------------------------------

def visibilidad(marca):
    return getattr(marca, "visibility", 1.0)


def elegir_persona(poses, ancho, alto):
    """
    De todas las personas detectadas, se queda con la mas cercana a la camara:
    la que tiene los hombros mas anchos en la imagen. Devuelve None si nadie se
    ve bien.
    """
    mejor, mejor_ancho = None, ANCHO_HOMBROS_MINIMO
    for marcas in poses:
        if len(marcas) <= MUNECA_B:
            continue
        ha, hb = marcas[HOMBRO_A], marcas[HOMBRO_B]
        if visibilidad(ha) < VISIBILIDAD_HOMBROS or visibilidad(hb) < VISIBILIDAD_HOMBROS:
            continue
        a = math.hypot((ha.x - hb.x) * ancho, (ha.y - hb.y) * alto)
        if a > mejor_ancho:
            mejor, mejor_ancho = marcas, a
    return mejor


def puntos_en_pixeles(marcas, ancho, alto):
    """
    Pasa los hombros y las munecas a pixeles. Se trabaja en pixeles y no en las
    coordenadas normalizadas (0..1) porque esas estan estiradas por la forma de
    la imagen y los angulos saldrian mal.
    Devuelve (puntos, manos_visibles).
    """
    puntos = {i: (marcas[i].x * ancho, marcas[i].y * alto)
              for i in (HOMBRO_A, HOMBRO_B, MUNECA_A, MUNECA_B)}
    manos = (visibilidad(marcas[MUNECA_A]) >= VISIBILIDAD_MANOS and
             visibilidad(marcas[MUNECA_B]) >= VISIBILIDAD_MANOS)
    return puntos, manos


def medir(puntos):
    """
    Devuelve (giro, altura):
      giro   = inclinacion del manubrio en grados (positivo = a la derecha), o
               None si las manos estan demasiado juntas para medirla.
      altura = altura de la mano mas baja respecto de los hombros, en anchos de
               hombro (0 = a la altura de los hombros, positivo = mas arriba).
    """
    ha, hb = puntos[HOMBRO_A], puntos[HOMBRO_B]
    ancho_hombros = max(1.0, math.hypot(ha[0] - hb[0], ha[1] - hb[1]))

    # La imagen NO esta espejada: la mano derecha del jugador aparece a la
    # izquierda de la imagen. Ordenando por X no dependemos de que MediaPipe
    # acierte cual es cada mano.
    mano_der, mano_izq = sorted((puntos[MUNECA_A], puntos[MUNECA_B]))

    dx = mano_izq[0] - mano_der[0]
    # La Y de la imagen crece hacia abajo: positivo = la mano derecha esta mas
    # abajo, o sea que el manubrio gira a la derecha.
    dy = mano_der[1] - mano_izq[1]
    giro = math.degrees(math.atan2(dy, dx)) if dx >= SEPARACION_MINIMA_MANOS * ancho_hombros else None

    y_hombros = (ha[1] + hb[1]) / 2.0
    altura = (y_hombros - max(mano_der[1], mano_izq[1])) / ancho_hombros
    return giro, altura


def aplicar_zona_muerta(v):
    """Corta el ruido cerca del cero y reescala, para que no haya un salto."""
    if abs(v) < ZONA_MUERTA:
        return 0.0
    signo = 1.0 if v > 0 else -1.0
    return signo * (abs(v) - ZONA_MUERTA) / (1.0 - ZONA_MUERTA)


class Control:
    """Lo que se le manda al juego, con el estado que se arrastra entre frames."""

    def __init__(self):
        self.direccion = 0.0
        self.wheelie = False
        self.detectado = False
        self.centro = 0.0          # giro que cuenta como "derecho" (tecla c)
        self.ultimo_giro = 0.0     # ultima lectura cruda, para centrar
        self.ultima_altura = -1.0
        self.ultimo_visto = -999.0

    def actualizar(self, giro, altura, ahora):
        """Lectura buena de las dos manos."""
        self.ultimo_visto = ahora
        self.detectado = True
        self.ultima_altura = altura

        if giro is not None:
            self.ultimo_giro = giro
            objetivo = aplicar_zona_muerta((giro - self.centro) / GRADOS_GIRO_MAXIMO)
            objetivo = max(-1.0, min(1.0, objetivo))
            self.direccion += (objetivo - self.direccion) * SUAVIZADO
        # Si giro es None (manos muy juntas) se mantiene la direccion anterior.

        if self.wheelie:
            self.wheelie = altura > WHEELIE_SALE
        else:
            self.wheelie = altura > WHEELIE_ENTRA

    def sin_lectura(self, ahora):
        """Este cuadro no se vio a nadie (o no se le vieron las manos)."""
        if ahora - self.ultimo_visto < SEGUNDOS_DE_GRACIA:
            return   # parpadeo corto: se mantiene la ultima lectura

        # Te perdimos de verdad: soltamos todo y el juego vuelve al teclado.
        self.direccion += (0.0 - self.direccion) * SUAVIZADO
        self.wheelie = False
        self.detectado = False

    def centrar(self):
        self.centro = max(-CENTRADO_MAXIMO, min(CENTRADO_MAXIMO, self.ultimo_giro))
        self.direccion = 0.0

    def mensaje(self):
        lateral = self.direccion if self.detectado else 0.0
        return f"{lateral:.3f};{1 if self.wheelie else 0};{1 if self.detectado else 0}"


# ---------------------------------------------------------------------------
# Dibujo de la ventana de control (todo el texto en ASCII: cv2.putText no
# dibuja acentos ni enies, salen como basura)
# ---------------------------------------------------------------------------

def dibujar(imagen, puntos, manos, control, fps):
    alto, ancho = imagen.shape[:2]

    if puntos is not None:
        ha = tuple(int(v) for v in puntos[HOMBRO_A])
        hb = tuple(int(v) for v in puntos[HOMBRO_B])
        cv2.line(imagen, ha, hb, (200, 200, 200), 2)

        # Linea del wheelie: a la altura de los hombros. Si las dos manos pasan
        # por arriba de esta linea, se activa.
        ancho_h = max(1.0, math.hypot(ha[0] - hb[0], ha[1] - hb[1]))
        y_linea = int((ha[1] + hb[1]) / 2.0 - WHEELIE_ENTRA * ancho_h)
        x_ini = int(min(ha[0], hb[0]) - ancho_h)
        x_fin = int(max(ha[0], hb[0]) + ancho_h)
        color_linea = (40, 190, 90) if control.wheelie else (255, 255, 255)
        cv2.line(imagen, (x_ini, y_linea), (x_fin, y_linea), color_linea, 1)
        cv2.putText(imagen, "WHEELIE", (x_fin + 6, y_linea + 5), cv2.FONT_HERSHEY_SIMPLEX,
                    0.45, color_linea, 1)

        if manos:
            ma = tuple(int(v) for v in puntos[MUNECA_A])
            mb = tuple(int(v) for v in puntos[MUNECA_B])
            # El "manubrio": la linea entre las dos manos.
            cv2.line(imagen, ma, mb, (255, 200, 100), 5)
            for p in (ma, mb):
                cv2.circle(imagen, p, 11, (255, 255, 255), -1)
                cv2.circle(imagen, p, 11, (255, 200, 100), 2)

    # Barra de direccion abajo
    cy = alto - 40
    x0, x1 = 60, ancho - 60
    centro = (x0 + x1) // 2
    cv2.rectangle(imagen, (x0, cy - 14), (x1, cy + 14), (60, 60, 60), -1)
    cv2.line(imagen, (centro, cy - 16), (centro, cy + 16), (150, 150, 150), 2)

    mx = int(centro + control.direccion * (x1 - x0) / 2.0)
    cv2.rectangle(imagen, (mx - 9, cy - 16), (mx + 9, cy + 16), (255, 200, 100), -1)
    cv2.putText(imagen, "IZQ", (x0 - 48, cy + 8), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (180, 180, 180), 1)
    cv2.putText(imagen, "DER", (x1 + 8, cy + 8), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (180, 180, 180), 1)

    # Estado arriba
    if puntos is None:
        texto, color = "NO TE VEO - ponete frente a la camara", (0, 120, 255)
    elif not manos:
        texto, color = "NO VEO TUS MANOS - alejate un poco y subilas al pecho", (0, 200, 255)
    else:
        texto, color = "OK - te estoy viendo", (0, 220, 0)
    cv2.putText(imagen, texto, (16, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.62, color, 2)

    cv2.putText(imagen, f"direccion: {control.direccion:+.2f}   manos: {control.ultima_altura:+.2f}",
                (16, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (230, 230, 230), 1)
    cv2.putText(imagen, f"{fps:.0f} fps", (ancho - 90, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.55,
                (0, 220, 0) if fps >= 20 else (0, 120, 255), 1)

    if control.wheelie:
        cv2.rectangle(imagen, (12, 76), (210, 110), (40, 190, 90), -1)
        cv2.putText(imagen, "WHEELIE", (34, 101), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (255, 255, 255), 2)

    cv2.putText(imagen, "c = centrar el manubrio    q = salir", (16, alto - 70),
                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (170, 170, 170), 1)


# ---------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description="Control por camara para Crazy Moto")
    ap.add_argument("--camara", type=int, default=0, help="indice de la camara (0 por defecto)")
    ap.add_argument("--puerto", type=int, default=PUERTO_JUEGO, help="puerto UDP del juego")
    ap.add_argument("--sin-ventana", action="store_true", help="no abrir la ventana de control")
    args = ap.parse_args()

    if not RUTA_MODELO.exists():
        print(f"ERROR: falta el modelo en {RUTA_MODELO}")
        print("Bajalo con el comando que esta en vision/README.md")
        return 1

    camara = cv2.VideoCapture(args.camara, cv2.CAP_DSHOW)   # DSHOW abre mucho mas rapido en Windows
    if not camara.isOpened():
        print(f"ERROR: no pude abrir la camara {args.camara}.")
        print("Proba con --camara 1, o fijate que no la tenga abierta otro programa.")
        return 1

    camara.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    camara.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
    # Sin esto la camara acumula cuadros viejos y el control va con retraso.
    camara.set(cv2.CAP_PROP_BUFFERSIZE, 1)

    opciones = vision.PoseLandmarkerOptions(
        base_options=mp_python.BaseOptions(model_asset_path=str(RUTA_MODELO)),
        running_mode=vision.RunningMode.VIDEO,
        num_poses=MAX_PERSONAS,
    )

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    destino = (IP_JUEGO, args.puerto)

    control = Control()
    marca_tiempo = 0        # timestamp para MediaPipe, siempre creciente
    fps = 0.0
    antes = time.perf_counter()

    print(f"Mandando a {IP_JUEGO}:{args.puerto}. Agarra el manubrio imaginario y juga.")

    with vision.PoseLandmarker.create_from_options(opciones) as detector:
        while True:
            ok, cuadro = camara.read()
            if not ok:
                print("ERROR: se corto la camara.")
                break

            alto, ancho = cuadro.shape[:2]

            # Detectamos sobre la imagen SIN espejar (medir() cuenta con eso).
            rgb = cv2.cvtColor(cuadro, cv2.COLOR_BGR2RGB)
            imagen_mp = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

            # detect_for_video exige timestamps ESTRICTAMENTE crecientes: si dos
            # cuadros caen en el mismo milisegundo, tira excepcion y se corta todo.
            marca_tiempo = max(marca_tiempo + 1, int(time.perf_counter() * 1000))
            resultado = detector.detect_for_video(imagen_mp, marca_tiempo)

            ahora = time.perf_counter()
            fps += (1.0 / max(1e-3, ahora - antes) - fps) * 0.1
            antes = ahora

            puntos, manos = None, False
            persona = elegir_persona(resultado.pose_landmarks, ancho, alto)
            if persona is not None:
                puntos, manos = puntos_en_pixeles(persona, ancho, alto)

            if manos:
                giro, altura = medir(puntos)
                control.actualizar(giro, altura, ahora)
            else:
                control.sin_lectura(ahora)

            sock.sendto(control.mensaje().encode("ascii"), destino)

            if not args.sin_ventana:
                # Espejamos solo para mostrar, asi te ves como en un espejo. Los
                # puntos hay que espejarlos tambien para que caigan encima tuyo.
                vista = cv2.flip(cuadro, 1)
                puntos_vista = None
                if puntos is not None:
                    puntos_vista = {i: (ancho - x, y) for i, (x, y) in puntos.items()}

                dibujar(vista, puntos_vista, manos, control, fps)
                cv2.imshow("Crazy Moto - control por camara", vista)

                tecla = cv2.waitKey(1) & 0xFF
                if tecla in (ord("q"), 27):
                    break
                if tecla == ord("c"):
                    control.centrar()
                    print(f"Manubrio centrado en {control.centro:+.1f} grados.")

    camara.release()
    cv2.destroyAllWindows()
    sock.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
