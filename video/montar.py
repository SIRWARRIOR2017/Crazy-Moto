"""
Arma el video final de Crazy Moto: escenas animadas + fotos + clips del juego +
voz + musica, en el orden del guion (guion.md).

Uso:
    py video/montar.py          arma render/crazy-moto.mp4

Antes hay que renderizar las escenas (py video/escenas.py <escena>). Lo que
falte de material/ (un clip, la voz, la musica) se reemplaza por una placa que
dice que va ahi, asi el video se puede ver entero aunque este a medio hacer.

Material esperado (carpeta material/, no se sube al repo):
    fotos/menu.png                         la captura del menu
    clips/ciudad.mp4, campo.mp4, wheelie.mp4, choque.mp4, ranking.mp4
    voz/voz-1.* ... voz/voz-7.*            un archivo por tramo del guion
    musica.*                               opcional
"""

import subprocess
from pathlib import Path

import imageio_ffmpeg
from PIL import Image

from estilo import (ALTO, ANCHO, CYAN, FPS, MEDIA, RENDER, TINTA_CLARA, TINTA_SUAVE, Lienzo,
                    Video, aparece, suave)

MATERIAL = Path(__file__).resolve().parent / "material"
TEMP = RENDER / "partes"
FUNDIDO = 0.4           # segundos de fundido entre partes
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

# (tramo del guion, tipo, origen, duracion, desde_segundo)
PARTES = [
    (1, "escena", "1-gancho", 6.0, 0),
    (2, "foto", "fotos/menu.png", 4.0, 0),
    (2, "clip", "clips/ciudad.mp4", 4.5, 2),
    (2, "escena", "2-autopista", 8.0, 0),
    (3, "escena", "3-wheelie", 16.0, 0),
    (4, "escena", "4-camara", 27.0, 0),
    (5, "escena", "5-problema", 15.0, 0),
    (6, "clip", "clips/campo.mp4", 2.6, 2),
    (6, "clip", "clips/wheelie.mp4", 2.6, 2),
    (6, "clip", "clips/choque.mp4", 2.6, 1),
    (6, "clip", "clips/ranking.mp4", 2.6, 0),
    (7, "escena", "7-cierre", 10.0, 0),
]


def ffmpeg(*args):
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", *map(str, args)], check=True)


def placa(texto, duracion, salida):
    """Placa provisoria para el material que todavia no esta."""
    video = Video(salida)
    for i in range(int(round(duracion * FPS))):
        t = i / FPS
        lz = Lienzo()
        lz.rect((560, 400), (1360, 680), CYAN, 0.6, relleno=False, ancho=2, radio=10)
        lz.texto((960, 490), "ACÁ VA", 30, TINTA_SUAVE, aparece(t, 0, 0.3), espaciado=6)
        lz.texto((960, 570), texto, 40, TINTA_CLARA, aparece(t, 0, 0.3), nombre=MEDIA)
        video.agregar(lz.cuadro())
    video.cerrar()


def foto_con_zoom(ruta, duracion, salida):
    """La foto con un zoom lento hacia el centro (efecto Ken Burns)."""
    img = Image.open(ruta).convert("RGB")
    escala = max(ANCHO / img.width, ALTO / img.height) * 1.12
    base = img.resize((int(img.width * escala), int(img.height * escala)), Image.LANCZOS)
    video = Video(salida)
    total = int(round(duracion * FPS))
    for i in range(total):
        z = 1.0 + 0.10 * suave(i / max(1, total - 1))          # 1.00 -> 1.10
        w, h = ANCHO * 1.12 / z, ALTO * 1.12 / z
        cx, cy = base.width / 2, base.height / 2
        cuadro = base.crop((int(cx - w / 2), int(cy - h / 2), int(cx + w / 2), int(cy + h / 2)))
        video.agregar(cuadro.resize((ANCHO, ALTO), Image.LANCZOS))
    video.cerrar()


def preparar(k, tramo, tipo, origen, duracion, desde):
    salida = TEMP / f"{k:02d}.mp4"
    if tipo == "escena":
        ruta = RENDER / f"{origen}.mp4"
        if not ruta.exists():
            raise SystemExit(f"Falta {ruta}: renderizala con py video/escenas.py")
        ffmpeg("-i", ruta, "-t", duracion, "-an", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-r", FPS, salida)
    elif tipo == "foto":
        ruta = MATERIAL / origen
        if ruta.exists():
            foto_con_zoom(ruta, duracion, salida)
        else:
            placa(f"la foto {Path(origen).name}", duracion, salida)
    else:
        ruta = MATERIAL / origen
        if ruta.exists():
            ffmpeg("-ss", desde, "-i", ruta, "-t", duracion, "-an",
                   "-vf", f"scale={ANCHO}:{ALTO}:force_original_aspect_ratio=increase,crop={ANCHO}:{ALTO},fps={FPS}",
                   "-c:v", "libx264", "-pix_fmt", "yuv420p", salida)
        else:
            placa(f"el clip «{Path(origen).stem}»", duracion, salida)
    return salida


def buscar(carpeta, nombre):
    for ruta in sorted(carpeta.glob(nombre + ".*")):
        return ruta
    return None


def main():
    TEMP.mkdir(parents=True, exist_ok=True)
    archivos, inicios, t = [], {}, 0.0
    for k, (tramo, tipo, origen, duracion, desde) in enumerate(PARTES):
        print(f"parte {k + 1}/{len(PARTES)}: {origen}")
        archivos.append(preparar(k, tramo, tipo, origen, duracion, desde))
        inicios.setdefault(tramo, t)
        t += duracion - FUNDIDO
    total = t + FUNDIDO

    # Video: cadena de fundidos (xfade) entre todas las partes.
    entradas, filtro, previo, offset = [], [], "0:v", 0.0
    for archivo in archivos:
        entradas += ["-i", archivo]
    for k in range(1, len(archivos)):
        offset += PARTES[k - 1][3] - FUNDIDO
        salida = f"v{k}"
        filtro.append(f"[{previo}][{k}:v]xfade=transition=fade:duration={FUNDIDO}:offset={offset:.3f}[{salida}]")
        previo = salida

    # Audio: cada tramo de voz arranca un poco despues de que empieza su parte.
    audio, n = [], len(archivos)
    voces = []
    for tramo in sorted(inicios):
        ruta = buscar(MATERIAL / "voz", f"voz-{tramo}")
        if ruta:
            entradas += ["-i", ruta]
            ms = int((inicios[tramo] + 0.3) * 1000)
            audio.append(f"[{n}:a]adelay={ms}|{ms},aresample=48000[voz{tramo}]")
            voces.append(f"[voz{tramo}]")
            n += 1
    if voces:
        audio.append(f"{''.join(voces)}amix=inputs={len(voces)}:normalize=0,apad[voz]")
    musica = buscar(MATERIAL, "musica")
    if musica:
        entradas += ["-stream_loop", "-1", "-i", musica]
        audio.append(f"[{n}:a]volume=0.35,aresample=48000,afade=t=in:d=1.5,"
                     f"afade=t=out:st={total - 2.5:.2f}:d=2.5[mus]")
        if voces:
            # La musica baja sola cuando hablas (sidechain).
            audio.append("[voz]asplit[voz1][voz2]")
            audio.append("[mus][voz2]sidechaincompress=threshold=0.03:ratio=8:attack=20:release=400[musb]")
            audio.append("[voz1][musb]amix=inputs=2:normalize=0[audio]")
        else:
            audio.append("[mus]anull[audio]")
    elif voces:
        audio.append("[voz]anull[audio]")

    mapa = ["-map", f"[{previo}]"]
    if audio:
        mapa += ["-map", "[audio]", "-c:a", "aac", "-b:a", "192k"]
    destino = RENDER / "crazy-moto.mp4"
    ffmpeg(*entradas, "-filter_complex", ";".join(filtro + audio), *mapa, "-t", f"{total:.3f}",
           "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-r", FPS, "-movflags", "+faststart", destino)
    print(f"listo -> {destino} ({total:.1f} s)")
    for tramo in sorted(inicios):
        print(f"  tramo {tramo} empieza en {inicios[tramo]:5.1f} s")


if __name__ == "__main__":
    main()
