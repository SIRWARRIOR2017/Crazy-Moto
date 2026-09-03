# AUDITORÍA — Crazy Moto

Fecha: 2026-08-26. Solo diagnóstico, **no se aplicó ningún cambio**.

> Nota (2026-08-27): este documento es una foto del 26/8 y quedó viejo en varios
> puntos. Tras la reorganización del repo vive en `docs/`, los scripts en
> `Assets/Scripts/` y el prefab en `Assets/Prefabs/Tramo.prefab`. Estado real:
> **1.3 resuelto por decisión de diseño** (no habrá salto; la mecánica vertical
> es el wheelie), **1.4 resuelto** (`TrafficManager`), **1.5 y 1.6 resueltos**
> (pase de robustez). Del checklist de la sección 2, ya están hechos el Audio
> Manager (`AudioManager.cs`, con sonido de choque) y los **datos persistentes**
> (`RankingData` = ScriptableObject + `ranking.json`). Para el estado al día ver
> `docs/ENTREGAS.md` y "Decisiones tomadas" en `CLAUDE.md`.
>
> Nota (2026-08-31): **1.1 resuelto** (`Tramo.prefab` reescalado a `{10, 1, 30}`,
> el camino ya no tiene huecos) y **1.8 resuelto** (se le sacó el `MeshCollider`).
>
> Nota (2026-09-03): **1.7 resuelto** al agregar la pausa. `MenuManager` ahora
> tiene `estaPausado`, la variable que faltaba para distinguir *por qué* el juego
> está en `Time.timeScale = 0` (menú / game over / pausa). Ver el nuevo
> `MenuPausa.cs` y la decisión del 2026-09-03 en `CLAUDE.md`.

---

## 1. Bugs y riesgos

Ordenados de más grave a menos grave.

### 1.1 — El camino visual tiene huecos enormes (ROMPE HOY, visual)

**Dónde:** `Assets/Prefabs/Tramo.prefab` (escala `{1.2, 1, 3}` sobre el cubo default de
Unity) vs `RoadManager.cs:9` (`largoTramo = 30`).

**Qué pasa:** cada segmento de camino mide 3 unidades de largo en Z, pero
`RoadManager` los coloca cada 30 unidades. Es decir, solo el 10% del camino tiene
geometría visible; el otro 90% es vacío. Al correr el juego se ve una sucesión de
bloques flotando con huecos gigantes entre ellos, no una carretera continua.

**Por qué importa:** es lo primero que se ve al abrir el juego. No rompe la
lógica (el jugador nunca choca contra el camino, solo contra el tag
`Obstaculo`), pero para una demo en la feria es un problema de presentación
grave. Casi seguro `Tramo` es un placeholder que quedó sin ajustar.

### 1.2 — `PlayerPrefs("IrAJugar")` puede dejar el juego "pegado" arrancando en partida (EDGE CASE — puede romper en la demo)

**Dónde:** `MenuManager.cs:15-19` y `MenuManager.cs:38-40`.

**Qué pasa:** `Jugar()` escribe `PlayerPrefs.SetInt("IrAJugar", 1)` **antes** de
llamar a `SceneManager.LoadScene(...)`. Si la aplicación se cierra a la fuerza
(Alt+F4, cuelgue, corte de luz en la feria) justo en esa ventana, la próxima vez
que se abra el juego (incluso en una sesión completamente nueva) va a saltar el
menú principal y arrancar jugando directo, porque `PlayerPrefs` persiste en
disco/registro entre ejecuciones, no solo en memoria.

**Por qué importa:** es poco probable, pero el peor momento para que pase es en
vivo frente al jurado. Es fácilmente evitable (ver plan de trabajo).

### 1.3 — Sin salto real: el "wheelie" es solo una rotación visual (REQUISITOS vs IMPLEMENTACIÓN, no es un bug de código)

**Dónde:** `PlayerController.cs:42-55` (`Wheelie()`).

**Qué pasa:** `Wheelie()` solo interpola el ángulo de `cuerpo` y `pivotCamara`.
La posición Y del jugador nunca cambia. El documento de la feria describe el
loop como "avanzar → **saltar** el obstáculo controlando la inclinación de la
moto en el aire", lo cual implica una mecánica de salto real que hoy no existe.

**Por qué importa:** no es un bug, es una brecha de diseño — la ubico acá porque
determina cómo tienen que ser los obstáculos futuros (ver sección 2 y el plan).

### 1.4 — Un solo obstáculo, nunca se genera otro (VA A ROMPER CUANDO agreguemos dificultad progresiva)

**Dónde:** `GameManager.cs:39-49` (`CrearObstaculoDePrueba`).

**Qué pasa:** se crea un único cubo rojo en `distanciaObstaculo = 50`, una sola
vez, en `Start()`. Una vez que el jugador lo esquiva o lo pasa, no hay forma de
perder nunca más — el juego acelera para siempre sin ningún riesgo.

**Por qué importa:** cumple el alcance de la primera entrega (una condición de
derrota), pero está lejísimos de "obstáculos cada vez más difíciles" que pide el
documento de la feria completa. El propio comentario en el código lo marca como
temporal ("Obstáculo temporal para comprobar el ciclo completo de la entrega").

### 1.5 — Sin verificación de nulls en referencias de Inspector (DEUDA TÉCNICA — funciona hoy, es frágil)

**Dónde:** `RoadManager.cs` (`jugador`, `prefabTramo`), `MenuManager.cs`
(`panelMenu`, `panelJuego`, `panelGameOver`), `PlayerController.cs` (`cuerpo`,
`pivotCamara`), y el acceso a `GameManager.Instance` en `PlayerController.cs:22`.

**Qué pasa:** ninguno de estos scripts valida que la referencia no sea `null`
antes de usarla. Hoy todas las referencias están bien asignadas en la escena (lo
verifiqué), así que no falla. Pero si alguien desconecta sin querer una
referencia en el Inspector (fácil que pase al mover objetos), el error va a ser
un `NullReferenceException` silencioso en tiempo de ejecución, sin ningún
mensaje que diga cuál referencia falta.

**Por qué importa:** con dos personas tocando la escena, es cuestión de tiempo
que se rompa una referencia por accidente. No es urgente arreglarlo ahora, pero
conviene antes de sumar más objetos a la escena.

### 1.6 — Pooling de `RoadManager` reubica un solo tramo por frame (DEUDA TÉCNICA — funciona hoy con los valores actuales)

**Dónde:** `RoadManager.cs:20-31` (`Update`), usa `if` en vez de `while`.

**Qué pasa:** si en un solo frame el jugador avanzara más de `largoTramo` (30
unidades) — por un pico de lag muy grande, o si en el futuro se sube mucho
`velocidadMaxima` o se baja `largoTramo` — el camino se quedaría con un hueco
por uno o dos frames hasta que el bucle lo compense. Con los valores actuales
(`velocidadMaxima = 60`, `largoTramo = 30`) haría falta un frame de más de 0.5
segundos para que pase, algo muy raro pero no imposible en una PC de feria bajo
carga.

**Por qué importa:** es una corrección de una línea (`if` → `while`) que hace el
sistema robusto ante cualquier valor futuro de velocidad/largo, sin cambiar el
comportamiento normal.

### 1.7 — `Time.timeScale` es un interruptor global compartido por menú y game-over (RESUELTO 2026-09-03)

**Resuelto:** al implementar la pausa se agregó `MenuManager.estaPausado`, que
identifica el tercer estado. Menú, game over y pausa ya no se pisan.


**Dónde:** `GameManager.cs:56` y `MenuManager.cs:29`.

**Qué pasa:** tanto "estamos en el menú" como "el juego terminó" se representan
con el mismo `Time.timeScale = 0`. Hoy no genera problema porque el flujo
siempre pasa por una recarga completa de escena entre un estado y otro. Pero el
día que se agregue una función de pausa durante la partida (tercer estado que
también necesita `timeScale = 0`), estos tres estados van a pisarse entre sí
porque no hay ninguna variable que diga *por qué* está pausado.

**Por qué importa:** no bloquea nada del alcance actual, pero conviene saberlo
antes de implementar pausa.

### 1.8 — `MeshCollider` de `Tramo` no cumple ninguna función (DEUDA TÉCNICA MENOR, funciona)

**Dónde:** `Assets/Prefabs/Tramo.prefab` (componente `MeshCollider`, `m_IsTrigger: 0`).

**Qué pasa:** el jugador tiene Y fija (`Rigidbody` kinemático, sin gravedad) y
solo reacciona a colisiones por tag vía `OnTriggerEnter`. El `MeshCollider` de
`Tramo` no está tageado como `Obstaculo`, así que nunca genera ningún evento ni
interacción física real. Es peso muerto (impacto de performance insignificante
con 5 tramos, pero conceptualmente confunde: parece que el camino "sostiene"
físicamente a la moto y no es así).

### 1.9 — Paquete de Input System instalado pero sin usar (RIESGO LATENTE, no rompe hoy)

**Dónde:** `Packages/manifest.json` (`com.unity.inputsystem`),
`ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 2` = "Both"),
`Assets/InputSystem_Actions.inputactions` (sin referenciar desde ningún script).

**Qué pasa:** todo el input real se hace con las APIs viejas
(`Input.GetAxis`, `Input.GetKey`). Como `activeInputHandler` está en "Both", hoy
convive sin problema. Pero si alguien cambia esa configuración a "Input System
Package (New)" únicamente (algo que Unity a veces sugiere hacer), **todas las
llamadas a `Input.GetAxis`/`Input.GetKey` van a tirar excepción** y el jugador
dejaría de moverse.

**Por qué importa:** no es un bug activo, es una trampa para el futuro si
alguien "limpia" la configuración del proyecto sin saber que el código depende
del sistema viejo.

---

## 2. Requisitos vs. implementación

Base: checklist técnico de `PROYECTO FERIA DE CIENCIAS.docx` (sección 2) +
alcance de `documento.docx` (primera entrega, 20 de agosto).

### Alcance de la primera entrega (`documento.docx`)

| Punto | Estado |
|---|---|
| Menú principal con botones Jugar y Salir | **Hecho.** `MenuManager` + escena, botones cableados. |
| Jugador con movimiento lateral y wheelie por teclado | **Hecho** (el wheelie es visual, no salto — ver 1.3, pero cumple lo pedido para esta entrega). |
| Una condición de derrota mediante un obstáculo | **Hecho**, con el obstáculo de prueba hardcodeado. |
| Pantalla de Game Over con botón Continuar | **Hecho.** |
| Prueba completa del ciclo antes de entregar | No puedo verificarlo yo (requiere correr el Editor), pero el cableado de escena es coherente y no encontré referencias rotas. |

Conclusión: **la primera entrega está funcionalmente completa** a nivel de
código y escena, salvo el problema visual del camino (1.1).

### Checklist técnico completo (feria)

| Ítem del checklist | Estado | Nota |
|---|---|---|
| Controlador de juego (Game Manager) | **Hecho** | `GameManager.cs` existe y cumple su rol básico (velocidad, estado de fin de juego). |
| Controlador de sonido (Audio Manager) | **Ausente** | No hay ningún `AudioSource` en la escena ni script de sonido. |
| Efectos: partículas | **Ausente** | Ningún `ParticleSystem` en la escena o prefabs. |
| Efectos: luces | **A medias** | Hay una luz direccional default y un Global Volume con perfil de post-proceso, pero no hay luces como *efecto* de jugabilidad (choque, wheelie, etc.). |
| Datos persistentes usando ScriptableObject | **Ausente** | Lo único persistente es un flag `PlayerPrefs` int (`IrAJugar`), que no es un ScriptableObject ni guarda datos de juego (puntaje, config). |
| UI responsive | **A medias** | El `Canvas` está bien configurado (`CanvasScaler` en modo "Scale With Screen Size", 1920x1080 de referencia), que es la base correcta. Pero no hay evidencia de que se haya probado en más de una resolución, y el HUD de juego (`PanelJuego`) no tiene ningún elemento dentro — está vacío. |
| Ejecutable sin errores visuales | **No verificable desde acá** | El hueco del camino (1.1) es un error visual conocido que hay que corregir antes de decir que sí. |
| Ejecutable sin bugs de código | **No verificable desde acá sin correrlo**, pero no encontré ninguna referencia rota ni excepción garantizada con el estado actual de la escena. Los riesgos de la sección 1 son edge cases, no fallas garantizadas. |
| Escena de inicio (menú principal) | **Hecho.** |

### Lo que pide la sección 1 del documento (diseño) y no está reflejado en el juego

- **Loop descripto** ("avanzar → saltar el obstáculo → sumar sosteniendo wheelie
  en el aire → obstáculos cada vez más difíciles → al chocar se suma el puntaje
  y se compara contra el ranking"): de esto, **solo "avanzar" y "chocar" están
  implementados**. No hay salto, no hay puntaje, no hay dificultad progresiva
  real (la velocidad sube sola, pero no hay más obstáculos), no hay ranking.
- **"Es competitivo" (1.6)**: no hay ningún sistema de puntaje ni comparación
  entre jugadores todavía. La respuesta del documento compromete a algo que el
  juego aún no tiene.
- **Recursos para la feria (1.5)**: es logístico, no depende del código.

---

## 3. Plan de trabajo (ordenado por dependencia)

No por importancia — por qué necesita qué para poder hacerse.

**1. Robustez base (sin tocar escena/prefabs).**
Agregar validaciones defensivas donde faltan (1.5) y cambiar el `if` por `while`
en el pooling de `RoadManager` (1.6).
*Tamaño: XS. Archivos: `RoadManager.cs`, `MenuManager.cs`, `PlayerController.cs`.*
Va primero porque todo lo que se construya después (más obstáculos, HUD) se
apoya en que estos sistemas no fallen en silencio.

**2. Arreglar el flag de `PlayerPrefs` (1.2).**
Limpiar o reordenar la escritura del flag para que un cierre inesperado no dañe
el arranque siguiente.
*Tamaño: XS. Archivo: `MenuManager.cs`.*
Independiente de lo demás, conviene resolverlo temprano porque es el tipo de
bug que se nota justo en la peor demo.

**3. Decisión de diseño: ¿hay salto o no? (1.3)**
Esto es una conversación, no código: definir si el wheelie va a incluir salto
vertical real (como describe el documento de la feria) o si el diseño final se
queda con esquive lateral únicamente y se ajusta el documento a lo que el juego
realmente hace.
Bloquea el punto 4: no tiene sentido diseñar el sistema de obstáculos sin saber
si tienen que ser "saltables" o solo "esquivables".

**4. Reemplazar el obstáculo de prueba por un generador real (1.4).**
Un sistema tipo `RoadManager` pero para obstáculos: spawn a lo largo del
camino, variedad, dificultad que crece con `velocidadActual` o con el tiempo.
*Tamaño: M/L. Archivos: nuevo script (p.ej. `ObstacleManager.cs`), ajuste en
`GameManager.cs` para sacar `CrearObstaculoDePrueba`, nuevo(s) prefab(s) de
obstáculo, cambios en la escena.* Toca escena y prefabs → requiere tu OK antes
de tocar nada.
Depende del punto 3.

**5. Arte real del camino (1.1).**
Ajustar `Tramo` (escala, o un mesh nuevo) para que cubra `largoTramo` sin
huecos.
*Tamaño: S (si alcanza con reescalar) a M (si hace falta modelar/texturizar
algo nuevo). Archivo: `Assets/Prefabs/Tramo.prefab`.* Toca un prefab → requiere tu OK.
No depende de nada técnico, se puede hacer en paralelo con el punto 4.

**6. Puntaje + HUD.**
Contador de puntaje (por distancia, tiempo, o wheelie sostenido, según lo que
se decida en el punto 3) y un elemento de UI en `PanelJuego` que lo muestre.
*Tamaño: M. Archivos: `GameManager.cs`, escena (agregar Texto TMP a
`PanelJuego`).* Toca escena → requiere tu OK.
Depende parcialmente del punto 4 (si el puntaje premia obstáculos esquivados) y
del punto 3 (si premia wheelie).

**7. Ranking / comparación entre jugadores.**
Definir mecanismo (¿ranking local con `PlayerPrefs`/archivo, ¿ingreso de
nombre?) y mostrarlo en algún panel.
*Tamaño: L.* Depende de que exista el puntaje (punto 6).

**8. Audio Manager + sonidos de juego.**
*Tamaño: M.* No depende técnicamente de nada anterior, pero conviene hacerlo
después de que el loop de mecánicas esté estable, para no tener que
resincronizar sonidos con mecánicas que todavía están cambiando.

**9. Partículas y luces de efecto (choque, wheelie, etc.).**
*Tamaño: M.* Depende de que las mecánicas visuales que van a acompañar (choque,
salto) estén definidas y no vayan a cambiar de nuevo.

**10. Datos persistentes vía ScriptableObject.**
Solo tiene sentido una vez que se sepa qué hay que persistir (config, ranking).
*Tamaño: S/M.* Depende del punto 7 si el ranking necesita guardarse así.

**11. Pulido final.**
Nombre del proyecto/compañía en `ProjectSettings` (hoy dice "My project" /
"DefaultCompany"), prueba completa del ciclo en la PC que se lleva a la feria,
revisión de "sin errores visuales / sin bugs de código" del checklist.
*Tamaño: S.* Va al final porque depende de que todo lo anterior esté cerrado.
