"""
Carteles para el stand de la feria (issue #17), con el estilo "Ruta de noche"
del juego.

    py decoracion/carteles.py            genera los dos carteles en decoracion/render/
    py decoracion/carteles.py poster     solo el poster A3
    py decoracion/carteles.py instrucciones

Salen en PDF listos para imprimir a 300 dpi (y una vista previa en vistas/):
  - poster-a3.pdf          A3 vertical, para pegar en el stand y verse de lejos.
  - instrucciones-a4.pdf   A4 vertical, al lado de la compu: como se juega con la camara.

Usa el mismo motor que el video (video/estilo.py): colores, fuente Chakra Petch y
efecto neon. Lo que en el video eran sprites del menu (sol, grilla, resplandor)
aca se dibuja con codigo a la resolucion final, porque estirados a A3 los PNG de
512 px salen borrosos.
"""

import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

AQUI = Path(__file__).resolve().parent
sys.path.insert(0, str(AQUI.parent / "video"))

from escenas import codo  # noqa: E402
from estilo import (AMBAR, CYAN, FONDO, MAGENTA, MEDIA, SS, TINTA, TINTA_APAGADA,  # noqa: E402
                    TINTA_CLARA, TINTA_SUAVE, Lienzo, codigo_qr, fuente, sprite)

RENDER = AQUI / "render"
IMG = AQUI / "img"
DPI = 300

URL_ITCH = "https://s1rwa.itch.io/crazy-moto"
URL_VIDEO = "https://youtu.be/SZbZtkWUglE"

# Tamaños "de diseño" (150 dpi): el lienzo dibuja al doble, o sea a 300 dpi.
A3 = (1754, 2480)
A4 = (1240, 1754)


# ---------------------------------------------------------------------------
# Fondo: cielo nocturno, sol retro, resplandor y grilla de neon, a 300 dpi
# ---------------------------------------------------------------------------

def fondo_cartel(ancho, alto, horizonte, radio_sol=0, alfa_grilla=0.5):
    """Todo en px de diseño; la imagen sale al doble (resolucion de impresion)."""
    W, H, hz = ancho * SS, alto * SS, int(horizonte * SS)
    base = sprite("FondoNoche").resize((W, H), Image.BICUBIC)

    if radio_sol:
        R = radio_sol * SS
        cx, cy = W // 2, hz + int(R * 0.12)
        # Halo: un brillo radial que tiñe el cielo alrededor del sol.
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / (R * 2.6)
        halo = np.clip(1 - d, 0, 1) ** 2 * 0.55
        halo[hz:, :] *= 0.35
        cielo = np.asarray(base, dtype=np.float32)
        tinte = np.array([255, 70, 150, 0], dtype=np.float32)
        cielo = cielo + (tinte - cielo) * halo[..., None] * np.array([1, 1, 1, 0])
        base = Image.fromarray(np.clip(cielo, 0, 255).astype(np.uint8), "RGBA")

        # El sol: degradado amarillo -> rosa y franjas horizontales abajo, como el del menu.
        sol = np.zeros((2 * R, 2 * R, 4), dtype=np.float32)
        y, x = np.mgrid[0:2 * R, 0:2 * R].astype(np.float32)
        dentro = (x - R) ** 2 + (y - R) ** 2 <= R * R
        u = y / (2 * R)
        arriba, medio, abajo = np.array([255, 236, 150]), np.array([255, 170, 80]), np.array([255, 70, 140])
        color = np.where(u[..., None] < 0.5,
                         arriba + (medio - arriba) * (u[..., None] / 0.5),
                         medio + (abajo - medio) * ((u[..., None] - 0.5) / 0.5))
        franjas = np.ones_like(u, dtype=bool)
        # Franjas cada vez mas gruesas hacia abajo, en la parte que queda sobre el
        # horizonte (el centro del sol esta un poco por debajo de la linea).
        for k in range(6):
            y0 = (0.24 + k * 0.034) * 2 * R
            franjas &= ~((y >= y0) & (y < y0 + (0.004 + k * 0.0032) * 2 * R))
        sol[..., :3] = color
        sol[..., 3] = (dentro & franjas) * 255
        sol_img = Image.fromarray(sol.astype(np.uint8), "RGBA")
        capa = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        capa.alpha_composite(sol_img, (cx - R, cy - R))
        capa.paste((0, 0, 0, 0), (0, hz, W, H))  # se esconde detras del horizonte
        base.alpha_composite(capa)

    # Resplandor sobre la linea del horizonte
    banda = int(0.07 * H)
    rampa = (np.linspace(0, 1, banda) ** 2 * 120).astype(np.uint8)
    resp = Image.new("RGBA", (W, banda), MAGENTA + (0,))
    resp.putalpha(Image.fromarray(np.repeat(rampa[:, None], W, axis=1)))
    base.alpha_composite(resp, (0, hz - banda))

    # Piso oscuro + grilla de neon en perspectiva
    piso = Image.new("RGBA", (W, H - hz), (12, 6, 30, 255))
    base.paste(piso, (0, hz))
    g = Image.new("RGBA", (W, H - hz), (0, 0, 0, 0))
    dg = ImageDraw.Draw(g)
    tinta = CYAN + (int(255 * alfa_grilla),)
    grosor = max(2, int(1.6 * SS))
    for k in range(-18, 19):                      # lineas que se van al punto de fuga
        dg.line([(W / 2, 0), (W / 2 + k * W * 0.11, H - hz)], fill=tinta, width=grosor)
    n = 14
    for k in range(1, n + 1):                     # lineas horizontales, mas juntas hacia el fondo
        y = (H - hz) * (k / n) ** 2.2
        dg.line([(0, y), (W, y)], fill=tinta, width=grosor)
    sombra = np.linspace(0.25, 1, H - hz)[:, None]  # la grilla se apaga hacia el horizonte
    a = np.asarray(g.getchannel("A"), dtype=np.float32) * sombra
    g.putalpha(Image.fromarray(a.astype(np.uint8)))
    base.alpha_composite(g, (0, hz))

    linea = Image.new("RGBA", (W, max(3, int(1.5 * SS))), (255, 120, 200, 255))
    base.alpha_composite(linea, (0, hz - linea.height // 2))
    base.alpha_composite(sprite("Vineta").resize((W, H), Image.BICUBIC))
    return base.convert("RGB")


# ---------------------------------------------------------------------------
# Piezas
# ---------------------------------------------------------------------------

def parrafo(lz, x, y, texto, tam, ancho_max, color=TINTA, nombre=MEDIA, interlineado=1.35, anchor="la"):
    """Texto que corta solo en renglones de `ancho_max` px. Devuelve la y de abajo."""
    f = fuente(nombre, tam)
    renglones, actual = [], ""
    for palabra in texto.split():
        prueba = (actual + " " + palabra).strip()
        if f.getlength(prueba) / SS > ancho_max and actual:
            renglones.append(actual)
            actual = palabra
        else:
            actual = prueba
    renglones.append(actual)
    for k, r in enumerate(renglones):
        lz.texto((x, y + k * tam * interlineado), r, tam, color, nombre=nombre, anchor=anchor)
    return y + len(renglones) * tam * interlineado


def tarjeta(lz, a, b, color=CYAN, alfa=0.92):
    lz.rect(a, b, (10, 10, 34), alfa, radio=18)
    lz.rect(a, b, color, 0.85, relleno=False, ancho=3, radio=18, brillo=True)


def bloque_qr(lz, centro, lado, titulo, url_visible, url, color):
    x, y = centro
    lz.texto((x, y - lado / 2 - 60), titulo, 40, color, espaciado=6, brillo=True)
    lz.rect((x - lado / 2 - 24, y - lado / 2 - 24), (x + lado / 2 + 24, y + lado / 2 + 24), (255, 255, 255), 1,
            radio=10)
    lz.imagen(codigo_qr(url, lado), (x, y), 1 / SS)
    lz.texto((x, y + lado / 2 + 58), url_visible, 30, TINTA_CLARA, nombre=MEDIA)


def silueta(lz, cx, cy, s, giro=0.0, arriba=False):
    """Una persona de la cintura para arriba agarrando un manubrio imaginario."""
    hombro_i, hombro_d = (cx - 62 * s, cy), (cx + 62 * s, cy)
    cadera_i, cadera_d = (cx - 44 * s, cy + 175 * s), (cx + 44 * s, cy + 175 * s)
    lz.circulo((cx, cy - 78 * s), 36 * s, TINTA_SUAVE, 0.9, relleno=False, ancho=4 * s)
    lz.linea((cx, cy - 42 * s), (cx, cy), TINTA_SUAVE, 4 * s, 0.8)
    for a, b in ((hombro_i, hombro_d), (hombro_i, cadera_i), (hombro_d, cadera_d), (cadera_i, cadera_d)):
        lz.linea(a, b, CYAN, 5 * s, 0.75)

    centro = (cx, cy + (-80 * s if arriba else 100 * s))
    mitad = (74 if arriba else 92) * s       # manos afuera del torso
    c, sn = math.cos(math.radians(giro)), math.sin(math.radians(giro))
    muneca_d = (centro[0] + c * mitad, centro[1] + sn * mitad)
    muneca_i = (centro[0] - c * mitad, centro[1] - sn * mitad)
    for hombro, muneca, lado in ((hombro_i, muneca_i, -1), (hombro_d, muneca_d, 1)):
        el = codo(hombro, muneca, lado, l1=78 * s, l2=74 * s)
        lz.linea(hombro, el, CYAN, 5 * s, 0.9)
        lz.linea(el, muneca, CYAN, 5 * s, 0.9)
        lz.circulo(el, 6 * s, TINTA_CLARA, 1)
    lz.linea(muneca_i, muneca_d, AMBAR, 8 * s, 1, brillo=True)
    for p in (hombro_i, hombro_d, muneca_i, muneca_d):
        lz.circulo(p, 10 * s, MAGENTA, 1, brillo=True)
    return muneca_i, muneca_d, centro, mitad


def flecha(lz, a, b, color, ancho=6):
    lz.linea(a, b, color, ancho, 1, brillo=True)
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    p = lambda d, da: (b[0] - d * math.cos(ang + da), b[1] - d * math.sin(ang + da))
    lz.poligono([b, p(26, 0.45), p(26, -0.45)], color, 1)


def flecha_curva(lz, centro, radio, desde, hasta, color, ancho=6):
    """Arco con punta de flecha en `hasta` (grados, sentido horario como en Pillow)."""
    lz.arco(centro, radio, desde, hasta, color, ancho, 1, brillo=True)
    ang = math.radians(hasta)
    fin = (centro[0] + radio * math.cos(ang), centro[1] + radio * math.sin(ang))
    tang = ang + (math.pi / 2 if hasta > desde else -math.pi / 2)
    p = lambda d, da: (fin[0] - d * math.cos(tang + da), fin[1] - d * math.sin(tang + da))
    lz.poligono([fin, p(26, 0.45), p(26, -0.45)], color, 1)


# ---------------------------------------------------------------------------
# Poster A3
# ---------------------------------------------------------------------------

def poster():
    ancho, alto = A3
    lz = Lienzo(fondo_cartel(ancho, alto, horizonte=1830, radio_sol=300, alfa_grilla=0.55), ancho, alto)
    cx = ancho / 2

    lz.titulo("CRAZY MOTO", (cx, 245), 236, 99, 0)
    lz.linea((cx - 640, 405), (cx - 470, 405), CYAN, 3, 1)
    lz.linea((cx + 470, 405), (cx + 640, 405), CYAN, 3, 1)
    lz.texto((cx, 405), "AUTOPISTA EN CONTRAMANO", 46, CYAN, espaciado=10, brillo=True)

    foto = Image.open(IMG / "contramano.jpg")
    w = 1520
    h = w * foto.height / foto.width
    y0 = 500
    lz.rect((cx - w / 2 - 12, y0 - 12), (cx + w / 2 + 12, y0 + h + 12), MAGENTA, 1, relleno=False, ancho=6,
            radio=12, brillo=True)
    lz.imagen(foto, (cx, y0 + h / 2), w / foto.width)

    y = y0 + h + 115
    lz.texto((cx, y), "¡MANEJÁ CON TU CUERPO!", 92, TINTA, espaciado=4, brillo=True)
    parrafo(lz, cx, y + 85, "Una webcam ve tus hombros y tus manos: girá un manubrio imaginario para "
            "esquivar los autos y levantá las manos para hacer wheelie.", 40, 1350, TINTA_CLARA, anchor="ma")

    tarjeta(lz, (150, 1905), (ancho - 150, 2300), MAGENTA, 0.9)
    bloque_qr(lz, (cx - 360, 2120), 240, "JUGALO GRATIS", "s1rwa.itch.io/crazy-moto", URL_ITCH, MAGENTA)
    bloque_qr(lz, (cx + 360, 2120), 240, "MIRÁ EL VIDEO", "youtu.be/SZbZtkWUglE", URL_VIDEO, CYAN)

    lz.texto((cx, 2375), "Joaquín Pastorino  ·  Santiago Castiñeira", 44, TINTA, nombre=MEDIA)
    lz.texto((cx, 2432), "FERIA DE CIENCIAS 2026  ·  COLEGIO TÉCNICO DE LA UNIVERSIDAD DE MENDOZA", 26, CYAN,
             espaciado=4)
    return lz.cuadro(alta=True)


# ---------------------------------------------------------------------------
# Cartel de instrucciones A4
# ---------------------------------------------------------------------------

PASOS = [
    ("PARATE FRENTE A LA CÁMARA", "A uno o dos metros, de modo que se vean tus hombros y tus dos manos.",
     dict(giro=0, arriba=False)),
    ("PARA DOBLAR", "Agarrá un manubrio imaginario y giralo, como uno de verdad. Bajá la mano del lado "
     "al que querés ir.", dict(giro=24, arriba=False)),
    ("PARA HACER WHEELIE", "Levantá las dos manos por encima de los hombros. ¡Así sumás puntos! "
     "Pero en una rueda casi no podés esquivar.", dict(giro=0, arriba=True)),
]


def instrucciones():
    ancho, alto = A4
    lz = Lienzo(fondo_cartel(ancho, alto, horizonte=1480, radio_sol=0, alfa_grilla=0.4), ancho, alto)
    cx = ancho / 2

    lz.titulo("CÓMO SE JUEGA", (cx, 140), 124, 99, 0)
    lz.texto((cx, 262), "CON LA CÁMARA WEB", 34, CYAN, espaciado=9, brillo=True)

    y0, alto_t, hueco = 320, 335, 28
    for k, (titulo, texto, pose) in enumerate(PASOS):
        top = y0 + k * (alto_t + hueco)
        color = MAGENTA if k != 1 else CYAN
        tarjeta(lz, (80, top), (ancho - 80, top + alto_t), color)
        lz.circulo((130, top + 50), 32, color, 1, brillo=True)
        lz.texto((130, top + 50), str(k + 1), 40, (20, 6, 42))

        fx, fy = 300, top + 150
        mi, md, centro, mitad = silueta(lz, fx, fy, 0.92, **pose)
        if pose["giro"]:
            flecha_curva(lz, centro, mitad + 28, -50, -10, AMBAR)
            flecha_curva(lz, centro, mitad + 28, 135, 175, AMBAR)
        if pose["arriba"]:
            for m in (mi, md):
                flecha(lz, (m[0], m[1] + 90), (m[0], m[1] + 26), AMBAR)
            lz.linea_punteada((fx - 150, fy), (fx + 150, fy), TINTA_SUAVE, 3, 0.9)

        lz.texto((520, top + 70), titulo, 35, color, anchor="lm", espaciado=2, brillo=True)
        parrafo(lz, 520, top + 118, texto, 32, ancho - 520 - 120, TINTA)

    yo = y0 + 3 * (alto_t + hueco) + 10
    lz.texto((cx, yo + 20), "ESQUIVÁ LOS AUTOS  ·  HACÉ WHEELIE  ·  ¡ENTRÁ AL RANKING!", 29, AMBAR,
             espaciado=2, brillo=True)

    yt = 1515
    for x0, x1, titulo, texto in ((80, 610, "¿NO TE DETECTA?",
                                   "Alejate un poco: tienen que verse tus hombros y tus manos."),
                                  (630, ancho - 80, "¿PREFERÍS EL TECLADO?",
                                   "A / D para moverte y Shift para el wheelie.")):
        tarjeta(lz, (x0, yt), (x1, yt + 170), TINTA_APAGADA, 0.9)
        lz.texto((x0 + 30, yt + 42), titulo, 28, TINTA_CLARA, anchor="lm", espaciado=3)
        parrafo(lz, x0 + 30, yt + 72, texto, 26, x1 - x0 - 60, TINTA_SUAVE)
    lz.texto((cx, alto - 38), "CRAZY MOTO  ·  s1rwa.itch.io/crazy-moto", 22, TINTA_APAGADA, espaciado=4)
    return lz.cuadro(alta=True)


# ---------------------------------------------------------------------------

CARTELES = {"poster": (poster, "poster-a3"), "instrucciones": (instrucciones, "instrucciones-a4")}


def guardar(img, nombre):
    RENDER.mkdir(parents=True, exist_ok=True)
    img.save(RENDER / f"{nombre}.pdf", resolution=DPI)
    # Vista previa liviana para mostrar en el README (GitHub no muestra PDFs ahi).
    (AQUI / "vistas").mkdir(exist_ok=True)
    vista = img.copy()
    vista.thumbnail((800, 1130), Image.LANCZOS)
    vista.save(AQUI / "vistas" / f"{nombre}.jpg", quality=88)
    print(f"{nombre}: {img.width}x{img.height} px -> {RENDER / (nombre + '.pdf')}")


if __name__ == "__main__":
    elegidos = sys.argv[1:] or list(CARTELES)
    for clave in elegidos:
        funcion, nombre = CARTELES[clave]
        guardar(funcion(), nombre)
