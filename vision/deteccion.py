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

# Wheelie: hay DOS formas de activarlo y alcanza con cumplir una sola. Ninguna
# regla sola sirve para todo el mundo, porque cada uno se sienta distinto:
#
#   WHEELIE_DELTA    -> cuanto doblaste los brazos, en terminos absolutos.
#                       Sirve para el que se sienta con los brazos estirados, que
#                       tiene recorrido de sobra.
#   WHEELIE_FRACCION -> que parte del recorrido que TE QUEDA usaste.
#                       Sirve para el que se sienta encogido: en absoluto no puede
#                       doblar mucho mas, pero si puede usar casi todo lo que le
#                       queda.
#
# Con un umbral fijo solo (como estaba antes), el que se sentaba con los brazos ya
# doblados no llegaba NUNCA: la flexion se clava en 1, le quedaba menos recorrido
# que el umbral y el wheelie era imposible. Peor todavia, recalibrar en esa misma
# postura lo dejaba igual de muerto.
WHEELIE_DELTA = 0.16
WHEELIE_FRACCION = 0.50

# Con cuanta intensidad se APAGA, donde 1.0 es el umbral de encendido. Histeresis,
# para que no titile prendido/apagado justo en el borde.
WHEELIE_SALIDA = 0.62

# Piso del recorrido que se asume disponible, para no dividir por casi cero si
# alguien calibra con los brazos muy cerrados.
MARGEN_MINIMO = 0.18

# Si en la calibracion la flexion promedio supera esto, la persona se sento con
# los brazos demasiado doblados y casi no le queda recorrido: se le avisa.
BASE_DEMASIADO_CERRADA = 0.80

# Cuanta confianza pedimos para dar por buenos los puntos del cuerpo. Los hombros
# se ven casi siempre; las munecas se tapan contra el torso justo cuando hacer el
# wheelie, asi que con los brazos somos mas permisivos.
VISIBILIDAD_HOMBROS = 0.5
VISIBILIDAD_BRAZOS = 0.3

# Si perdemos los puntos por menos tiempo que esto, mantenemos la ultima lectura
# buena en vez de cortar. Sin esto, un parpadeo de la deteccion en pleno wheelie
# te tiraba de golpe al teclado.
SEGUNDOS_DE_GRACIA = 0.5

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
        minimo = VISIBILIDAD_HOMBROS if i in (HOMBRO_IZQ, HOMBRO_DER) else VISIBILIDAD_BRAZOS
        if getattr(m, "visibility", 1.0) < minimo:
            return None
        puntos[i] = (m.x * ancho, m.y * alto)
    return puntos


# ---------------------------------------------------------------------------
# Dibujo de la ventana de control (todo el texto en ASCII: cv2.putText no
# dibuja acentos ni enies, salen como basura)
# ---------------------------------------------------------------------------

def dibujar(imagen, puntos, direccion, wheelie, calibrando, detectado, progreso):
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

    # Barra de wheelie: que tan cerca estas de activarlo. La linea blanca es el
    # umbral, o sea el 100%. Si tiras a fondo y la barra no llega a la linea, hay
    # que bajar WHEELIE_DELTA o WHEELIE_FRACCION.
    ESCALA = 1.5   # la barra llega hasta el 150% del umbral
    by, bx0, bx1 = 88, 16, 226
    cv2.rectangle(imagen, (bx0, by - 12), (bx1, by + 12), (60, 60, 60), -1)
    lleno = int((bx1 - bx0) * max(0.0, min(1.0, progreso / ESCALA)))
    if lleno > 0:
        color = (40, 190, 90) if wheelie else (60, 160, 220)
        cv2.rectangle(imagen, (bx0, by - 12), (bx0 + lleno, by + 12), color, -1)
    umbral_x = int(bx0 + (bx1 - bx0) / ESCALA)
    cv2.line(imagen, (umbral_x, by - 15), (umbral_x, by + 15), (255, 255, 255), 2)
    cv2.putText(imagen, f"wheelie {progreso * 100:.0f}%", (bx1 + 10, by + 7),
                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (200, 200, 200), 1)

    if wheelie:
        cv2.rectangle(imagen, (12, 112), (210, 146), (40, 190, 90), -1)
        cv2.putText(imagen, "WHEELIE", (34, 137), cv2.FONT_HERSHEY_SIMPLEX, 0.75, (255, 255, 255), 2)

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
    progreso = 0.0          # cuanto del recorrido de wheelie estas usando (0..1)
    ultimo_visto = 0.0      # cuando fue la ultima vez que te vimos bien
    marca_tiempo = 0        # timestamp para MediaPipe, siempre creciente

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

            # detect_for_video exige timestamps ESTRICTAMENTE crecientes: si dos
            # cuadros caen en el mismo milisegundo, tira excepcion y se corta todo.
            marca_tiempo = max(marca_tiempo + 1, int(time.perf_counter() * 1000))
            resultado = detector.detect_for_video(imagen_mp, marca_tiempo)

            puntos = None
            if resultado.pose_landmarks:
                puntos = puntos_en_pixeles(resultado.pose_landmarks[0], ancho, alto)

            ahora = time.time()

            if puntos is not None:
                ultimo_visto = ahora
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

                        # Si calibraste con los brazos ya muy cerrados, casi no te
                        # queda recorrido para el wheelie. Avisamos en vez de
                        # dejarte peleando con un gesto que no responde.
                        if base_media > BASE_DEMASIADO_CERRADA:
                            print("AVISO: calibraste con los brazos muy doblados y te queda poco")
                            print("       recorrido para el wheelie. Estira mas los brazos y")
                            print("       apreta 'c' para recalibrar.")
                        else:
                            print("Calibrado. Ya podes jugar.")
                else:
                    objetivo = aplicar_zona_muerta((diferencia - base_diferencia) * GANANCIA_DIRECCION)
                    objetivo = max(-1.0, min(1.0, objetivo))
                    direccion += (objetivo - direccion) * SUAVIZADO

                    media_suave += (media - media_suave) * SUAVIZADO

                    # Cuanto doblaste los brazos respecto de tu postura de reposo,
                    # medido de las dos formas. Nos quedamos con la que mas te
                    # favorece, asi el gesto responde igual te sientes con los
                    # brazos estirados o ya bastante cerrados.
                    delta = media_suave - base_media
                    margen = max(MARGEN_MINIMO, 1.0 - base_media)
                    progreso = max(delta / WHEELIE_DELTA,
                                   (delta / margen) / WHEELIE_FRACCION)

                    # progreso 1.0 = justo el umbral. Histeresis: enciende en 1.0 y
                    # apaga mas abajo para que no titile en el borde.
                    if wheelie:
                        wheelie = progreso > WHEELIE_SALIDA
                    else:
                        wheelie = progreso > 1.0

                detectado = True

            elif (ahora - ultimo_visto) < SEGUNDOS_DE_GRACIA and not calibrando:
                # Parpadeo corto de la deteccion (pasa justo al tirar los brazos,
                # que se tapan contra el cuerpo): mantenemos la ultima lectura en
                # vez de cortar y tirarte al teclado en medio del wheelie.
                detectado = True

            else:
                # Te perdimos de verdad: soltamos todo y el juego vuelve al teclado.
                direccion += (0.0 - direccion) * SUAVIZADO
                wheelie = False
                progreso = 0.0
                detectado = False

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

                dibujar(vista, puntos_vista, direccion, wheelie, calibrando, detectado, progreso)
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
                    progreso = 0.0
                    media_suave = 0.0
                    print("Recalibrando... estira un poco los brazos, no los cierres.")

    camara.release()
    cv2.destroyAllWindows()
    sock.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
