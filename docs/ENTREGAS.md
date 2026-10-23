# Cronograma de entregas — Crazy Moto

Calendario de entregas de la feria de ciencias, con el estado de cada una.

- Fecha de análisis: **2026-08-27** (última actualización: **2026-10-23**).
- **El estado de cada entrega es el oficial del cronograma de la materia**, tal
  como lo confirmó el alumno el 2026-10-23. Las notas de cada sección guardan, como
  historia, qué se hizo y cuándo.
- Alumnos: Joaquín Pastorino y Santiago Castiñeira.
- **Regla del profesor: los trabajos entregados fuera de tiempo valen CERO.**
- Total en juego: **10 puntos** (8 entregas de 1 punto + 1 entrega de 2 puntos).
- Los pendientes se siguen como **issues de GitHub**
  ([SIRWARRIOR2017/Crazy-Moto](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues)),
  agrupados en dos milestones: "Entrega 28 sep — Versión final" y "Entrega 05 nov
  — Publicación".

> **Sobre las fechas.** Lo que cuenta es la **fecha del documento de cada
> entrega**, no la fecha del commit en git (confirmado por el alumno el
> 2026-08-27). Con ese criterio, las entregas del 6 y 13 de agosto están **dentro
> de plazo** (los documentos llevan fecha 30 de julio y 13 de agosto
> respectivamente), aunque en el repo se hayan subido después.

---

## Resumen

| Fecha | Pts | Estado | Riesgo / pendiente |
|---|---|---|---|
| 06 ago | 1 | ✅ Terminada | Definiciones de diseño completas, con el boceto incluido. |
| 13 ago | 1 | ✅ Terminada | Plan de trabajo (`documento.docx`, fecha 13 ago) + UI base (paneles, botones) funcionando. |
| 20 ago | 1 | ✅ Terminada | Loop completo iniciar → terminar → continuar. |
| 27 ago | 1 | ✅ Terminada | Controladores programados y pusheados. |
| 03 sep | 1 | ✅ Terminada | Mecánica principal: esquivar autos por 3 carriles + wheelie de riesgo-recompensa + puntaje en pantalla. |
| 10 sep | 1 | ✅ Terminada | Datos persistentes (ranking con ScriptableObject + JSON) + sonido (música de menú, choque y click). |
| 17 sep | 1 | ✅ Terminada | Efectos/partículas, hardware externo (control por **webcam**), efectos de cámara e iluminación. |
| 28 sep | 1 | 🟡 Hecha, fuera de fecha | **Venció** antes de terminarla, y por la regla del profesor lo entregado fuera de tiempo vale cero: conviene confirmar con él qué pasa con esta. Ya está todo: build de Windows probado, [carpeta en Drive](https://drive.google.com/drive/folders/1uzdDwrgAJaFdZmjhe8o6fqzH9Q4M7_rZ?usp=drive_link) con el link en el README, efectos, sonido y pulido. El build se probó jugando, con la cámara y con el ranking que persiste al reabrir ([#13](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/13)). |
| 05 nov | 2 | ⏳ Pendiente | **Es la próxima fecha.** itch.io (0,5) + video de YouTube (0,5) + decoración del ambiente (1). Issues: [#15](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/15), [#16](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/16), [#17](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/17). |

**Estado al 2026-10-23:** 7 entregas terminadas (del 06 ago al 17 sep, 7 puntos).
La del **28 de septiembre** se terminó después de su fecha (falta confirmar con el profesor); la
próxima fecha es el **05 de noviembre** (2 puntos), que no hay que dejar que se
corra.

---

## 06 de agosto (inclusive) — 1 punto — Definiciones de diseño

Se responden en `docs/PROYECTO FERIA DE CIENCIAS.docx`.

| # | Ítem | Estado |
|---|---|---|
| 1 | Idea del juego | ✅ Definida (endless-runner de moto en 3D). |
| 1 | Boceto del juego | ✅ Hecho (confirmado en el cronograma oficial; issue #9 cerrado). |
| 1 | Condiciones de victoria / derrota / finalización | ✅ Definida: "el juego finaliza cuando el jugador se choca contra un objeto". No hay victoria (es endless), la finalización es el choque. |
| 2 | Loop general | ✅ Definido en el punto 1.2 del documento. |
| 3 | Upgrade (qué mejora/castiga al finalizar) | ✅ Definido en 1.3: el juego se acelera y se pone más difícil con el tiempo. |
| 4 | Mecánica principal | ✅ Definida en 1.4: controlar el wheelie y superar obstáculos consecutivos. |
| 5 | Recursos físicos para la feria | ✅ Definido en 1.5: computadora, enchufe, cargador, 2 mesas, proyector, silla y teclado. |
| 6 | ¿Multiplayer? | ✅ **Definido: no.** El juego es de **1 jugador** (confirmado por el alumno el 2026-08-27). |
| 7 | ¿Competitivo? / Ranking | ✅ **Definido.** Sí es competitivo. El ranking es una **comparación de jugadores y sus puntajes**: se ordena de mayor a menor, el puntaje más alto figura primero y el más bajo último (confirmado el 2026-08-27). Lo técnico quedó resuelto en la entrega del 10 sep: ranking local, nombre puesto una vez, Top 10. |

**Veredicto: ✅ TERMINADA.** Todos los ítems de diseño están definidos, incluido el
boceto.

**Nota:** conviene que el texto de multiplayer (1 jugador) y de ranking también
esté volcado en el punto 1.6 de `docs/PROYECTO FERIA DE CIENCIAS.docx`.

---

## 13 de agosto (inclusive) — 1 punto

### a) Documento "documento" con el PLAN de trabajo (uso de IA)

- Archivo: `docs/documento.docx` (fecha del documento: 13 de agosto de 2026).
- ✅ El documento existe y tiene el plan de trabajo, el alcance y la forma de
  trabajo con IA.
- ✅ **Dentro de plazo:** cuenta la fecha del documento (13 ago), no la del
  commit (que fue el 18 ago).

### b) Interfaz de usuario programada y funcionando

| Elemento | Estado |
|---|---|
| Paneles (menú / juego / game over) | ✅ `MenuManager` los controla con `SetActive`. En la escena desde el 13 ago. |
| Botones (Jugar, Salir, Continuar, Menú) | ✅ Cableados por `OnClick` a métodos de `MenuManager`. |
| Textos (TMP) | ✅ Título y botones desde el 13 ago; el HUD de juego (puntaje) se agregó con la mecánica principal del 3 sep. |
| Slider (volumen) | ✅ En su momento `SliderVolumen` + `VolumenSlider.cs`; desde el 31 ago son 3 sliders en el menú de Opciones. |
| Fondos | ✅ Fondo nocturno en el menú (2026-09-15); el resto de las pantallas se restilaron el 2026-09-30. |

**Veredicto: ✅ TERMINADA.** El plan de trabajo cuenta por su fecha (13 ago) y la
UI base (paneles, botones, slider, textos de menú) está programada y funcionando.

---

## 20 de agosto (inclusive) — 1 punto — Loop del juego funcionando

Pedido: el juego debe poder **iniciar, terminar y continuar**, con condiciones
de victoria/derrota/finalización.

| Parte | Estado |
|---|---|
| Iniciar (menú → Jugar) | ✅ `MenuManager.Jugar()` recarga la escena y arranca la partida. |
| Terminar (chocar → Game Over) | ✅ `PlayerController.OnTriggerEnter` → `GameManager.GameOver()` → panel de Game Over. |
| Continuar (botón Continuar) | ✅ `MenuManager.VolverAJugar()` recarga y arranca de nuevo. |
| Obstáculo de derrota | ✅ En su momento un cubo rojo hardcodeado en `GameManager`; hoy son los autos del `TrafficManager`. |

**Evidencia:** commits `b1a5b62`, `faa7963`, `12860ec` del **2026-08-18** (a
tiempo).

**Veredicto: ✅ TERMINADA y a tiempo.** Es el alcance de la "primera entrega"
descripto en `docs/documento.docx`.

**Nota histórica:** el camino se veía con huecos (el prefab `Tramo` medía 3 de
largo y se spawneaba cada 30); se arregló el 2026-08-31.

---

## 27 de agosto (inclusive) — 1 punto — Controladores programados

| Controlador | Estado |
|---|---|
| Controlador de juego (`GameManager.cs`) | ✅ Lleva velocidad, aceleración y estado de fin de juego. Desde el 13 ago. |
| Controlador de sonido (`AudioManager.cs`) | ✅ Singleton con `AudioSource` de música y de efectos. Enganchado a `GameOver()`. Desde el 26 ago. |
| Controlador de camino (`RoadManager.cs`) | ✅ Pooling circular de tramos. |
| Controlador de menú (`MenuManager.cs`) | ✅ Maneja los paneles y el flujo de escena. |
| Controlador del jugador (`PlayerController.cs`) | ✅ Movimiento, wheelie y detección de choque. |
| Slider de volumen | ✅ Lee/guarda `PlayerPrefs("Volumen")` y aplica `AudioListener.volume` (hoy dentro de `MenuOpciones` + `AudioManager`). |

**Evidencia:** todo commiteado y pusheado a `main` el **2026-08-27** (commits
`60bf03a` y `e493ba8`).

**Veredicto: ✅ TERMINADA.**

**Nota:** al momento de esta entrega `AudioManager` solo tenía asignado el sonido
de choque (`Crash.mp3`). Para "controlador programado" alcanzaba; el contenido de
audio se completó para la entrega del 10 de septiembre.

---

## 03 de septiembre (inclusive) — 1 punto — Mecánica principal

Pedido: mecánica principal programada y funcionando (movimientos, interacciones
físicas como colisiones o detecciones).

Diseño final (decidido el 2026-08-27): "autopista en contramano" — vienen autos
de frente por 3 carriles y se esquivan **solo** con movimiento lateral A-D.
**No hay salto**; la moto solo hace wheelie (Shift), que frena el movimiento
lateral y es la única forma de sumar puntaje.

| Parte | Estado |
|---|---|
| Movimiento lateral libre (A-D) entre 3 carriles | ✅ `PlayerController.MoverLateral()`, `Input.GetAxisRaw("Horizontal")`, clamp ±3 (hoy ±4). |
| Avance automático + aceleración con el tiempo | ✅ `PlayerController.Avanzar()` + `GameManager` sube `velocidadActual`. |
| Wheelie (Shift) con penalización de control | ✅ Mientras se sostiene, el jugador se mueve al 30% de costado (`factorLateralEnWheelie`). |
| Generación de obstáculos progresiva | ✅ `TrafficManager`: autos de frente por los 3 carriles, filas de 1-2 autos (nunca las 3), se juntan a más velocidad. Pooling. |
| Colisión / detección | ✅ `OnTriggerEnter` con tag `Obstaculo` → `GameOver()`. |

**Veredicto: ✅ TERMINADA (probada en Unity el 2026-08-27).** La mecánica central
—esquivar autos por carriles + wheelie de riesgo-recompensa— está programada y
funcionando.

**Nota histórica:** al momento de la entrega los autos eran cubos creados por
código y el HUD texto TMP armado por código (sin arte). Desde el 2026-09-09 los
autos y la moto son modelos 3D con rig, y desde el 2026-09-30 el HUD tiene el arte
de "Ruta de noche".

---

## 10 de septiembre (inclusive) — 1 punto

### a) Sonido implementado y programado

- ✅ `AudioManager` está programado y tiene los **3 clips asignados** en la
  escena: `Breakneck_Boulevard.mp3` (`musicaFondo`, música del menú),
  `Crash.mp3` (`sonidoChoque`) y `mouse-click-sound.mp3` (`sonidoClick`).
- ✅ Los 3 volúmenes (general / música / efectos) se ajustan desde Opciones y se
  guardan en `PlayerPrefs`.
- Los sonidos ambientales de la partida (motor de la moto, autos pasando) no
  son parte de esta entrega: la música es solo del menú por decisión de
  diseño. Quedan como pulido del 28 sep
  ([#12](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/12)).

### b) Datos persistentes (ScriptableObject o base de datos)

- ✅ `RankingData` es un `ScriptableObject` que guarda el ranking en
  `ranking.json` (`Application.persistentDataPath`). Probado el 2026-08-27.
- El jugador pone su nombre una vez (hoy en Opciones, `PlayerPrefs("NombreJugador")`);
  al chocar, su puntaje entra al ranking (`RankingData.Agregar`).
- El menú muestra el **Top 10** ordenado de mayor a menor, una línea "Estás en el
  puesto N" si quedás afuera, y el botón "Reiniciar tabla" (hoy en Opciones).
- Una fila por jugador: si repetís nombre, se queda tu mejor puntaje.
- Se probó cerrando y reabriendo Unity: el ranking persiste.

**Veredicto: ✅ TERMINADA.** Las dos partes están: datos persistentes (ranking en
JSON) y sonido (los 3 clips asignados y sonando, 2026-09-09).

---

## 17 de septiembre (inclusive) — 1 punto

| Ítem | Estado |
|---|---|
| Efectos: partículas | ✅ Terminada según el cronograma oficial. Las partículas de choque, wheelie y velocidad se siguen puliendo en [#10](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/10). |
| Efectos: interacción con hardware externo | ✅ El juego se controla con la **webcam**: `vision/deteccion.py` (MediaPipe) detecta los gestos del jugador y se los manda a Unity por UDP (`EntradaCamara.cs`). Programado el 2026-09-09; reemplaza al manubrio de Arduino, que salía caro. |
| Efectos de cámara | ✅ Cámara de **primera persona** por código (`CamaraJugador.cs`): sigue pegada al `PuntoCamara` de la moto, se tumba (roll) al esquivar y sube la vista al cielo mientras se sostiene el wheelie. Más efectos y luces de efecto: [#11](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/11). |
| Iluminación implementada | ✅ Desde el 2026-09-30: atardecer (sol bajo y cálido a la vista, skybox procedural, luz ambiente Trilight) y niebla del color del horizonte. |

**Veredicto: ✅ TERMINADA.**

---

## 28 de septiembre (inclusive) — 1 punto — Versión final + build

Pedido: "Versión final funcionando sin errores, UI estéticamente linda y coherente
con el arte, audio y efectos funcionando" + "Proyecto buildeado ejecutable sin
errores (crear carpeta en Drive, compartir y colocar el link en el Readme del
repositorio)".

**Estado oficial: NO marcada como terminada. La fecha ya venció (hoy es
2026-10-23).**

Lo que se hizo (cerrado el 2026-10-23, después de la fecha):

| Tema | Estado |
|---|---|
| Juego completo | ✅ Moto y autos 3D con rig, cámara en 1ª persona, UI "Ruta de noche", entorno de ciudad y campo. |
| Efectos y sonido de la partida | ✅ Partículas [#10](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/10), efectos de cámara y post-proceso [#11](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/11), motor y autos pasando [#12](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/12). |
| Jugabilidad y pulido | ✅ [#18](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/18) a [#23](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/23), y el control por cámara rehecho como "manubrio en el aire" [#35](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/35). |
| Build ejecutable | ✅ Build de Windows sin errores, probado por el alumno: el control por cámara anda y el ranking persiste al cerrar y reabrir el juego ([#13](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/13)). |
| Carpeta en Drive + link en el README | ✅ [Carpeta del juego](https://drive.google.com/drive/folders/1uzdDwrgAJaFdZmjhe8o6fqzH9Q4M7_rZ?usp=drive_link), enlazada al principio del `README.md` ([#14](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/14)). |

**Veredicto: 🟡 HECHA, PERO FUERA DE FECHA.**

---

## 05 de noviembre (inclusive) — 2 puntos

Es la **próxima fecha**. Se sigue en el milestone "Entrega 05 nov — Publicación".

| Ítem | Puntos | Estado |
|---|---|---|
| Publicación en itch.io | 0.5 | ⏳ Pendiente — [#15](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/15). Necesita el build ([#13](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/13)). |
| Video de promoción en YouTube | 0.5 | ⏳ Pendiente — [#16](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/16). |
| Decoración del ambiente (folletería, decoración relacionada al juego) | 1 | ⏳ Pendiente — [#17](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/17). |

**Veredicto: ⏳ PENDIENTE.**

---

## Qué hacer ahora (prioridad)

1. **Entrega del 28 sep:** está todo hecho. Falta hablar con el profesor por la
   fecha vencida.
2. **Entrega del 05 nov:** itch.io ([#15](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/15)),
   video de YouTube ([#16](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/16))
   y decoración del stand ([#17](https://github.com/SIRWARRIOR2017/Crazy-Moto/issues/17)).
   Recordá que lo entregado fuera de tiempo vale cero.
