# Cronograma de entregas — Crazy Moto

Calendario de entregas de la feria de ciencias, con el estado de cada una según
lo que hay **hoy en el proyecto**.

- Fecha de análisis: **2026-08-27** (última actualización: 2026-09-09).
- Alumnos: Joaquín Pastorino y Santiago Castiñeira.
- **Regla del profesor: los trabajos entregados fuera de tiempo valen CERO.**
- Total en juego: **10 puntos** (8 entregas de 1 punto + 1 entrega de 2 puntos).

> **Sobre las fechas.** Lo que cuenta es la **fecha del documento de cada
> entrega**, no la fecha del commit en git (confirmado por el alumno el
> 2026-08-27). Con ese criterio, las entregas del 6 y 13 de agosto están **dentro
> de plazo** (los documentos llevan fecha 30 de julio y 13 de agosto
> respectivamente), aunque en el repo se hayan subido después.

---

## Resumen

| Fecha | Pts | Estado | Riesgo |
|---|---|---|---|
| 06 ago | 1 | ⚠️ Casi | Multiplayer y ranking ya definidos. **Solo falta el boceto** en el documento. |
| 13 ago | 1 | ✅ Cumplida | Plan de trabajo (`documento.docx`, fecha 13 ago) + UI base (paneles, botones) funcionando. |
| 20 ago | 1 | ✅ Cumplida | Loop completo iniciar → terminar → continuar. |
| 27 ago | 1 | ✅ Cumplida | Controladores programados y pusheados. |
| 03 sep | 1 | ✅ Cumplida | Mecánica principal: esquivar autos por 3 carriles + wheelie de riesgo-recompensa + puntaje en pantalla. Probada el 27 ago. |
| 10 sep | 1 | ✅ Cumplida | Datos persistentes ✅ (ranking con ScriptableObject + JSON) + sonido ✅ (música de menú, choque y click asignados). |
| 17 sep | 1 | 🟡 A medias | Control por **webcam** programado (falta probarlo) → cubre "hardware externo". Faltan partículas e iluminación. (La cámara del juego ya tiene efectos por código: 1ª persona con roll y subida al cielo en el wheelie.) |
| 28 sep | 1 | ❌ Pendiente | Sin build, sin carpeta de Drive, sin link en el README. Arte a medias: la moto y los autos ya son modelos 3D con rig (ruedas girando, manubrio) y el **menú principal ya tiene arte** ("Ruta de noche", 2026-09-15); faltan las otras pantallas de UI y el camino, que sigue siendo un cubo gris. |
| 05 nov | 2 | ❌ Pendiente | Sin itch.io, sin video, sin decoración. |

**Estado al 2026-09-09:** 5 entregas cumplidas (13, 20, 27 ago, 3 sep y 10 sep);
la del 6 ago queda a un paso (falta solo el boceto). La próxima con trabajo de
código es la del **17 de septiembre** (partículas, efectos e iluminación).

---

## 06 de agosto (inclusive) — 1 punto — Definiciones de diseño

Se responden en `docs/PROYECTO FERIA DE CIENCIAS.docx`.

| # | Ítem | Estado |
|---|---|---|
| 1 | Idea del juego | ✅ Definida (endless-runner de moto en 3D). |
| 1 | Boceto del juego | ❌ **No hecho.** No hay ningún boceto/dibujo en el repo ni en el documento. |
| 1 | Condiciones de victoria / derrota / finalización | ✅ Definida: "el juego finaliza cuando el jugador se choca contra un objeto". No hay victoria (es endless), la finalización es el choque. |
| 2 | Loop general | ✅ Definido en el punto 1.2 del documento. |
| 3 | Upgrade (qué mejora/castiga al finalizar) | ✅ Definido en 1.3: el juego se acelera y se pone más difícil con el tiempo. |
| 4 | Mecánica principal | ✅ Definida en 1.4: controlar el wheelie y superar obstáculos consecutivos. |
| 5 | Recursos físicos para la feria | ✅ Definido en 1.5: computadora, enchufe, cargador, 2 mesas, proyector, silla y teclado. |
| 6 | ¿Multiplayer? | ✅ **Definido: no.** El juego es de **1 jugador** (confirmado por el alumno el 2026-08-27). |
| 7 | ¿Competitivo? / Ranking | ✅ **Definido.** Sí es competitivo. El ranking es una **comparación de jugadores y sus puntajes**: se ordena de mayor a menor, el puntaje más alto figura primero y el más bajo último (confirmado el 2026-08-27). Falta decidir lo técnico (¿local o en línea?, ¿ingreso de nombre?, ¿cuántos puestos se muestran?). |

**Veredicto: CASI COMPLETA.** Idea, condiciones, loop, upgrade, mecánica,
recursos, multiplayer (1 jugador) y ranking (comparación de puntajes, de mayor a
menor) están definidos. **Solo falta el boceto del juego.**

**Para cerrarla:** agregar el boceto al documento (una foto de un dibujo a mano
alcanza). Conviene también volcar al documento (`docs/PROYECTO FERIA DE
CIENCIAS.docx`, punto 1.6) el texto de multiplayer y ranking que hoy solo está
acá.

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
| Textos (TMP) | ⚠️ Hay textos en título y botones. **El HUD de juego (`PanelJuego`) está vacío** (no hay velocidad, distancia ni puntaje). |
| Slider (volumen) | ✅ `SliderVolumen` + `VolumenSlider.cs`. |
| Fondos | ⚠️ Hay un `Background` en el menú. Sin fondo/arte durante el juego. |

**Veredicto: ✅ CUMPLIDA.** El plan de trabajo cuenta por su fecha (13 ago) y la
UI base (paneles, botones, slider, textos de menú) está programada y funcionando.

**Pendiente para más adelante (no bloquea esta entrega):** el HUD de juego
(`PanelJuego`) está vacío; se llena cuando exista el puntaje.

---

## 20 de agosto (inclusive) — 1 punto — Loop del juego funcionando

Pedido: el juego debe poder **iniciar, terminar y continuar**, con condiciones
de victoria/derrota/finalización.

| Parte | Estado |
|---|---|
| Iniciar (menú → Jugar) | ✅ `MenuManager.Jugar()` recarga la escena y arranca la partida. |
| Terminar (chocar → Game Over) | ✅ `PlayerController.OnTriggerEnter` → `GameManager.GameOver()` → panel de Game Over. |
| Continuar (botón Continuar) | ✅ `MenuManager.VolverAJugar()` recarga y arranca de nuevo. |
| Obstáculo de derrota | ✅ Cubo rojo hardcodeado en `GameManager` (marcado como temporal). |

**Evidencia:** commits `b1a5b62`, `faa7963`, `12860ec` del **2026-08-18** (a
tiempo).

**Veredicto: ✅ CUMPLIDA y a tiempo.** Es el alcance de la "primera entrega"
descripto en `docs/documento.docx`, y está funcionalmente completo.

**Salvedad:** el camino se ve con huecos (el prefab `Tramo` mide 3 de largo y se
spawnea cada 30). No afecta esta entrega, pero sí cuenta para "ejecutable sin
errores visuales" del 28 de septiembre.

---

## 27 de agosto (inclusive) — 1 punto — Controladores programados

| Controlador | Estado |
|---|---|
| Controlador de juego (`GameManager.cs`) | ✅ Lleva velocidad, aceleración y estado de fin de juego. Desde el 13 ago. |
| Controlador de sonido (`AudioManager.cs`) | ✅ Singleton con `AudioSource` de música y de efectos. Enganchado a `GameOver()`. Desde el 26 ago. |
| Controlador de camino (`RoadManager.cs`) | ✅ Pooling circular de tramos. |
| Controlador de menú (`MenuManager.cs`) | ✅ Maneja los 3 paneles y el flujo de escena. |
| Controlador del jugador (`PlayerController.cs`) | ✅ Movimiento, wheelie y detección de choque. |
| Slider de volumen (`VolumenSlider.cs`) | ✅ Lee/guarda `PlayerPrefs("Volumen")` y aplica `AudioListener.volume`. |

**Evidencia:** todo commiteado y pusheado a `main` el **2026-08-27** (commits
`60bf03a` y `e493ba8`).

**Veredicto: ✅ CUMPLIDA** (la fecha límite es hoy, inclusive).

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
| Movimiento lateral libre (A-D) entre 3 carriles | ✅ `PlayerController.MoverLateral()`, `Input.GetAxisRaw("Horizontal")`, clamp ±3. |
| Avance automático + aceleración con el tiempo | ✅ `PlayerController.Avanzar()` + `GameManager` sube `velocidadActual`. |
| Wheelie (Shift) con penalización de control | ✅ Mientras se sostiene, el jugador se mueve al 30% de costado (`factorLateralEnWheelie`). |
| Generación de obstáculos progresiva | ✅ `TrafficManager`: autos de frente por los 3 carriles, filas de 1-2 autos (nunca las 3), se juntan a más velocidad. Pooling. |
| Colisión / detección | ✅ `OnTriggerEnter` con tag `Obstaculo` → `GameOver()`. |

**Veredicto: ✅ CUMPLIDA (probada en Unity el 2026-08-27).** La mecánica central
—esquivar autos por carriles + wheelie de riesgo-recompensa— está programada y
funcionando.

**Nota:** al momento de la entrega los autos eran cubos creados por código y el
HUD texto TMP armado por código (sin arte). Desde el 2026-09-09 los autos y la
moto ya son modelos 3D con rig; el HUD sigue en greybox. El arte real es trabajo
de las entregas de estética (28 de septiembre).

---

## 10 de septiembre (inclusive) — 1 punto

### a) Sonido implementado y programado

- ✅ **Hecho.** `AudioManager` está programado y tiene los **3 clips asignados**
  en la escena: `Breakneck_Boulevard.mp3` (`musicaFondo`, música del menú),
  `Crash.mp3` (`sonidoChoque`) y `mouse-click-sound.mp3` (`sonidoClick`).
- ✅ Los 3 volúmenes (general / música / efectos) se ajustan desde Opciones y se
  guardan en `PlayerPrefs`.
- 🟡 Faltan sonidos ambientales de la mecánica (motor de la moto, autos
  pasando). No bloquean esta entrega: la música es solo del menú por decisión de
  diseño, y el choque y el click ya cubren los efectos. Queda como pulido.

### b) Datos persistentes (ScriptableObject o base de datos)

- ✅ **Hecho (probado el 2026-08-27).** `RankingData` es un `ScriptableObject`
  que guarda el ranking en `ranking.json` (`Application.persistentDataPath`).
- El jugador pone su nombre una vez en el menú (`PlayerPrefs("NombreJugador")`);
  al chocar, su puntaje entra al ranking (`RankingData.Agregar`).
- El menú muestra el **Top 10** ordenado de mayor a menor, una línea "Estás en el
  puesto N" si quedás afuera, y un botón "Reiniciar tabla".
- Una fila por jugador: si repetís nombre, se queda tu mejor puntaje.
- Se probó cerrando y reabriendo Unity: el ranking persiste.

**Veredicto: ✅ CUMPLIDA.** Las dos partes están: datos persistentes (ranking en
JSON, probado el 2026-08-27) y sonido (los 3 clips asignados y sonando, 2026-09-09).

---

## 17 de septiembre (inclusive) — 1 punto

| Ítem | Estado |
|---|---|
| Efectos: partículas | ❌ No hay ningún `ParticleSystem` en la escena ni en prefabs. |
| Efectos: interacción con hardware externo | 🟡 **Programado el 2026-09-09, falta probarlo con la cámara real.** El juego se controla con la **webcam**: `vision/deteccion.py` (MediaPipe) detecta los gestos del jugador y se los manda a Unity por UDP (`EntradaCamara.cs`). Reemplaza al manubrio de Arduino, que salía caro. |
| Efectos de cámara (Cinemachine, etc.) | 🟡 A medias. No hay Cinemachine, pero desde el 2026-09-09 la cámara es de **primera persona** por código (`CamaraJugador.cs` en la `Main Camera`): sigue pegada al `PuntoCamara` de la moto, se tumba (roll) al esquivar y sube la vista al cielo mientras se sostiene el wheelie. Falta shake de choque / post-proceso reactivo. |
| Iluminación implementada | ⚠️ Hay una `Directional Light` default y un `Global Volume` con post-proceso, pero no iluminación pensada como parte del arte/efectos. |

**Veredicto: ❌ PENDIENTE.**

---

## 28 de septiembre (inclusive) — 1 punto — Versión final + build

| Ítem | Estado |
|---|---|
| Versión final sin errores, UI linda y coherente con el arte | 🟡 En camino: la moto y los autos son modelos 3D con rig (glTFast), la cámara es 1ª persona y el **menú principal ya está con arte** (dirección "Ruta de noche": fondo nocturno, sol retro, grilla de neón, Chakra Petch). Faltan con estilo el HUD, Opciones, la pausa y el Game Over, más el arte del camino (sigue siendo un cubo gris) y las texturas. El camino ya no tiene huecos. |
| Audio y efectos funcionando | ❌ Depende de las entregas del 10 y 17 de septiembre. |
| Proyecto buildeado y ejecutable sin errores | ❌ No hay ningún build. |
| Carpeta en Drive compartida + link en el `README.md` | ❌ El `README.md` no tiene ningún link a Drive. |

**Veredicto: ❌ PENDIENTE.**

---

## 05 de noviembre (inclusive) — 2 puntos

| Ítem | Puntos | Estado |
|---|---|---|
| Publicación en itch.io | 0.5 | ❌ Pendiente. |
| Video de promoción en YouTube | 0.5 | ❌ Pendiente. |
| Decoración del ambiente (folletería, decoración relacionada al juego) | 1 | ❌ Pendiente. |

**Veredicto: ❌ PENDIENTE.**

---

## Qué hacer ahora (prioridad)

1. **Cerrar la entrega del 6 de agosto:** agregar el **boceto del juego** al
   documento (falta solo eso). Volcar también al punto 1.6 del documento el texto
   de "1 jugador" y del ranking (comparación de puntajes de mayor a menor).
2. **17 de septiembre** — partículas, efectos de cámara e iluminación. Acá entra
   también el control por **cámara web** (detección de pose con Python +
   MediaPipe), que cubre el ítem "interacción con hardware externo" que hoy
   figura como no aplicable.
3. **28 de septiembre** — build ejecutable, carpeta de Drive y link en el README.
