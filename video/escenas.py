"""
Escenas animadas del video de Crazy Moto (ver guion.md).

Uso:
    py video/escenas.py camara                 renderiza render/4-camara.mp4
    py video/escenas.py camara --cuadros 3 12  solo guarda esos cuadros en PNG
"""

import argparse
import math

import numpy as np

from estilo import (ALERTA, AMBAR, CREMA, CYAN, MAGENTA, MEDIA, OK, SS, TINTA, TINTA_APAGADA,
                    TINTA_CLARA, TINTA_SUAVE, Lienzo, aparece, codigo_qr, entre, fondo_sol,
                    integrar, mezcla, rebote, renderizar, salida, suave)

# ---------------------------------------------------------------------------
# La silueta de MediaPipe: los 33 puntos de BlazePose, vista como en un espejo
# (como la ventana del detector). Los indices son los de MediaPipe.
# ---------------------------------------------------------------------------

CONEXIONES = [
    (0, 1), (1, 2), (2, 3), (3, 7), (0, 4), (4, 5), (5, 6), (6, 8), (9, 10),
    (11, 12), (11, 13), (13, 15), (15, 17), (15, 19), (15, 21), (17, 19),
    (12, 14), (14, 16), (16, 18), (16, 20), (16, 22), (18, 20),
    (11, 23), (12, 24), (23, 24), (23, 25), (24, 26), (25, 27), (26, 28),
    (27, 29), (28, 30), (29, 31), (30, 32), (27, 31), (28, 32),
]

HOMBROS = (11, 12)
MUNECAS = (15, 16)


def codo(hombro, muneca, afuera, l1=112, l2=106):
    """Posicion del codo para que el brazo llegue a la muneca (dos segmentos)."""
    dx, dy = muneca[0] - hombro[0], muneca[1] - hombro[1]
    d = max(1.0, min(math.hypot(dx, dy), l1 + l2 - 1))
    a = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
    h = math.sqrt(max(0.0, l1 * l1 - a * a))
    px, py = hombro[0] + a * dx / d, hombro[1] + a * dy / d
    c1 = (px - h * dy / d, py + h * dx / d)
    c2 = (px + h * dy / d, py - h * dx / d)
    # De las dos soluciones, la mas cerca de donde cuelga un codo de verdad:
    # abajo y un poco hacia afuera del hombro.
    natural = (hombro[0] + afuera * 80, hombro[1] + 100)
    return c1 if math.dist(c1, natural) < math.dist(c2, natural) else c2


def pose(cx, giro, altura):
    """
    Los 33 puntos de una persona sentada agarrando un manubrio imaginario.
    giro   = grados que gira el manubrio (positivo = a la derecha: baja la mano
             derecha, que en el espejo esta a la derecha de la pantalla).
    altura = donde esta el manubrio respecto de los hombros (positivo = abajo).
    """
    hy = 380
    p = {
        0: (cx, 232),
        1: (cx - 20, 214), 2: (cx - 30, 212), 3: (cx - 41, 215),
        4: (cx + 20, 214), 5: (cx + 30, 212), 6: (cx + 41, 215),
        7: (cx - 60, 228), 8: (cx + 60, 228),
        9: (cx - 17, 262), 10: (cx + 17, 262),
        11: (cx - 118, hy), 12: (cx + 118, hy),
        23: (cx - 82, 640), 24: (cx + 82, 640),
        25: (cx - 96, 790), 26: (cx + 96, 790),
        27: (cx - 92, 935), 28: (cx + 92, 935),
        29: (cx - 104, 962), 30: (cx + 104, 962),
        31: (cx - 70, 972), 32: (cx + 70, 972),
    }
    mitad = 140
    centro = (cx, hy + altura)
    c, s = math.cos(math.radians(giro)), math.sin(math.radians(giro))
    p[16] = (centro[0] + c * mitad, centro[1] + s * mitad)
    p[15] = (centro[0] - c * mitad, centro[1] - s * mitad)
    p[13] = codo(p[11], p[15], -1)
    p[14] = codo(p[12], p[16], +1)
    for m, (pulgar, indice, menique), lado in ((15, (21, 19, 17), -1), (16, (22, 20, 18), +1)):
        x, y = p[m]
        ux, uy = c * lado, s * lado          # hacia afuera del manubrio
        p[pulgar] = (x + ux * 8, y + uy * 8 - 22)
        p[indice] = (x + ux * 30 - uy * 4, y + uy * 30 - 8)
        p[menique] = (x + ux * 24 + uy * 6, y + uy * 24 + 12)
    return p


def zona_muerta(v, zm=0.12):
    if abs(v) < zm:
        return 0.0
    return math.copysign((abs(v) - zm) / (1 - zm), v)


def dibujar_silueta(lz, p, t, alfa, resaltar):
    """resaltar 0..1: cuanto se apagan los demas puntos y se encienden los 4."""
    for a, b in CONEXIONES:
        va = aparece(t, 0.8 + a * 0.08, 0.35) * aparece(t, 0.8 + b * 0.08, 0.35)
        ali = alfa * va * mezcla(0.55, 0.18, resaltar)
        if a in HOMBROS + MUNECAS and b in HOMBROS + MUNECAS:
            ali = alfa * va * 0.55
        lz.linea(p[a], p[b], CYAN, 3, ali)
    lz.circulo(((p[1][0] + p[4][0]) / 2, 240), 72, TINTA_APAGADA, alfa * 0.35 * aparece(t, 0.8, 0.6),
               relleno=False, ancho=2)
    for i, q in p.items():
        v = rebote(entre(t, 0.8 + i * 0.08, 1.15 + i * 0.08))
        if v <= 0:
            continue
        if i in HOMBROS + MUNECAS:
            r = mezcla(7, 12, resaltar) * v
            color = MAGENTA if resaltar > 0.5 else TINTA_CLARA
            lz.circulo(q, r, color, alfa, brillo=resaltar > 0.1)
        else:
            lz.circulo(q, 6 * v, TINTA_CLARA, alfa * mezcla(1, 0.3, resaltar))


def panel_moto(lz, t, alfa, direccion, wheelie):
    """Panel de la derecha: la autopista vista desde arriba y la moto."""
    x0, x1, y0, y1 = 1230, 1730, 230, 930
    lz.rect((x0, y0), (x1, y1), (9, 11, 36), alfa * 0.97, radio=6)
    lz.rect((x0, y0), (x1, y1), CYAN, alfa * 0.45, relleno=False, ancho=2, radio=6, brillo=True)
    lz.texto(((x0 + x1) / 2, y0 + 38), "LA MOTO", 24, CYAN, alfa, espaciado=6)
    cx = (x0 + x1) / 2
    for lx in (cx - 60, cx + 60):           # lineas de carril que se mueven
        desp = (t * 320) % 50
        y = y0 + 80 - 50 + desp
        while y < y1 - 20:
            a, b = max(y, y0 + 80), min(y + 26, y1 - 20)
            if b > a:
                lz.linea((lx, a), (lx, b), TINTA, 3, alfa * 0.35)
            y += 50
    for carril, fase in ((-1, 0.0), (1, 0.45), (0, 0.8)):   # autos de frente
        y = y0 + 80 + ((t * 0.42 + fase) % 1.0) * (y1 - y0 - 120)
        if y < y1 - 60:
            lz.rect((cx + carril * 120 - 26, y), (cx + carril * 120 + 26, y + 56),
                    CYAN, alfa * 0.5, relleno=False, ancho=2, radio=6)
    mx, my = cx + direccion * 120, y1 - 110
    color = AMBAR if wheelie else MAGENTA
    lz.rect((mx - 16, my - 34), (mx + 16, my + 34), color, alfa, radio=10, brillo=True)
    lz.poligono([(mx, my - 52), (mx - 14, my - 34), (mx + 14, my - 34)], color, alfa)
    if wheelie:
        lz.texto((cx, y0 + 110), "WHEELIE", 58, AMBAR, alfa, espaciado=8, brillo=True)


def caja(lz, centro, ancho, alto, titulo, detalle, alfa, color=CYAN):
    x, y = centro
    lz.rect((x - ancho / 2, y - alto / 2), (x + ancho / 2, y + alto / 2), (9, 11, 36), alfa * 0.97, radio=8)
    lz.rect((x - ancho / 2, y - alto / 2), (x + ancho / 2, y + alto / 2), color, alfa * 0.8,
            relleno=False, ancho=3, radio=8, brillo=True)
    lz.texto((x, y - 16), titulo, 32, TINTA, alfa, espaciado=3)
    lz.texto((x, y + 28), detalle, 22, TINTA_SUAVE, alfa, nombre=MEDIA)


def flecha_datos(lz, a, b, t, alfa, arriba, abajo=""):
    lz.linea(a, b, TINTA_APAGADA, 3, alfa * 0.7)
    lz.poligono([(b[0] + 4, b[1]), (b[0] - 14, b[1] - 10), (b[0] - 14, b[1] + 10)], TINTA_APAGADA, alfa * 0.7)
    for k in range(4):                       # paquetes viajando
        u = (t * 0.9 + k / 4) % 1.0
        lz.circulo((mezcla(a[0], b[0], u), a[1]), 7, MAGENTA, alfa * math.sin(math.pi * u), brillo=True)
    medio = (a[0] + b[0]) / 2
    lz.texto((medio, a[1] - 34), arriba, 22, TINTA_CLARA, alfa, nombre=MEDIA)
    if abajo:
        lz.texto((medio, a[1] + 36), abajo, 19, TINTA_SUAVE, alfa, nombre=MEDIA)


# ---------------------------------------------------------------------------
# Tramo 4: el control por camara (27 s)
# ---------------------------------------------------------------------------

LEYENDAS = [
    (1.0, 5.0, "MediaPipe detecta 33 puntos de tu cuerpo"),
    (5.0, 8.6, "Usamos solo 4: los hombros y las muñecas"),
    (8.6, 16.2, "Girás el manubrio imaginario → la moto dobla"),
    (16.2, 21.2, "Las dos manos arriba de los hombros → WHEELIE"),
    (21.6, 27.0, "Y todo le llega al juego en tiempo real"),
]


def escena_camara(t):
    lz = Lienzo()
    # La silueta arranca centrada y se corre a la izquierda cuando entra el panel.
    cx = mezcla(960, 640, suave(entre(t, 8.6, 9.4)))

    # Titulo
    a = aparece(t, 0.0, 0.6)
    lz.texto((960, 78 - 20 * (1 - a)), "CONTROL POR CÁMARA", 54, TINTA, a, espaciado=8, brillo=True)
    for desde, hasta, texto in LEYENDAS:
        al = aparece(t, desde, 0.4) * (1 - entre(t, hasta - 0.35, hasta))
        lz.texto((960, 142), texto, 30, TINTA_CLARA, al, nombre=MEDIA)

    # Movimiento de las manos
    rampa = suave(entre(t, 9.4, 10.0)) * (1 - suave(entre(t, 15.4, 16.0)))
    giro = 28 * math.sin(2 * math.pi * (t - 9.4) / 3.2) * rampa
    subida = suave(entre(t, 16.6, 17.6)) * (1 - suave(entre(t, 19.9, 20.8)))
    altura = mezcla(195, -110, subida)
    p = pose(cx, giro, altura)
    lateral = zona_muerta(giro / 30)
    wheelie = max(p[15][1], p[16][1]) < p[11][1]

    sale = 1 - suave(entre(t, 21.2, 22.0))        # la silueta se va antes del diagrama
    resaltar = suave(entre(t, 5.2, 6.2))
    if sale > 0:
        dibujar_silueta(lz, p, t, sale, resaltar)

        al = sale * aparece(t, 6.0, 0.5) * (1 - entre(t, 8.4, 8.8))
        lz.texto((p[11][0] - 30, p[11][1] - 32), "HOMBROS", 22, MAGENTA, al, anchor="rm", espaciado=3)
        lz.texto((p[15][0] - 34, p[15][1] + 4), "MUÑECAS", 22, MAGENTA, al, anchor="rm", espaciado=3)

        # El manubrio: la linea entre las dos munecas
        man = suave(entre(t, 8.0, 8.8)) * sale
        if man > 0:
            a_, b_ = p[15], p[16]
            fin = (mezcla(a_[0], b_[0], man), mezcla(a_[1], b_[1], man))
            lz.linea(a_, fin, AMBAR, 6, 1.0, brillo=True)
        ang = aparece(t, 9.4, 0.4) * (1 - entre(t, 15.6, 16.0)) * sale
        if ang > 0:
            centro = ((p[15][0] + p[16][0]) / 2, (p[15][1] + p[16][1]) / 2)
            lz.linea_punteada((centro[0] - 190, centro[1]), (centro[0] + 190, centro[1]), TINTA_SUAVE, 2, ang * 0.7)
            if abs(giro) > 1:
                lz.arco(centro, 95, 0, giro, AMBAR, 4, ang)
            lz.texto((centro[0] + 205, centro[1] + 34), f"giro {abs(giro):.0f}°", 26, AMBAR, ang, anchor="lm")

        # Linea de los hombros (el umbral del wheelie)
        lh = aparece(t, 16.2, 0.5) * (1 - entre(t, 20.8, 21.2)) * sale
        if lh > 0:
            y = p[11][1]
            lz.linea_punteada((cx - 300, y), (cx + 300, y), OK if wheelie else TINTA, 3, lh, brillo=wheelie)
            lz.texto((cx + 310, y), "línea de los hombros", 22, TINTA_SUAVE, lh, nombre=MEDIA, anchor="lm")

        pm = aparece(t, 9.0, 0.6) * sale
        if pm > 0:
            panel_moto(lz, t, pm, lateral, wheelie)

    # El recorrido de los datos
    d = aparece(t, 22.0, 0.6)
    if d > 0:
        y = 560
        cajas = [(300, 300, "WEBCAM", "la de la notebook"),
                 (960, 500, "PYTHON + MEDIAPIPE", "detecta los gestos"),
                 (1620, 300, "UNITY", "mueve la moto")]
        for k, (x, ancho, titulo, detalle) in enumerate(cajas):
            ak = aparece(t, 22.0 + k * 0.5, 0.5)
            caja(lz, (x, y + 30 * (1 - ak)), ancho, 170, titulo, detalle, ak,
                 MAGENTA if k == 1 else CYAN)
        flecha_datos(lz, (475, y), (690, y), t, aparece(t, 22.6, 0.5), "imagen")
        flecha_datos(lz, (1230, y), (1445, y), t, aparece(t, 23.1, 0.5), "UDP", "30 veces por segundo")

    return lz.cuadro()   # los fundidos entre escenas los pone montar.py


# ---------------------------------------------------------------------------
# Tramo 1: gancho (6 s)
# ---------------------------------------------------------------------------

def escena_gancho(t):
    lz = Lienzo(fondo_sol(salida(entre(t, 0.0, 1.8))))
    lz.titulo("CRAZY MOTO", (960, 210), 170, t, 1.2, retraso=0.07)
    a = aparece(t, 2.4, 0.6)
    lz.linea((960 - 330 - 150 * a, 330), (960 - 330, 330), CYAN, 2, a)
    lz.linea((960 + 330, 330), (960 + 330 + 150 * a, 330), CYAN, 2, a)
    lz.texto((960, 330), "AUTOPISTA EN CONTRAMANO", 30, CYAN, a, espaciado=7, brillo=True)
    # Destello cuando termina de armarse el titulo
    d = math.exp(-((t - 2.1) / 0.18) ** 2)
    if d > 0.01:
        lz.linea((960 - 700 * d, 262), (960 + 700 * d, 262), CREMA, 3, d, brillo=True)
    return lz.cuadro()


# ---------------------------------------------------------------------------
# Tramo 2: la autopista en contramano, vista desde arriba (8 s)
# ---------------------------------------------------------------------------

FILAS = [(0,), (1, 2), (0, 1), (2,), (0, 2), (1,), (1, 2), (0,), (0, 1), (2,), (1, 2), (0, 2),
         (1,), (0, 1), (2,), (0, 2), (1, 2), (0,), (0, 1), (2,), (1,), (0, 2), (1, 2), (0, 1)]
DURACION_AUTOPISTA = 10.5
CARRIL_X = (760, 960, 1160)
Y_MOTO = 900


def recorrido(t):
    """Cuanto avanzo el mundo (en px de pantalla): la velocidad crece con el tiempo."""
    return 320 * t + 45 * t * t


def libre(fila, desde):
    """El carril libre de la fila mas cerca de donde esta la moto."""
    libres = [c for c in range(3) if c not in fila]
    return min(libres, key=lambda c: abs(c - desde))


def escena_autopista(t):
    lz = Lienzo()
    a = aparece(t, 0.0, 0.5)
    lz.texto((960, 78), "AUTOPISTA EN CONTRAMANO", 50, TINTA, a, espaciado=7, brillo=True)
    leyendas = [(0.6, 4.2, "Los autos vienen de frente por tres carriles"),
                (4.2, DURACION_AUTOPISTA, "Siempre queda uno libre… pero el juego se acelera solo")]
    for desde, hasta, texto in leyendas:
        lz.texto((960, 140), texto, 30, TINTA_CLARA, aparece(t, desde, 0.4) * (1 - entre(t, hasta - 0.3, hasta)),
                 nombre=MEDIA)

    y0, y1 = 190, 1040
    lz.rect((650, y0), (1270, y1), (12, 12, 30), 0.96)
    lz.linea((650, y0), (650, y1), TINTA_SUAVE, 4, 0.6)
    lz.linea((1270, y0), (1270, y1), TINTA_SUAVE, 4, 0.6)
    s = recorrido(t)
    for lx in (860, 1060):
        y = y0 - 60 + (s % 80)
        while y < y1:
            a_, b_ = max(y, y0), min(y + 40, y1)
            if b_ > a_:
                lz.linea((lx, a_), (lx, b_), TINTA, 4, 0.55)
            y += 80

    # Filas de autos: la fila k aparece arriba cuando el recorrido llega a su marca.
    # El carril libre de cada fila es el mas cercano al que eligio la moto antes.
    objetivos, previo = [], 1
    for fila in FILAS:
        previo = libre(fila, previo)
        objetivos.append(previo)

    carril, resaltado = 1.0, None
    for k, fila in enumerate(FILAS):
        y = y0 - 80 + s - k * 430
        if y > Y_MOTO + 120:          # ya paso: la moto quedo en su carril libre
            carril = objetivos[k]
            continue
        # La moto se acomoda mientras la fila se acerca (entre y=380 y y=760)
        u = suave(entre(y, 380, 760))
        carril = mezcla(objetivos[k - 1] if k else 1, objetivos[k], u)
        if y > 380:
            resaltado = (objetivos[k], y, 0.10 * (1 - u))
        break

    if resaltado:
        c, y, al = resaltado
        lz.rect((CARRIL_X[c] - 90, y - 90), (CARRIL_X[c] + 90, y + 90), OK, al)
    for k, fila in enumerate(FILAS):
        y = y0 - 80 + s - k * 430
        al = entre(y, y0 + 80, y0 + 170) * (1 - entre(y, y1 - 170, y1 - 85))   # entra y sale con fundido
        if al <= 0:
            continue
        for c in fila:
            x = CARRIL_X[c]
            lz.rect((x - 48, y - 80), (x + 48, y + 80), CYAN, 0.9 * al, relleno=False, ancho=3, radio=14,
                    brillo=True)
            lz.circulo((x - 28, y + 66), 7, AMBAR, al, brillo=True)
            lz.circulo((x + 28, y + 66), 7, AMBAR, al, brillo=True)

    mx = mezcla(CARRIL_X[0], CARRIL_X[2], carril / 2)
    lz.rect((mx - 20, Y_MOTO - 46), (mx + 20, Y_MOTO + 46), MAGENTA, 1, radio=12, brillo=True)
    lz.poligono([(mx, Y_MOTO - 70), (mx - 18, Y_MOTO - 46), (mx + 18, Y_MOTO - 46)], MAGENTA)

    # Velocimetro: la velocidad real del juego va de 10 a 60 m/s
    v = mezcla(10, 60, entre(t, 0.5, DURACION_AUTOPISTA - 0.5)) * 3.6
    av = aparece(t, 0.8, 0.5)
    lz.texto((1560, 470), "VELOCIDAD", 24, TINTA_SUAVE, av, espaciado=6)
    lz.texto((1560, 545), f"{v:.0f}", 96, AMBAR if v > 150 else TINTA, av, brillo=True)
    lz.texto((1560, 620), "km/h", 26, TINTA_SUAVE, av, nombre=MEDIA)
    al = aparece(t, 1.2, 0.5)
    lz.texto((360, 520), "3 carriles", 40, TINTA, al, espaciado=2)
    lz.texto((360, 575), "1 o 2 autos por fila", 26, TINTA_SUAVE, al, nombre=MEDIA)
    return lz.cuadro()


# ---------------------------------------------------------------------------
# Tramo 3: el wheelie, riesgo y recompensa (16 s)
# ---------------------------------------------------------------------------

def rotar(p, centro, grados):
    a = math.radians(grados)
    dx, dy = p[0] - centro[0], p[1] - centro[1]
    return (centro[0] + dx * math.cos(a) - dy * math.sin(a), centro[1] + dx * math.sin(a) + dy * math.cos(a))


def moto_costado(lz, t, angulo, alfa=1.0):
    """La moto de costado (mirando a la derecha). `angulo` = cuanto levanta la rueda."""
    piso = 770
    trasera = (430, piso - 72)
    r = lambda p: rotar(p, trasera, -angulo)   # gira sobre el eje de la rueda trasera
    delantera = r((700, piso - 72))
    giro = t * 540
    for c in (r(trasera), delantera):
        lz.circulo(c, 72, CYAN, alfa, relleno=False, ancho=6, brillo=True)
        lz.circulo(c, 14, CYAN, alfa)
        for k in range(3):
            ang = math.radians(giro + k * 60)
            lz.linea((c[0] - 60 * math.cos(ang), c[1] - 60 * math.sin(ang)),
                     (c[0] + 60 * math.cos(ang), c[1] + 60 * math.sin(ang)), CYAN, 2, alfa * 0.5)
    # Motor y tanque: el bloque que hace que se lea como moto y no como bici
    motor = [r(p) for p in ((515, 618), (640, 604), (655, 650), (600, 700), (530, 690))]
    lz.poligono(motor, MAGENTA, alfa * 0.35)
    for a_, b_ in zip(motor, motor[1:] + motor[:1]):
        lz.linea(a_, b_, MAGENTA, 4, alfa)
    lz.linea(r((520, 690)), r((400, 668)), TINTA_SUAVE, 6, alfa)      # escape
    cuerpo = [r(p) for p in ((430, 698), (520, 610), (640, 600), (690, 560), (700, 698))]
    for a_, b_ in zip(cuerpo, cuerpo[1:]):
        lz.linea(a_, b_, MAGENTA, 7, alfa, brillo=True)
    lz.linea(r((500, 600)), r((600, 592)), MAGENTA, 12, alfa)        # asiento
    lz.linea(r((690, 560)), r((672, 520)), TINTA, 5, alfa)            # manubrio
    # Piloto
    cadera, hombro = r((545, 585)), r((585, 475))
    lz.linea(cadera, hombro, TINTA, 7, alfa)
    lz.linea(hombro, r((670, 525)), TINTA, 6, alfa)
    lz.linea(cadera, r((610, 650)), TINTA, 7, alfa)
    lz.circulo(r((600, 430)), 30, TINTA, alfa, relleno=False, ancho=5)
    # Piso que se mueve
    lz.linea((150, piso), (1000, piso), TINTA_SUAVE, 3, alfa * 0.6)
    desp = (t * 900) % 120
    x = 1000 - desp
    while x > 150:
        lz.linea((x, piso + 26), (max(150, x - 50), piso + 26), TINTA_APAGADA, 3, alfa * 0.6)
        x -= 120


def barra(lz, y, rotulo, valor, maximo, texto_valor, color, alfa, etiqueta=None, color_etiqueta=None):
    x0, x1 = 1100, 1780
    if etiqueta:
        lz.texto((x1, y - 44), etiqueta, 22, color_etiqueta, alfa, anchor="rm", espaciado=5, brillo=True)
    lz.texto((x0, y - 44), rotulo, 24, TINTA_SUAVE, alfa, anchor="lm", espaciado=4)
    lz.rect((x0, y - 16), (x1, y + 16), (20, 22, 50), alfa, radio=6)
    w = (x1 - x0) * max(0.0, min(1.0, valor / maximo))
    if w > 2:
        lz.rect((x0, y - 16), (x0 + w, y + 16), color, alfa, radio=6, brillo=True)
    lz.texto((x0, y + 52), texto_valor, 30, TINTA, alfa, anchor="lm")


def pps(t):
    """Puntos por segundo del juego: de 100 a 300 a medida que sube la velocidad."""
    return mezcla(100, 300, suave(entre(t, 5.5, 10.0)))


def escena_wheelie(t):
    lz = Lienzo()
    a = aparece(t, 0.0, 0.5)
    lz.texto((960, 78), "EL WHEELIE", 54, TINTA, a, espaciado=8, brillo=True)
    leyendas = [(0.6, 5.4, "La única forma de sumar puntos"),
                (5.4, 10.4, "Cuanto más rápido vas, más puntos te da"),
                (10.4, 16.0, "Pero en una rueda casi no podés esquivar")]
    for desde, hasta, texto in leyendas:
        lz.texto((960, 142), texto, 30, TINTA_CLARA, aparece(t, desde, 0.4) * (1 - entre(t, hasta - 0.3, hasta)),
                 nombre=MEDIA)

    angulo = 24 * suave(entre(t, 2.6, 3.6))
    moto_costado(lz, t, angulo)

    en_wheelie = t >= 3.4
    puntos = integrar(pps, 3.4, t) if en_wheelie else 0
    ap = aparece(t, 1.0, 0.5)
    lz.texto((1440, 290), "PUNTAJE", 24, TINTA_SUAVE, ap, espaciado=6)
    lz.texto((1440, 360), f"{int(puntos):,}".replace(",", "."), 84, MAGENTA if en_wheelie else TINTA, ap,
             brillo=en_wheelie)

    velocidad = mezcla(10, 60, suave(entre(t, 5.5, 10.0))) * 3.6
    ab = aparece(t, 4.0, 0.5)
    barra(lz, 520, "VELOCIDAD", velocidad, 216, f"{velocidad:.0f} km/h", CYAN, ab)
    barra(lz, 680, "PUNTOS POR SEGUNDO", pps(t), 300, f"{pps(t):.0f} pts/s", OK, aparece(t, 4.6, 0.5),
          "RECOMPENSA", OK)
    lateral = mezcla(100, 30, suave(entre(t, 11.0, 12.4)))
    barra(lz, 840, "MOVIMIENTO DE COSTADO", lateral, 100, f"{lateral:.0f} %", ALERTA if lateral < 60 else CYAN,
          aparece(t, 10.6, 0.5), "RIESGO", ALERTA)
    return lz.cuadro()


# ---------------------------------------------------------------------------
# Tramo 5: el problema que resolvimos (15 s)
# ---------------------------------------------------------------------------

_RUIDO = np.random.default_rng(7).normal(0, 1, 400)


def senal(u, ruidosa):
    """El mismo gesto (doblar a un lado y al otro) medido de dos formas."""
    base = math.sin(u * 2 * math.pi * 1.5)
    if not ruidosa:
        return base
    i = int(u * 399)
    salto = 1.6 if (i // 23) % 3 == 1 else 0.0      # tramos donde el angulo "salta"
    return base * 0.5 + _RUIDO[i] * 0.45 + salto * math.copysign(1, _RUIDO[(i * 7) % 400])


def camarita(lz, c, alfa, mira=1):
    x, y = c
    lz.rect((x - 46, y - 30), (x + 34, y + 30), TINTA_SUAVE, alfa, relleno=False, ancho=3, radio=6)
    lz.poligono([(x + 34, y - 14), (x + 60, y - 28), (x + 60, y + 28), (x + 34, y + 14)], TINTA_SUAVE, alfa)
    lz.circulo((x - 6, y), 14, CYAN, alfa, relleno=False, ancho=3)


def grafico(lz, caja_, titulo, color, t, desde, ruidosa, alfa):
    x0, y0, x1, y1 = caja_
    lz.rect((x0, y0), (x1, y1), (9, 11, 36), alfa * 0.95, radio=8)
    lz.rect((x0, y0), (x1, y1), color, alfa * 0.6, relleno=False, ancho=2, radio=8)
    lz.texto((x0 + 24, y0 + 30), titulo, 24, color, alfa, anchor="lm", espaciado=3)
    medio = (y0 + y1) / 2 + 22
    amp = (y1 - y0 - 96) / 2 / 1.15
    lz.linea((x0 + 24, medio), (x1 - 24, medio), TINTA_APAGADA, 1, alfa * 0.5)
    hasta = entre(t, desde, desde + 3.0)
    n = int(160 * hasta)
    puntos = [(mezcla(x0 + 24, x1 - 24, k / 159), medio - max(-1.15, min(1.15, senal(k / 159, ruidosa))) * amp)
              for k in range(n)]
    for a_, b_ in zip(puntos, puntos[1:]):
        lz.linea(a_, b_, color, 3, alfa, brillo=True)


def escena_problema(t):
    lz = Lienzo()
    a = aparece(t, 0.0, 0.5)
    lz.texto((960, 78), "EL PROBLEMA QUE RESOLVIMOS", 50, TINTA, a, espaciado=7, brillo=True)
    leyendas = [(0.6, 4.0, "La primera versión medía cuánto doblabas el codo"),
                (4.0, 7.8, "Pero de frente, la cámara casi no ve el antebrazo"),
                (7.8, 11.6, "La señal saltaba… así que cambiamos el gesto"),
                (11.6, 15.0, "El manubrio se ve siempre bien, aunque te muevas")]
    for desde, hasta, texto in leyendas:
        lz.texto((960, 142), texto, 30, TINTA_CLARA, aparece(t, desde, 0.4) * (1 - entre(t, hasta - 0.3, hasta)),
                 nombre=MEDIA)

    vistas = 1 - suave(entre(t, 7.6, 8.2))
    if vistas > 0:
        # De costado: el brazo doblado apuntando a la camara
        ac = aparece(t, 0.8, 0.5) * vistas
        lz.rect((200, 240), (920, 900), CYAN, ac * 0.4, relleno=False, ancho=2, radio=8)
        lz.texto((560, 285), "VISTO DE COSTADO", 24, CYAN, ac, espaciado=5)
        camarita(lz, (300, 620), ac)
        hombro, codo_, muneca = (780, 450), (790, 640), (560, 640)
        lz.circulo((790, 385), 34, TINTA_APAGADA, ac * 0.7, relleno=False, ancho=3)      # la persona, tenue
        lz.linea((790, 425), (800, 760), TINTA_APAGADA, 6, ac * 0.5)
        lz.linea(hombro, codo_, TINTA, 8, ac)
        lz.linea(codo_, muneca, MAGENTA, 8, ac, brillo=True)
        for p in (hombro, codo_, muneca):
            lz.circulo(p, 11, TINTA_CLARA, ac)
        lz.arco(codo_, 56, 180, 275, AMBAR, 4, ac)
        lz.texto((560, 820), "el codo se ve doblado: 90°", 28, AMBAR, ac, nombre=MEDIA)
        for k in range(3):
            lz.linea_punteada((362, 600 + k * 20), (548, 610 + k * 15), TINTA_APAGADA, 2, ac * 0.6, 8, 8)

        # Lo que ve la camara: el antebrazo casi desaparece
        af = aparece(t, 3.8, 0.5) * vistas
        lz.rect((1000, 240), (1720, 900), MAGENTA, af * 0.4, relleno=False, ancho=2, radio=8)
        lz.texto((1360, 285), "LO QUE VE LA CÁMARA", 24, MAGENTA, af, espaciado=5)
        temblor = 6 * _RUIDO[int(t * 12) % 400]
        h2, c2, m2 = (1360, 450), (1362, 640), (1372 + temblor, 652 - temblor)
        lz.circulo((1460, 385), 34, TINTA_APAGADA, af * 0.7, relleno=False, ancho=3)     # cabeza y hombros
        lz.linea(h2, (1560, 450), TINTA_APAGADA, 6, af * 0.5)
        lz.linea((1400, 452), (1410, 760), TINTA_APAGADA, 5, af * 0.35)
        lz.linea((1520, 452), (1510, 760), TINTA_APAGADA, 5, af * 0.35)
        lz.linea(h2, c2, TINTA, 8, af)
        lz.linea(c2, m2, MAGENTA, 8, af, brillo=True)
        for p in (h2, c2, m2):
            lz.circulo(p, 11, TINTA_CLARA, af)
        angulo = 110 + 45 * _RUIDO[int(t * 7) % 400]
        lz.texto((1360, 760), f"ángulo del codo: {angulo:.0f}°", 30, ALERTA, af)
        lz.texto((1360, 820), "el antebrazo casi no se ve", 28, TINTA_SUAVE, af, nombre=MEDIA)

    ag = aparece(t, 8.0, 0.6)
    if ag > 0:
        grafico(lz, (200, 260, 1720, 540), "ANTES · SEÑAL DEL CODO", ALERTA, t, 8.2, True, ag)
        grafico(lz, (200, 600, 1720, 880), "AHORA · SEÑAL DEL MANUBRIO", OK, t, 10.8, False, aparece(t, 10.6, 0.6))
        ok = aparece(t, 13.6, 0.5)
        if ok > 0:
            c = (1620, 660)
            lz.linea((c[0] - 22, c[1]), (c[0] - 6, c[1] + 18), OK, 7, ok, brillo=True)
            lz.linea((c[0] - 6, c[1] + 18), (c[0] + 26, c[1] - 20), OK, 7, ok, brillo=True)
    return lz.cuadro()


# ---------------------------------------------------------------------------
# Tramo 7: cierre (10 s)
# ---------------------------------------------------------------------------

URL_ITCH = "https://s1rwa.itch.io/crazy-moto"


def escena_cierre(t):
    lz = Lienzo(fondo_sol(1.0 - 0.35 * suave(entre(t, 0.0, 10.0))))
    lz.titulo("CRAZY MOTO", (960, 150), 120, t, 0.2, retraso=0.05)
    an = aparece(t, 1.2, 0.6)
    lz.texto((960, 262), "Joaquín Pastorino  ·  Santiago Castiñeira", 34, TINTA, an, nombre=MEDIA)
    lz.texto((960, 312), "PROYECTO FERIA DE CIENCIAS", 24, CYAN, an, espaciado=7)

    aq = aparece(t, 2.2, 0.6)
    if aq > 0:
        lado = 300
        y = 570 + 30 * (1 - aq)
        lz.rect((960 - lado / 2 - 18, y - lado / 2 - 18), (960 + lado / 2 + 18, y + lado / 2 + 18), MAGENTA, aq,
                relleno=False, ancho=3, radio=10, brillo=True)
        lz.imagen(codigo_qr(URL_ITCH, lado), (960, y), 1 / SS, aq)
        lz.texto((960, 790), "s1rwa.itch.io/crazy-moto", 40, CYAN, aq, brillo=True)
        lz.texto((960, 846), "Bajalo gratis en itch.io", 28, TINTA_CLARA, aq, nombre=MEDIA)
    return lz.cuadro()


ESCENAS = {
    "gancho": (escena_gancho, 6.0, "1-gancho"),
    "autopista": (escena_autopista, DURACION_AUTOPISTA, "2-autopista"),
    "wheelie": (escena_wheelie, 16.0, "3-wheelie"),
    "camara": (escena_camara, 27.0, "4-camara"),
    "problema": (escena_problema, 15.0, "5-problema"),
    "cierre": (escena_cierre, 10.0, "7-cierre"),
}


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("escena", choices=sorted(ESCENAS))
    ap.add_argument("--cuadros", type=float, nargs="*", help="segundos a guardar como PNG")
    args = ap.parse_args()
    funcion, duracion, nombre = ESCENAS[args.escena]
    renderizar(funcion, duracion, nombre, args.cuadros)
