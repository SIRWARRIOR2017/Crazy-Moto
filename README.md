# 🏍️ Crazy Moto

[![Unity](https://img.shields.io/badge/Unity-6000.5.8f1-000000?logo=unity&logoColor=white)](https://unity.com/releases/editor/archive)
[![URP](https://img.shields.io/badge/Render-URP%2017.5-1a6fb4)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.5/manual/index.html)
[![Python](https://img.shields.io/badge/Python-3.14.3-3776AB?logo=python&logoColor=white)](https://www.python.org/downloads/)
[![MediaPipe](https://img.shields.io/badge/MediaPipe-1.0.1-00A98F)](https://ai.google.dev/edge/mediapipe/solutions/guide)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows-0078D6?logo=windows&logoColor=white)](#)

> Endless-runner de moto en 3D. Proyecto integrador para la **Feria de Ciencias**.
> Se juega con el teclado… **o moviendo los brazos frente a la webcam**, como si
> agarraras un manubrio de verdad.

---

## 🎮 De qué trata

Avanzás por una autopista **en contramano**, esquivando autos que vienen de frente
por tres carriles. Para sumar puntos tenés que sostener el **wheelie** (la rueda
delantera arriba), pero mientras lo hacés casi no podés moverte de costado: ahí
está el riesgo. El juego se va acelerando solo, así que cada vez es más difícil.

Cuando chocás, se termina la partida y tu puntaje entra al **ranking**, que se
guarda en disco y se ve en el menú (Top 10 + en qué puesto quedaste).

---

## 🕹️ Controles

Hay **dos formas de jugar**, y funcionan las dos al mismo tiempo. Si el control
por cámara está andando manda la cámara; si no, manda el teclado.

| Acción | ⌨️ Teclado | 📷 Cámara web |
|---|---|---|
| Mover a la izquierda | `A` o `←` | Tirás el brazo **izquierdo** hacia el cuerpo y empujás el derecho |
| Mover a la derecha | `D` o `→` | Tirás el brazo **derecho** hacia el cuerpo y empujás el izquierdo |
| Wheelie (sumar puntos) | Mantener `Shift` | Tirás **los dos brazos** hacia el cuerpo a la vez |
| Pausa | `Esc` | — (usá el botón `II` del HUD) |

El gesto de la cámara es el de un manubrio real: para doblar a la derecha tirás
del puño derecho y empujás el izquierdo. Para el wheelie, el tirón de los dos
puños que hacés en una moto para levantar la rueda.

---

## ⚙️ Instalación paso a paso

### Paso 1 — Instalar Unity

1. Bajá e instalá el **[Unity Hub](https://unity.com/download)**.
2. Dentro del Hub, andá a **Installs → Install Editor** e instalá la versión
   **`6000.5.8f1`** (Unity 6.5).
   Si no aparece en la lista, buscala en el
   [archivo de versiones](https://unity.com/releases/editor/archive).

> ⚠️ Tiene que ser esa versión. Con una más nueva Unity te va a pedir actualizar
> el proyecto y puede romper cosas.

### Paso 2 — Bajar el proyecto

```bash
git clone https://github.com/SIRWARRIOR2017/Crazy-Moto.git
cd Crazy-Moto
```

> 📦 Los modelos 3D (`.glb`) y el modelo de detección de pose están
> **incluidos en el repo**. No hace falta bajar nada por separado ni configurar
> Git LFS.

### Paso 3 — Abrir el proyecto

1. En el Unity Hub: **Add → Add project from disk** y elegí la carpeta `Crazy-Moto`.
2. Abrilo con la versión `6000.5.8f1`.

**La primera vez tarda varios minutos.** Unity descarga solo todos los paquetes
que están en `Packages/manifest.json` (URP, glTFast, TextMeshPro, Input System…)
e importa los modelos 3D. **No tenés que instalar ningún paquete a mano.**

### Paso 4 — Jugar

1. En el panel *Project*, abrí `Assets/Scenes/ESCENA 1.unity`.
2. Apretá **▶ Play**.
3. Poné tu nombre en **Opciones → Perfil** para figurar en el ranking.

Con esto ya podés jugar con el teclado. **El control por cámara es opcional.**

---

### Paso 5 *(opcional)* — Control por cámara web 📷

Esta parte reemplaza al teclado usando la webcam. Es lo que se muestra en la
feria, pero el juego funciona perfecto sin ella.

#### 5.1 · Instalar Python

Bajá **[Python](https://www.python.org/downloads/)** e instalalo.

> ✅ En el instalador, marcá la casilla **"Add python.exe to PATH"**. Si no,
> los comandos de abajo no van a funcionar.

Probado con **Python 3.14.3**. MediaPipe no declara una versión mínima, así que
otras versiones 3.x recientes deberían andar igual.

Para verificar que quedó bien instalado:

```bash
python --version
```

#### 5.2 · Instalar las librerías

Desde la carpeta del proyecto:

```bash
python -m pip install -r vision/requirements.txt
```

Esto instala **MediaPipe** (detección de pose) y **OpenCV** (cámara e imagen).
Son unos cuantos MB, tarda un rato la primera vez.

#### 5.3 · Encender el detector

```bash
python vision/deteccion.py
```

Se abre una ventana con tu silueta marcada. **Quedate quieto 2 segundos** con los
brazos en posición de manubrio: eso es la calibración, y aprende cómo tenés los
brazos en reposo. Cuando dice `OK - te estoy viendo`, ya está.

| Tecla | Qué hace |
|---|---|
| `c` | Recalibrar la pose neutra |
| `q` | Cerrar el detector |

Opciones útiles:

```bash
python vision/deteccion.py --camara 1      # si tenés más de una cámara
python vision/deteccion.py --sin-ventana   # sin la ventana de OpenCV (para la feria)
```

#### 5.4 · Comprobar que el juego lo recibe

Con el detector abierto, entrá en el juego a **Opciones → Probar cámara**. Vas a
ver en vivo si te está leyendo, hacia dónde estás doblando y si se activa el
wheelie. Sirve para acomodarte antes de empezar la partida.

Si dice **"SIN SEÑAL"**, el detector no está corriendo (volvé al paso 5.3).

---

## 📦 Resumen de dependencias

| Qué | Versión | Cómo se instala | ¿Obligatorio? |
|---|---|---|---|
| Unity Editor | `6000.5.8f1` | A mano, desde el Unity Hub (paso 1) | ✅ Sí |
| Paquetes de Unity (URP, glTFast, TMP, Input System…) | ver `Packages/manifest.json` | 🔄 Solos, al abrir el proyecto | ✅ Sí |
| Modelos 3D (`.glb`) y modelo de pose (`.task`) | — | 📦 Ya vienen en el repo | ✅ Sí |
| Python | probado en `3.14.3` | A mano (paso 5.1) | ❌ Solo para la cámara |
| MediaPipe + OpenCV | `1.0.1` / `5.0.0` | `pip install -r vision/requirements.txt` | ❌ Solo para la cámara |

---

## 🗂️ Estructura del proyecto

```
Crazy-Moto/
├── Assets/
│   ├── Scenes/ESCENA 1.unity     ← la escena única del juego
│   ├── Scripts/                  ← todo el código C#
│   ├── Models/                   ← modelos 3D crudos (.glb)
│   ├── Resources/Autos/          ← prefabs de los autos del tráfico
│   └── Materials/  Prefabs/  Sounds/  Settings/
├── vision/                       ← control por cámara (Python)
│   ├── deteccion.py              ← el detector de gestos
│   ├── modelo/                   ← modelo de MediaPipe (.task)
│   └── README.md                 ← detalle técnico de cómo funciona
├── docs/                         ← documentos de la feria, auditoría y entregas
└── README.md                     ← este archivo
```

> 🔍 ¿Querés saber **cómo funciona** la detección por dentro (por qué no se usa la
> profundidad de la cámara, cómo se separan los dos gestos)? Está explicado en
> **[`vision/README.md`](vision/README.md)**.

---

## 🚑 Problemas comunes

| Síntoma | Solución |
|---|---|
| Unity pide actualizar el proyecto | Estás abriendo con otra versión. Instalá la `6000.5.8f1` |
| Errores raros al abrir por primera vez | Esperá: todavía está bajando paquetes e importando modelos |
| `com.unity.ai.assistant` da error al clonar | Es un paquete *pre-release* que necesita cuenta de Unity con IA. Si molesta, borrá esa línea de `Packages/manifest.json` (el juego no la usa) |
| `python` no se reconoce como comando | Reinstalá Python marcando **"Add python.exe to PATH"** |
| `no pude abrir la camara 0` | Otro programa la está usando (Zoom, Meet, Teams). Cerralo, o probá `--camara 1` |
| El juego dice "SIN SEÑAL" | El detector no está corriendo. Ejecutá `python vision/deteccion.py` |
| "CONECTADA, PERO NO TE VEO" | Falta luz, o no entran tus hombros y codos en el cuadro. Alejate un poco |
| La moto dobla sola | Recalibrá: apretá `c` en la ventana del detector, quieto y simétrico |
| El wheelie no se activa | Mirá la barra `wheelie` de la ventana: si al tirar los brazos a fondo no llega a la línea blanca, bajá `WHEELIE_DELTA` en `vision/deteccion.py`. **Calibrá con los brazos algo estirados**, no encogidos |

---

## 👥 Integrantes

- **Joaquín Pastorino**
- **Santiago Castiñeira**
