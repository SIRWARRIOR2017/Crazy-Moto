"""
Motor de las animaciones del video de Crazy Moto.

Cada escena es una funcion que recibe el tiempo t (en segundos) y dibuja ese
cuadro. Aca esta todo lo que comparten: los colores y las fuentes del juego (los
mismos de Assets/Scripts/EstiloUI.cs), el fondo nocturno hecho con los sprites
del menu, el efecto neon y el escritor que le pasa los cuadros a ffmpeg.

Se dibuja al doble de resolucion y despues se achica (SS = 2): Pillow no suaviza
las lineas, y asi los bordes salen limpios.
"""

import math
import subprocess
from functools import lru_cache
from pathlib import Path

import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ANCHO, ALTO, FPS = 1920, 1080, 30
SS = 2

RAIZ = Path(__file__).resolve().parent.parent
SPRITES = RAIZ / "Assets" / "Resources" / "Sprites"
FUENTES = RAIZ / "Assets" / "Fonts"
RENDER = Path(__file__).resolve().parent / "render"

# Paleta "Ruta de noche" (EstiloUI.cs)
MAGENTA = (255, 61, 154)
CYAN = (55, 224, 255)
AMBAR = (255, 180, 84)
CREMA = (255, 243, 208)
TINTA = (234, 244, 255)
TINTA_CLARA = (189, 239, 255)
TINTA_SUAVE = (143, 147, 181)
TINTA_APAGADA = (110, 115, 153)
FONDO = (8, 6, 26)
OK = (92, 242, 176)
ALERTA = (255, 92, 122)

NEGRITA = "ChakraPetch-Bold.ttf"
NEGRITA_ITALICA = "ChakraPetch-BoldItalic.ttf"
MEDIA = "ChakraPetch-SemiBold.ttf"


# ---------------------------------------------------------------------------
# Tiempo
# ---------------------------------------------------------------------------

def entre(t, desde, hasta):
    """Progreso 0..1 de t entre dos momentos (fuera del tramo queda en 0 o 1)."""
    if hasta <= desde:
        return 1.0 if t >= hasta else 0.0
    return max(0.0, min(1.0, (t - desde) / (hasta - desde)))


def suave(x):
    """Aceleracion y frenado suaves (smoothstep)."""
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def salida(x):
    """Arranca rapido y frena al final (para cosas que aparecen)."""
    x = max(0.0, min(1.0, x))
    return 1 - (1 - x) ** 3


def rebote(x):
    """Se pasa un poco y vuelve (para cosas que 'saltan' al aparecer)."""
    x = max(0.0, min(1.0, x))
    c = 1.70158
    return 1 + (c + 1) * (x - 1) ** 3 + c * (x - 1) ** 2


def aparece(t, desde, dur=0.5):
    """Alfa de algo que aparece en `desde` y tarda `dur` en estar entero."""
    return salida(entre(t, desde, desde + dur))


def mezcla(a, b, x):
    return a + (b - a) * x


def mezcla_color(c1, c2, x):
    return tuple(int(round(mezcla(p, q, x))) for p, q in zip(c1, c2))


def integrar(f, desde, hasta, paso=1 / 60):
    """Integral numerica de f(t) entre dos momentos (para contadores que acumulan)."""
    total, t = 0.0, desde
    while t < hasta:
        dt = min(paso, hasta - t)
        total += f(t + dt / 2) * dt
        t += dt
    return total


# ---------------------------------------------------------------------------
# Recursos
# ---------------------------------------------------------------------------

@lru_cache(maxsize=None)
def fuente(nombre, tam):
    """Fuente a tamano `tam` en pixeles del video (ya escalada por SS)."""
    return ImageFont.truetype(str(FUENTES / nombre), int(round(tam * SS)))


@lru_cache(maxsize=None)
def sprite(nombre):
    return Image.open(SPRITES / f"{nombre}.png").convert("RGBA")


@lru_cache(maxsize=None)
def fondo_noche(grilla=0.35):
    """
    El cielo nocturno del menu: degradado de FondoNoche, la grilla de neon abajo,
    la linea del horizonte y la vineta. Es fijo, se arma una sola vez.
    """
    base = sprite("FondoNoche").resize((ANCHO, ALTO), Image.BILINEAR)
    if grilla > 0:
        alto_g = int(ALTO * 0.40)
        g = sprite("GrillaNeon").resize((ANCHO, alto_g), Image.BILINEAR)
        g.putalpha(g.getchannel("A").point(lambda a: int(a * grilla)))
        base.alpha_composite(g, (0, ALTO - alto_g))
        h = sprite("LineaHorizonte").resize((ANCHO, 4), Image.BILINEAR)
        h.putalpha(h.getchannel("A").point(lambda a: int(a * grilla * 1.4)))
        base.alpha_composite(h, (0, ALTO - alto_g - 2))
    v = sprite("Vineta").resize((ANCHO, ALTO), Image.BILINEAR)
    base.alpha_composite(v)
    return base.convert("RGB")


HORIZONTE = ALTO - int(ALTO * 0.40)


@lru_cache(maxsize=128)
def _fondo_sol(subida_cent):
    """Cielo con el sol retro asomando detras del horizonte (subida 0..100)."""
    subida = subida_cent / 100
    base = sprite("FondoNoche").resize((ANCHO, ALTO), Image.BILINEAR)
    halo = sprite("HaloSol").resize((1500, 1500), Image.BILINEAR)
    halo.putalpha(halo.getchannel("A").point(lambda a: int(a * 0.9 * subida)))
    base.alpha_composite(halo, (ANCHO // 2 - 750, HORIZONTE - 750 - int(70 * subida)))
    # El sol va en su propia capa para cortarlo justo en la linea del horizonte.
    sol = sprite("SolRetro").resize((480, 480), Image.LANCZOS)
    capa = Image.new("RGBA", (ANCHO, ALTO), (0, 0, 0, 0))
    cy = int(mezcla(HORIZONTE + 260, HORIZONTE - 70, subida))
    capa.alpha_composite(sol, (ANCHO // 2 - 240, cy - 240))
    capa.paste((0, 0, 0, 0), (0, HORIZONTE, ANCHO, ALTO))
    base.alpha_composite(capa)
    alto_g = ALTO - HORIZONTE
    # Resplandor sobre el horizonte, que se desvanece hacia arriba (sin corte).
    resp = sprite("ResplandorHorizonte").resize((ANCHO, 220), Image.BILINEAR)
    rampa = Image.linear_gradient("L").resize((ANCHO, 220))       # 0 arriba, 255 abajo
    alfa = np.asarray(resp.getchannel("A"), dtype=np.float32) * (np.asarray(rampa, dtype=np.float32) / 255) * 0.5
    resp.putalpha(Image.fromarray(alfa.astype(np.uint8)))
    base.alpha_composite(resp, (0, HORIZONTE - 218))
    g = sprite("GrillaNeon").resize((ANCHO, alto_g), Image.BILINEAR)
    g.putalpha(g.getchannel("A").point(lambda a: int(a * 0.6)))
    base.alpha_composite(g, (0, HORIZONTE))
    h = sprite("LineaHorizonte").resize((ANCHO, 4), Image.BILINEAR)
    base.alpha_composite(h, (0, HORIZONTE - 2))
    base.alpha_composite(sprite("Vineta").resize((ANCHO, ALTO), Image.BILINEAR))
    return base.convert("RGB")


def fondo_sol(subida):
    return _fondo_sol(int(round(max(0.0, min(1.0, subida)) * 100)))


@lru_cache(maxsize=None)
def letras_degradado(texto, tam, nombre=NEGRITA_ITALICA, arriba=CREMA, abajo=MAGENTA, espaciado=0):
    """
    Cada letra como imagen (a resolucion SS) pintada con el degradado de los
    titulos del juego, para poder animarlas de a una.
    Devuelve ([(imagen, x_izquierda_en_px_del_video)], ancho_total, alto).
    """
    f = fuente(nombre, tam)
    asc, desc = f.getmetrics()
    alto = asc + desc
    pad = int(tam * SS * 0.2)        # las italicas se salen de su caja
    grad = Image.new("RGBA", (1, alto))
    for y in range(alto):
        grad.putpixel((0, y), mezcla_color(arriba, abajo, suave(y / alto)) + (255,))
    letras, x = [], 0.0
    for ch in texto:
        w = f.getlength(ch)
        if ch != " ":
            ancho = int(w) + 2 * pad
            mascara = Image.new("L", (ancho, alto), 0)
            ImageDraw.Draw(mascara).text((pad, 0), ch, font=f, fill=255)
            im = grad.resize((ancho, alto))
            im.putalpha(mascara)
            letras.append((im, (x - pad) / SS))
        x += w + espaciado * SS
    return letras, (x - espaciado * SS) / SS, alto / SS


@lru_cache(maxsize=None)
def codigo_qr(url, lado):
    """QR con modulos oscuros sobre blanco (asi lo lee cualquier celular)."""
    import qrcode
    qr = qrcode.QRCode(border=2, box_size=10, error_correction=qrcode.constants.ERROR_CORRECT_M)
    qr.add_data(url)
    qr.make(fit=True)
    img = qr.make_image(fill_color=FONDO, back_color=(255, 255, 255)).convert("RGBA")
    return img.resize((lado * SS, lado * SS), Image.NEAREST)


# ---------------------------------------------------------------------------
# Lienzo: se dibuja en coordenadas del video (1920x1080) y por dentro se
# multiplica por SS. Lo que lleva brillo=True se dibuja ademas en una capa que
# despues se desenfoca: eso es el "neon".
# ---------------------------------------------------------------------------

def _rgba(color, alfa):
    return (color[0], color[1], color[2], int(max(0, min(1, alfa)) * 255))


class Lienzo:
    def __init__(self, fondo=None):
        self.fondo = fondo if fondo is not None else fondo_noche()
        self.capa = Image.new("RGBA", (ANCHO * SS, ALTO * SS), (0, 0, 0, 0))
        self.brillo = Image.new("RGBA", (ANCHO * SS, ALTO * SS), (0, 0, 0, 0))
        self._medidor = ImageDraw.Draw(self.capa)

    @staticmethod
    def _p(p):
        return (p[0] * SS, p[1] * SS)

    def _trazar(self, caja, dibujo, brillo, margen=0):
        """
        Dibuja en un parche del tamano de `caja` (px a resolucion SS) y lo mezcla
        con la capa. Hace falta porque ImageDraw, sobre una imagen RGBA, pisa los
        pixeles en vez de mezclarlos: una forma semitransparente "agujereaba" lo
        que tenia abajo.
        dibujo(draw, mover, grosor): `mover` pasa un punto del video al parche y
        `grosor` multiplica los anchos (la capa de brillo va mas gruesa).
        """
        destinos = [(self.capa, 1.0)] + ([(self.brillo, 2.2)] if brillo else [])
        for capa, grosor in destinos:
            m = margen * grosor + 4
            x0 = max(0, int(caja[0] - m)); y0 = max(0, int(caja[1] - m))
            x1 = min(capa.width, int(caja[2] + m) + 1); y1 = min(capa.height, int(caja[3] + m) + 1)
            if x1 <= x0 or y1 <= y0:
                continue
            parche = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
            mover = lambda q, x0=x0, y0=y0: (q[0] * SS - x0, q[1] * SS - y0)
            dibujo(ImageDraw.Draw(parche), mover, grosor)
            capa.alpha_composite(parche, (x0, y0))

    @staticmethod
    def _caja(puntos):
        xs = [q[0] * SS for q in puntos]; ys = [q[1] * SS for q in puntos]
        return (min(xs), min(ys), max(xs), max(ys))

    def linea(self, a, b, color, ancho=3, alfa=1.0, brillo=False):
        if alfa <= 0:
            return
        tinta = _rgba(color, alfa)

        def dibujo(d, mover, grosor):
            w = ancho * SS * grosor
            d.line([mover(a), mover(b)], fill=tinta, width=max(1, int(w)))
            if grosor == 1:
                r = w / 2
                for q in (a, b):   # puntas redondeadas
                    x, y = mover(q)
                    d.ellipse([x - r, y - r, x + r, y + r], fill=tinta)
        self._trazar(self._caja((a, b)), dibujo, brillo, ancho * SS)

    def linea_punteada(self, a, b, color, ancho=2, alfa=1.0, trazo=14, hueco=10, brillo=False):
        largo = math.dist(a, b)
        if largo < 1:
            return
        ux, uy = (b[0] - a[0]) / largo, (b[1] - a[1]) / largo
        s = 0.0
        while s < largo:
            e = min(largo, s + trazo)
            self.linea((a[0] + ux * s, a[1] + uy * s), (a[0] + ux * e, a[1] + uy * e),
                       color, ancho, alfa, brillo)
            s += trazo + hueco

    def circulo(self, c, r, color, alfa=1.0, relleno=True, ancho=3, brillo=False):
        if alfa <= 0 or r <= 0:
            return
        tinta = _rgba(color, alfa)

        def dibujo(d, mover, grosor):
            x, y = mover(c)
            rr = r * SS
            if relleno:
                d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=tinta)
            else:
                d.ellipse([x - rr, y - rr, x + rr, y + rr], outline=tinta, width=max(1, int(ancho * SS * grosor)))
        self._trazar(self._caja(((c[0] - r, c[1] - r), (c[0] + r, c[1] + r))), dibujo, brillo, ancho * SS)

    def arco(self, c, r, desde_grados, hasta_grados, color, ancho=3, alfa=1.0, brillo=False):
        if alfa <= 0:
            return
        a0, a1 = sorted((desde_grados, hasta_grados))
        tinta = _rgba(color, alfa)

        def dibujo(d, mover, grosor):
            x, y = mover(c)
            rr = r * SS
            d.arc([x - rr, y - rr, x + rr, y + rr], a0, a1, fill=tinta, width=max(1, int(ancho * SS * grosor)))
        self._trazar(self._caja(((c[0] - r, c[1] - r), (c[0] + r, c[1] + r))), dibujo, brillo, ancho * SS)

    def rect(self, a, b, color, alfa=1.0, relleno=True, ancho=2, radio=0, brillo=False):
        if alfa <= 0:
            return
        tinta = _rgba(color, alfa)

        def dibujo(d, mover, grosor):
            caja = [mover(a), mover(b)]
            if relleno:
                d.rounded_rectangle(caja, radius=radio * SS, fill=tinta)
            else:
                d.rounded_rectangle(caja, radius=radio * SS, outline=tinta, width=max(1, int(ancho * SS * grosor)))
        self._trazar(self._caja((a, b)), dibujo, brillo, ancho * SS)

    def poligono(self, puntos, color, alfa=1.0, brillo=False):
        if alfa <= 0:
            return
        tinta = _rgba(color, alfa)
        self._trazar(self._caja(puntos),
                     lambda d, mover, grosor: d.polygon([mover(q) for q in puntos], fill=tinta), brillo)

    def texto(self, pos, texto, tam, color=TINTA, alfa=1.0, nombre=NEGRITA, anchor="mm",
              espaciado=0, brillo=False):
        """Texto con anchor de Pillow ('mm' = centrado). `espaciado` en px entre letras."""
        if alfa <= 0 or not texto:
            return
        f = fuente(nombre, tam)
        tinta = _rgba(color, alfa)
        x, y = self._p(pos)
        if espaciado == 0:
            piezas = [(x, y, texto, anchor)]
        else:
            # Con espaciado entre letras: se arma letra por letra.
            anchos = [f.getlength(ch) for ch in texto]
            total = sum(anchos) + espaciado * SS * (len(texto) - 1)
            if anchor[0] == "m":
                x -= total / 2
            elif anchor[0] == "r":
                x -= total
            piezas = []
            for ch, w in zip(texto, anchos):
                piezas.append((x, y, ch, "l" + anchor[1]))
                x += w + espaciado * SS
        cajas = [self._medidor.textbbox((px, py), tx, font=f, anchor=an) for px, py, tx, an in piezas]
        caja = (min(c[0] for c in cajas), min(c[1] for c in cajas), max(c[2] for c in cajas), max(c[3] for c in cajas))

        def dibujo(d, mover, grosor):
            for px, py, tx, an in piezas:
                d.text(mover((px / SS, py / SS)), tx, font=f, fill=tinta, anchor=an)
        self._trazar(caja, dibujo, brillo, 8)

    def imagen(self, img, centro, escala=1.0, alfa=1.0, brillo=False):
        """
        Pega una imagen centrada en `centro`. `escala` es en px del video: una
        imagen ya hecha a resolucion SS va con escala=1/SS.
        """
        if alfa <= 0:
            return
        w, h = int(img.width * escala * SS), int(img.height * escala * SS)
        im = img.convert("RGBA")
        if (w, h) != im.size:
            im = im.resize((max(1, w), max(1, h)), Image.LANCZOS)
        if alfa < 1:
            im.putalpha(im.getchannel("A").point(lambda a: int(a * alfa)))
        x, y = self._p(centro)
        destino = (int(x - w / 2), int(y - h / 2))
        self.capa.alpha_composite(im, destino)
        if brillo:
            self.brillo.alpha_composite(im, destino)

    def titulo(self, texto, centro, tam, t, desde, alfa=1.0, espaciado=0, retraso=0.06):
        """Titulo con degradado que se arma letra por letra a partir de `desde`."""
        letras, ancho, alto = letras_degradado(texto, tam, espaciado=espaciado)
        x0 = centro[0] - ancho / 2
        for k, (im, x) in enumerate(letras):
            v = entre(t, desde + k * retraso, desde + k * retraso + 0.45)
            if v <= 0:
                continue
            sube = (1 - salida(v)) * 60
            cx = x0 + x + im.width / SS / 2
            self.imagen(im, (cx, centro[1] + sube), 1 / SS, alfa * salida(v), brillo=True)

    def cuadro(self, intensidad_brillo=1.0):
        """Junta fondo + neon + capa y devuelve el cuadro final (RGB 1920x1080)."""
        img = self.fondo.copy()
        if intensidad_brillo > 0 and self.brillo.getbbox():
            # Premultiplicado (RGBa): el color ya trae el alfa adentro y se puede
            # sumar al fondo como si fuera luz.
            chico = self.brillo.convert("RGBa").resize((ANCHO // 4, ALTO // 4), Image.BILINEAR)
            chico = chico.filter(ImageFilter.GaussianBlur(5))
            luz = np.asarray(chico.resize((ANCHO, ALTO), Image.BILINEAR), dtype=np.float32)[:, :, :3]
            suma = np.asarray(img, dtype=np.float32) + luz * (1.6 * intensidad_brillo)
            img = Image.fromarray(np.clip(suma, 0, 255).astype(np.uint8))
        capa = self.capa.convert("RGBa").resize((ANCHO, ALTO), Image.LANCZOS).convert("RGBA")
        img = img.convert("RGBA")
        img.alpha_composite(capa)
        return img.convert("RGB")


# ---------------------------------------------------------------------------
# Salida
# ---------------------------------------------------------------------------

class Video:
    """Le pasa cuadros a ffmpeg y sale un .mp4 en H.264."""

    def __init__(self, ruta):
        Path(ruta).parent.mkdir(parents=True, exist_ok=True)
        self.proc = subprocess.Popen(
            [imageio_ffmpeg.get_ffmpeg_exe(), "-y", "-loglevel", "error",
             "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{ANCHO}x{ALTO}", "-r", str(FPS),
             "-i", "-", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18",
             "-preset", "medium", str(ruta)],
            stdin=subprocess.PIPE)

    def agregar(self, img):
        self.proc.stdin.write(img.tobytes())

    def cerrar(self):
        self.proc.stdin.close()
        if self.proc.wait() != 0:
            raise RuntimeError("ffmpeg fallo al escribir el video")


def renderizar(escena, duracion, nombre, cuadros=None):
    """
    Renderiza `escena(t)` de 0 a `duracion` en render/<nombre>.mp4.
    Con `cuadros` (lista de segundos) solo guarda esos cuadros como PNG, para
    revisar rapido sin generar el video entero.
    """
    if cuadros:
        for t in cuadros:
            ruta = RENDER / f"{nombre}-{t:05.2f}s.png"
            ruta.parent.mkdir(parents=True, exist_ok=True)
            escena(t).save(ruta)
            print(ruta)
        return

    total = int(round(duracion * FPS))
    video = Video(RENDER / f"{nombre}.mp4")
    for i in range(total):
        video.agregar(escena(i / FPS))
        if i % FPS == 0:
            print(f"\r{nombre}: {i // FPS}/{int(duracion)} s", end="", flush=True)
    video.cerrar()
    print(f"\r{nombre}: listo -> {RENDER / (nombre + '.mp4')}")
