"""
Control por camara web para Crazy Moto.

Detecta la pose del jugador con MediaPipe y le manda al juego, por UDP a
localhost, cuanto esta doblando y si esta haciendo el wheelie.

La idea: el jugador se sienta frente a la camara y sube los brazos como si
agarrara un manubrio de verdad.

  - Doblar  -> tira UN brazo hacia el cuerpo y empuja el otro hacia adelante
               (igual que un manubrio real).
  - Wheelie -> tira LOS DOS brazos hacia el cuerpo al mismo tiempo.

Como se mide, y por que asi:
Una camara comun no mide profundidad de verdad (MediaPipe da una coordenada Z
pero es estimada y tiembla mucho). Pero cuando tiras el brazo hacia el cuerpo el
codo se DOBLA, y eso se ve perfecto en 2D. Asi que medimos el angulo del codo
(hombro - codo - muneca), que usa solo X e Y, que es donde MediaPipe es preciso.

Con la flexion de cada brazo salen las dos senales, y son independientes:
    direccion = flexion_derecha - flexion_izquierda   (la diferencia)
    wheelie   = (flexion_izq + flexion_der) / 2       (el promedio)
Doblar cambia la diferencia y deja el promedio quieto; el wheelie cambia el
promedio y deja la diferencia quieta. Por eso un gesto no pisa al otro.

Uso:
    python deteccion.py                 (camara 0, ventana de control abierta)
    python deteccion.py --camara 1      (si tenes mas de una camara)
    python deteccion.py --sin-ventana   (para la feria, sin ventana de OpenCV)

Teclas en la ventana:
    c = recalibrar la pose neutra      q o ESC = salir
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

# Angulos del codo, en grados: con el brazo estirado y con el brazo bien doblado.
# Entre esos dos valores se reparte la "flexion" de 0 a 1.
ANGULO_ESTIRADO = 165.0
ANGULO_DOBLADO = 55.0

# La gente no hace el recorrido completo, asi que amplificamos un poco.
GANANCIA_DIRECCION = 2.2

# Zona muerta: abajo de esto se considera que vas derecho (si no, la moto
# tiembla porque nadie se queda perfectamente simetrico).
ZONA_MUERTA = 0.10

# Suavizado exponencial: 0 = no se mueve nunca, 1 = sin suavizado (tiembla).
SUAVIZADO = 0.40

# Wheelie: cuanto mas doblados que en reposo tienen que estar LOS DOS brazos.
# Son dos umbrales (histeresis) para que no titile prendido/apagado en el borde.
WHEELIE_ENCIENDE = 0.16
WHEELIE_APAGA = 0.10

# Cuanta confianza pedimos para dar por buenos los puntos del cuerpo.
VISIBILIDAD_MINIMA = 0.5

SEGUNDOS_CALIBRACION = 2.0

# Indices de los puntos que usamos (BlazePose, 33 puntos).
# Ojo: "izquierdo" y "derecho" son del lado real de la persona.
HOMBRO_IZQ, HOMBRO_DER = 11, 12
CODO_IZQ, CODO_DER = 13, 14
MUNECA_IZQ, MUNECA_DER = 15, 16

PUNTOS_NECESARIOS = (HOMBRO_IZQ, HOMBRO_DER, CODO_IZQ, CODO_DER, MUNECA_IZQ, MUNECA_DER)

RUTA_MODELO = Path(__file__).parent / "modelo" / "pose_landmarker_lite.task"


# ---------------------------------------------------------------------------
# Calculo de las senales
# ---------------------------------------------------------------------------

def angulo(a, b, c):
    """Angulo en grados en el vertice b, formado por a-b-c. 180 = estirado."""
    v1 = (a[0] - b[0], a[1] - b[1])
    v2 = (c[0] - b[0], c[1] - b[1])

    n1 = math.hypot(*v1)
    n2 = math.hypot(*v2)
    if n1 < 1e-6 or n2 < 1e-6:
        return 180.0

    coseno = (v1[0] * v2[0] + v1[1] * v2[1]) / (n1 * n2)
    return math.degrees(math.acos(max(-1.0, min(1.0, coseno))))


def flexion(ang):
    """Pasa el angulo del codo a 0..1. 0 = brazo estirado, 1 = brazo doblado."""
    t = (ANGULO_ESTIRADO - ang) / (ANGULO_ESTIRADO - ANGULO_DOBLADO)
    return max(0.0, min(1.0, t))


def aplicar_zona_muerta(v):
    """Corta el ruido cerca del cero y reescala, para que no haya un salto."""
    if abs(v) < ZONA_MUERTA:
        return 0.0
    signo = 1.0 if v > 0 else -1.0
    return signo * (abs(v) - ZONA_MUERTA) / (1.0 - ZONA_MUERTA)


def puntos_en_pixeles(marcas, ancho, alto):
    """
    Pasa las marcas a pixeles. Se trabaja en pixeles y no en las coordenadas
    normalizadas (0..1) porque esas estan estiradas por la forma de la imagen y
    los angulos saldrian mal.
    Devuelve None si falta algun punto o no se ve con confianza.
    """
    puntos = {}
    for i in PUNTOS_NECESARIOS:
        if i >= len(marcas):
            return None
        m = marcas[i]
        if getattr(m, "visibility", 1.0) < VISIBILIDAD_MINIMA:
            return None
        puntos[i] = (m.x * ancho, m.y * alto)
    return puntos


# ---------------------------------------------------------------------------
# Dibujo de la ventana de control (todo el texto en ASCII: cv2.putText no
# dibuja acentos ni enies, salen como basura)
# ---------------------------------------------------------------------------

def dibujar(imagen, puntos, direccion, wheelie, calibrando, detectado):
    alto, ancho = imagen.shape[:2]

    if puntos is not None:
        brazos = ((HOMBRO_IZQ, CODO_IZQ, MUNECA_IZQ), (HOMBRO_DER, CODO_DER, MUNECA_DER))
        for hombro, codo, muneca in brazos:
            p1 = tuple(int(v) for v in puntos[hombro])
            p2 = tuple(int(v) for v in puntos[codo])
            p3 = tuple(int(v) for v in puntos[muneca])
            cv2.line(imagen, p1, p2, (0, 220, 255), 3)
            cv2.line(imagen, p2, p3, (0, 220, 255), 3)
            for p in (p1, p2, p3):
                cv2.circle(imagen, p, 7, (255, 255, 255), -1)

        hi = tuple(int(v) for v in puntos[HOMBRO_IZQ])
        hd = tuple(int(v) for v in puntos[HOMBRO_DER])
        cv2.line(imagen, hi, hd, (200, 200, 200), 2)

    # Barra de direccion abajo
    cy = alto - 40
    x0, x1 = 60, ancho - 60
    centro = (x0 + x1) // 2
    cv2.rectangle(imagen, (x0, cy - 14), (x1, cy + 14), (60, 60, 60), -1)
    cv2.line(imagen, (centro, cy - 16), (centro, cy + 16), (150, 150, 150), 2)

    mx = int(centro + direccion * (x1 - x0) / 2.0)
    cv2.rectangle(imagen, (mx - 9, cy - 16), (mx + 9, cy + 16), (255, 200, 100), -1)
    cv2.putText(imagen, "IZQ", (x0 - 48, cy + 8), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (180, 180, 180), 1)
    cv2.putText(imagen, "DER", (x1 + 8, cy + 8), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (180, 180, 180), 1)

    # Estado arriba
    if calibrando:
        texto, color = "CALIBRANDO - quedate quieto en posicion de manubrio", (0, 200, 255)
    elif not detectado:
        texto, color = "NO TE VEO - acomodate frente a la camara", (0, 120, 255)
    else:
        texto, color = "OK - te estoy viendo", (0, 220, 0)
    cv2.putText(imagen, texto, (16, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.62, color, 2)

    cv2.putText(imagen, f"direccion: {direccion:+.2f}", (16, 60),
                cv2.FONT_HERSHEY_SIMPLEX, 0.55, (230, 230, 230), 1)

    if wheelie:
        cv2.rectangle(imagen, (12, 74), (210, 108), (40, 190, 90), -1)
        cv2.putText(imagen, "WHEELIE", (34, 99), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (255, 255, 255), 2)

    cv2.putText(imagen, "c = recalibrar    q = salir", (16, alto - 70),
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

    opciones = vision.PoseLandmarkerOptions(
        base_options=mp_python.BaseOptions(model_asset_path=str(RUTA_MODELO)),
        running_mode=vision.RunningMode.VIDEO,
        num_poses=1,
    )

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    destino = (IP_JUEGO, args.puerto)

    # Estado que se arrastra entre frames
    direccion = 0.0
    media_suave = 0.0
    wheelie = False

    # Calibracion de la pose neutra. La direccion casi no la necesita (es una
    # resta, ya da cero sola si estas simetrico), pero el wheelie si: hay que
    # saber cuan doblados tenes los brazos en reposo.
    base_diferencia = 0.0
    base_media = 0.0
    muestras = []
    calibrando = True
    inicio_calibracion = None

    print(f"Mandando a {IP_JUEGO}:{args.puerto}. Ponete en posicion de manubrio...")

    with vision.PoseLandmarker.create_from_options(opciones) as detector:
        while True:
            ok, cuadro = camara.read()
            if not ok:
                print("ERROR: se corto la camara.")
                break

            alto, ancho = cuadro.shape[:2]

            # Detectamos sobre la imagen SIN espejar, para que "izquierdo" y
            # "derecho" de MediaPipe sean los lados reales de la persona.
            rgb = cv2.cvtColor(cuadro, cv2.COLOR_BGR2RGB)
            imagen_mp = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
            resultado = detector.detect_for_video(imagen_mp, int(time.perf_counter() * 1000))

            puntos = None
            if resultado.pose_landmarks:
                puntos = puntos_en_pixeles(resultado.pose_landmarks[0], ancho, alto)

            detectado = puntos is not None

            if detectado:
                flex_izq = flexion(angulo(puntos[HOMBRO_IZQ], puntos[CODO_IZQ], puntos[MUNECA_IZQ]))
                flex_der = flexion(angulo(puntos[HOMBRO_DER], puntos[CODO_DER], puntos[MUNECA_DER]))

                # Tiras el brazo derecho y empujas el izquierdo -> doblas a la derecha.
                diferencia = flex_der - flex_izq
                media = (flex_izq + flex_der) / 2.0

                if calibrando:
                    if inicio_calibracion is None:
                        inicio_calibracion = time.time()
                    muestras.append((diferencia, media))

                    if time.time() - inicio_calibracion >= SEGUNDOS_CALIBRACION:
                        base_diferencia = sum(m[0] for m in muestras) / len(muestras)
                        base_media = sum(m[1] for m in muestras) / len(muestras)
                        media_suave = base_media
                        muestras.clear()
                        calibrando = False
                        print("Calibrado. Ya podes jugar.")
                else:
                    objetivo = aplicar_zona_muerta((diferencia - base_diferencia) * GANANCIA_DIRECCION)
                    objetivo = max(-1.0, min(1.0, objetivo))
                    direccion += (objetivo - direccion) * SUAVIZADO

                    media_suave += (media - media_suave) * SUAVIZADO
                    extra = media_suave - base_media
                    # Histeresis: sube con un umbral y baja con otro mas bajo.
                    if wheelie:
                        wheelie = extra > WHEELIE_APAGA
                    else:
                        wheelie = extra > WHEELIE_ENCIENDE
            else:
                # Si te perdemos, soltamos todo: el juego vuelve solo al teclado.
                direccion += (0.0 - direccion) * SUAVIZADO
                wheelie = False

            enviable = direccion if (detectado and not calibrando) else 0.0
            mensaje = f"{enviable:.3f};{1 if wheelie else 0};{1 if (detectado and not calibrando) else 0}"
            sock.sendto(mensaje.encode("ascii"), destino)

            if not args.sin_ventana:
                # Espejamos solo para mostrar, asi te ves como en un espejo. Los
                # puntos hay que espejarlos tambien para que caigan encima tuyo.
                vista = cv2.flip(cuadro, 1)
                puntos_vista = None
                if puntos is not None:
                    puntos_vista = {i: (ancho - x, y) for i, (x, y) in puntos.items()}

                dibujar(vista, puntos_vista, direccion, wheelie, calibrando, detectado)
                cv2.imshow("Crazy Moto - control por camara", vista)

                tecla = cv2.waitKey(1) & 0xFF
                if tecla in (ord("q"), 27):
                    break
                if tecla == ord("c"):
                    calibrando = True
                    inicio_calibracion = None
                    muestras.clear()
                    direccion = 0.0
                    wheelie = False
                    print("Recalibrando...")

    camara.release()
    cv2.destroyAllWindows()
    sock.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
