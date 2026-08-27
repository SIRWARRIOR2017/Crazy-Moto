# Cronograma de entregas — Crazy Moto

Calendario de entregas de la feria de ciencias, con el estado de cada una según
lo que hay **hoy en el proyecto**.

- Fecha de análisis: **2026-08-27** (última actualización: 2026-08-27).
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
| 27 ago | 1 | ✅ Cumplida (vence hoy) | Controladores programados y pusheados hoy. |
| 03 sep | 1 | ❌ Pendiente | Falta decidir si hay salto real y programar el generador de obstáculos. |
| 10 sep | 1 | ❌ Pendiente | Sonido a medias (solo choque); sin datos persistentes con ScriptableObject. |
| 17 sep | 1 | ❌ Pendiente | Sin partículas, sin Cinemachine, iluminación solo la default. |
| 28 sep | 1 | ❌ Pendiente | Sin build, sin carpeta de Drive, sin link en el README. |
| 05 nov | 2 | ❌ Pendiente | Sin itch.io, sin video, sin decoración. |

**Puntos evaluables hasta hoy (4):** 3 cumplidos (13, 20 y 27 ago); el del 6 ago
queda a un paso (falta solo el boceto).

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

**Nota:** `AudioManager` funciona pero solo tiene asignado el sonido de choque
(`Crash.mp3`); `musicaFondo` y `sonidoClick` están vacíos. Para "controlador
programado" alcanza; el contenido de audio se termina de completar para el
10 de septiembre.

---

## 03 de septiembre (inclusive) — 1 punto — Mecánica principal

Pedido: mecánica principal programada y funcionando (movimientos, interacciones
físicas como colisiones o detecciones).

| Parte | Estado |
|---|---|
| Movimiento lateral | ✅ `PlayerController.MoverLateral()` con `Input.GetAxis("Horizontal")` y clamp. |
| Avance automático | ✅ `PlayerController.Avanzar()` según `velocidadActual`. |
| Wheelie | ⚠️ **Solo visual.** Inclina la moto y la cámara, pero **no hay salto** (la posición Y nunca cambia). El documento describe el loop como "saltar el obstáculo controlando la inclinación en el aire". |
| Colisión / detección | ✅ `OnTriggerEnter` con tag `Obstaculo`. |
| Obstáculos consecutivos y progresivos | ❌ **No existe.** Hay un solo obstáculo fijo; una vez pasado, no se puede volver a perder. |

**Veredicto: ❌ PENDIENTE.** La mecánica central tal como está definida (wheelie
+ superar obstáculos consecutivos cada vez más difíciles) **no está completa**.

**Para cerrarla, en orden:**
1. **Decisión de diseño:** ¿el wheelie incluye salto vertical real o el juego se
   queda con esquive lateral? (Bloquea el resto.)
2. Si hay salto: programar el salto y el control de inclinación en el aire.
3. Generador de obstáculos (tipo `RoadManager` pero para obstáculos): spawn a lo
   largo del camino, variedad y dificultad que sube con el tiempo/velocidad.
4. Sacar el `CrearObstaculoDePrueba` hardcodeado de `GameManager`.

---

## 10 de septiembre (inclusive) — 1 punto

### a) Sonido implementado y programado

- ⚠️ **A medias.** `AudioManager` está programado y el choque suena
  (`Assets/Sounds/Crash.mp3`).
- ❌ Falta: música de fondo (`musicaFondo`) y sonido de UI (`sonidoClick`) — no
  hay archivos de audio para esos campos.
- ❌ Faltan sonidos de la mecánica (motor, wheelie, salto si se agrega).

### b) Datos persistentes (ScriptableObject o base de datos)

- ❌ **No existe.** Lo único que se persiste es `PlayerPrefs("Volumen")`, que no
  es un ScriptableObject ni guarda datos del juego (puntaje, ranking, config).
- Para cerrarla hay que decidir **qué se persiste** (mejor puntaje, ranking,
  config) y hacerlo con un ScriptableObject o un archivo/BD.

**Veredicto: ❌ PENDIENTE** (las dos partes).

---

## 17 de septiembre (inclusive) — 1 punto

| Ítem | Estado |
|---|---|
| Efectos: partículas | ❌ No hay ningún `ParticleSystem` en la escena ni en prefabs. |
| Efectos: interacción con hardware externo | ❌ No aplica / no hecho (el documento dice que no se conecta con electrónica). |
| Efectos de cámara (Cinemachine, etc.) | ❌ No hay Cinemachine instalado. La cámara sigue al jugador solo por jerarquía (es hija de `PivotCamara`). |
| Iluminación implementada | ⚠️ Hay una `Directional Light` default y un `Global Volume` con post-proceso, pero no iluminación pensada como parte del arte/efectos. |

**Veredicto: ❌ PENDIENTE.**

---

## 28 de septiembre (inclusive) — 1 punto — Versión final + build

| Ítem | Estado |
|---|---|
| Versión final sin errores, UI linda y coherente con el arte | ❌ Lejos: falta arte, HUD, y arreglar el camino con huecos. |
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
2. **Decisión de diseño del salto** (bloquea la entrega del 3 de septiembre):
   ¿el wheelie incluye salto vertical real o el juego se queda con esquive
   lateral?
3. Programar la mecánica principal (salto si aplica + generador de obstáculos)
   antes del 3 de septiembre.
