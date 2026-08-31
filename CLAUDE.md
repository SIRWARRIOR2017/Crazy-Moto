# CLAUDE.md — Contexto del proyecto Crazy Moto

Este archivo es mi memoria de arranque de sesión. Si no me acuerdo de nada de una
sesión anterior, esto es lo que necesito saber.

## Qué es esto

"Crazy Moto" es un juego tipo endless-runner en 3D hecho en Unity (URP), proyecto
de **feria de ciencias** de dos alumnos (Joaquín Pastorino y Santiago Castiñeira).
No es un producto comercial: priorizar que el juego sea jugable y cumpla el
checklist de la feria por sobre cualquier arquitectura "prolija".

Hay dos documentos de requisitos en `docs/` (`docs/PROYECTO FERIA DE CIENCIAS.docx`
y `docs/documento.docx`) que definen dos cosas distintas:

- **`documento.docx`** ("Plan de trabajo"): el alcance de la **primera entrega**
  (20 de agosto): menú, jugar, movimiento lateral + wheelie, una condición de
  derrota por obstáculo, pantalla de Game Over con botón Continuar. Este alcance
  ya está prácticamente implementado.
- **`PROYECTO FERIA DE CIENCIAS.docx`**: la visión completa del juego para la
  feria. Habla de "saltar obstáculos controlando la inclinación en el aire",
  puntaje, y comparación contra un ranking de otros jugadores (dice que el juego
  "es competitivo"). Con las decisiones de diseño ya tomadas, ese loop se
  concretó así: **no hay salto** — se esquivan autos que vienen de frente por
  3 carriles y el wheelie (sin cambio de Y) es lo que da puntaje. Ya están el
  movimiento, la generación de obstáculos y el puntaje en pantalla; **faltan**
  la persistencia del puntaje / ranking, el sonido de fondo, las partículas, las
  luces y los efectos de cámara. Ver `docs/AUDITORIA.md` y `docs/ENTREGAS.md`.

## Arquitectura real

Todo vive en una sola escena (`Assets/Scenes/SampleScene.unity`) que se recarga
completa (`SceneManager.LoadScene`) para "reiniciar" el juego — no hay reseteo manual
de estado.

- **`GameManager.cs`** — Singleton (`GameManager.Instance`, sin `DontDestroyOnLoad`,
  se reinicia solo porque la escena entera se recarga). Lleva `velocidadActual`
  (acelera con el tiempo hasta `velocidadMaxima`), `juegoTerminado` y `puntaje`
  (int). El puntaje **solo sube mientras el jugador sostiene el wheelie**
  (`puntosPorSegundoEnWheelie`). En `Start()` crea por código, si no existen, un
  `TrafficManager` y un `Hud` (misma idea que el resto: la escena se recarga
  entera, así que no hace falta que estén en la jerarquía a mano). `GameOver()`
  pone `Time.timeScale = 0`, dispara `AudioManager.ReproducirChoque()` y busca un
  `MenuManager` en la escena (`FindAnyObjectByType`) para mostrar el panel de Game
  Over. Además, en `GameOver()` guarda el puntaje en el ranking:
  `RankingData.Instance.Agregar(nombre, puntaje)` con el nombre de
  `PlayerPrefs("NombreJugador")`.
- **`PlayerController.cs`** — Vive en el GameObject "Jugador" (tag `Player`). Cada
  `Update()` avanza el transform en Z según `GameManager.Instance.velocidadActual`,
  mueve lateralmente con `Input.GetAxisRaw("Horizontal")` (A-D) clampeado a
  `limiteLateral` (en la escena está en ±4; los 3 carriles son −3 / 0 / +3 pero
  el movimiento es libre entre ellos y un poco más allá), e inclina dos transforms (`cuerpo` y `pivotCamara`) al mantener
  **Shift** = wheelie. El wheelie **no es salto** (no hay cambio de Y): es una
  inclinación visual que además (a) hace que el jugador se mueva mucho más lento
  de costado (`factorLateralEnWheelie`) y (b) es la única forma de sumar puntaje.
  Todo el input crudo se lee solo en `LeerLateral()` y `LeerWheelie()` (para
  cambiarlo por el manubrio de Arduino a futuro sin tocar la lógica). Detecta
  derrota con `OnTriggerEnter` chequeando `tag == "Obstaculo"`. La cámara es hija
  de `pivotCamara`, que es hijo del jugador — el seguimiento es gratis por
  jerarquía, no hay script de cámara.
- **`TrafficManager.cs`** — Se crea en runtime desde `GameManager`. Autos (cubos
  rojos creados con `CreatePrimitive`, tag `Obstaculo`, sin trigger) que vienen de
  frente ("autopista en contramano") por los 3 carriles. Pool circular igual que
  `RoadManager`. Genera "filas" de **1 o 2 autos, nunca los 3**, así siempre queda
  un carril libre para esquivar. La distancia entre filas baja de
  `distanciaEntreFilasInicial` a `...Minima` a medida que sube `velocidadActual`.
  Todos los valores son ajustables en el Inspector del GameObject que crea en
  runtime.
- **`Hud.cs`** — Se crea en runtime desde `GameManager`. Arma por código dos
  textos TMP: "Puntaje: N" como hijo de `MenuManager.panelJuego` y
  "Puntaje final: N" como hijo de `MenuManager.panelGameOver`, así aparecen y
  desaparecen con esos paneles.
- **`RankingData.cs`** — Datos persistentes (entrega del 10 de septiembre). Es un
  `ScriptableObject` pero **no hay un `.asset`**: se usa vía `RankingData.Instance`
  (un `static` con `CreateInstance`, sobrevive a la recarga de escena). La lista
  de puntajes se guarda/lee de `ranking.json` en `Application.persistentDataPath`
  (un ScriptableObject por sí solo no persiste cambios de runtime en el build).
  `Agregar(nombre, puntaje)` = **una fila por jugador**: si el nombre ya está
  (sin distinguir mayúsculas) se queda con su mejor puntaje. `PuestoDe(nombre)`
  da el puesto (1 = primero) o −1. Ordena de mayor a menor. `Deduplicar()` limpia
  nombres repetidos de datos viejos al cargar. `BorrarTodo()` vacía todo.
- **`MenuRanking.cs`** — Se crea en runtime desde `MenuManager`. Arma por código,
  dentro de `MenuManager.panelMenu`, la tabla de **Top 10** (más una línea "Estás
  en el puesto N" si el jugador quedó afuera del top 10). El campo "Tu nombre" y
  el botón "Reiniciar tabla" **ya no están acá**: se movieron a `MenuOpciones`,
  que llama a `MenuRanking.Refrescar()` cuando cambian.
- **`MenuOpciones.cs`** — Se crea en runtime desde `MenuManager`. Arma por código
  el `PanelOpciones` (hermano de los otros paneles, hijo del `Canvas`) y lo
  registra en `MenuManager.panelOpciones`. Contenido en secciones apiladas con
  `VerticalLayoutGroup`: **AUDIO** (3 sliders: "Volumen general" →
  `AudioListener.volume`, "Volumen música" → `AudioManager.musica.volume`,
  "Volumen efectos" → `AudioManager.efectos.volume`), **PERFIL** (campo "Tu nombre
  (para el ranking)" → `PlayerPrefs("NombreJugador")`), **DATOS** (botón "Reiniciar
  tabla de ranking" → `RankingData.Instance.BorrarTodo()`), **CONTROLES** (texto
  informativo "Mover: A / D · Wheelie: Shift", no editable hasta el Arduino), y un
  botón "Volver". Además, en `Start()` agrega el **botón "Opciones"** al menú
  principal: clona `BotonJugar` (para heredar el estilo), le cambia el texto y el
  `OnClick` (evento nuevo → `MenuManager.MostrarOpciones()`, descartando el
  listener heredado de "Jugar"), lo ubica entre Jugar y Salir y reacomoda los 3.
- **`RoadManager.cs`** — Pool circular de tramos de camino (prefab
  `Assets/Prefabs/Tramo.prefab`, un cubo en escala `{10, 1, 30}` — mide exactamente
  `largoTramo` de largo, así los tramos encajan sin huecos; sin `MeshCollider`, el
  jugador nunca colisiona con el camino). El prefab tiene 12 hijos `LineaCarril`
  (cubos finos con `Assets/Materials/LineaCarril.mat`, URP Unlit blanco): las
  líneas discontinuas que separan los 3 carriles, en X = ±1.5, período 5 en Z
  (segmento de 2 + hueco de 3) que divide justo los 30 del tramo para que tilen
  sin cortes en las uniones:
  crea `cantidadTramos` al `Start()`, y en `Update()` cuando el tramo más viejo
  queda a más de `largoTramo` detrás del jugador, lo reubica adelante de todo
  (`Queue` implementada a mano con `List<Transform>`).
- **`MenuManager.cs`** — Controla 4 paneles (menú / juego / game over con
  `SetActive`, y `panelOpciones` que se arma por código — ver `MenuOpciones`).
  Usa un `static bool irAJugar` en memoria (no `PlayerPrefs`) para saber, al
  recargar la escena, si debe arrancar jugando directo o mostrar el menú. Los
  botones que ya estaban en la escena (Jugar/Salir/Continuar/Menú) llaman a sus
  métodos públicos vía `OnClick` del Inspector; los botones "Opciones", "Volver"
  y "Reiniciar tabla" los cablea `MenuOpciones` por código. `MostrarOpciones()` /
  `VolverDeOpciones()` alternan menú ↔ opciones. En `Start()` crea el
  `MenuRanking` y el `MenuOpciones` si no existen.
- **`AudioManager.cs`** — Singleton (`AudioManager.Instance`, mismo patrón que
  `GameManager`, se recrea con cada recarga de escena). Tiene un `AudioSource`
  para música (loop) y otro para efectos (`PlayOneShot`); si no se asignan a
  mano en el Inspector, se crean solos en `Awake()`. **No reproduce nada al
  arrancar**: la música es solo del menú, la dispara `MenuManager.MostrarMenu()`
  con `ReproducirMusicaMenu()` (y `EmpezarJuego()` llama a `DetenerMusica()`).
  Maneja **3 volúmenes** que se ajustan en Opciones y se guardan en `PlayerPrefs`
  (`Volumen` → `AudioListener.volume`, `VolumenMusica` → `musica.volume`,
  `VolumenEfectos` → `efectos.volume`): los aplica en `Awake()` y los cambia con
  `SetVolumenGeneral/Musica/Efectos` (las claves están como constantes públicas
  `AudioManager.ClaveVolumen*`). Expone además `ReproducirChoque()` (enganchado
  desde `GameManager.GameOver()`) y `ReproducirClick()` (todavía sin usar). Los
  campos de `AudioClip` (`musicaFondo`, `sonidoChoque`, `sonidoClick`) están
  vacíos salvo `sonidoChoque` (`Crash.mp3`): faltan los otros archivos, hay que
  arrastrarlos en el Inspector cuando existan. (El viejo `VolumenSlider.cs` se
  eliminó: su función pasó a `MenuOpciones` + `AudioManager`.)

Comunicación entre scripts: casi todo pasa por `GameManager.Instance` (acceso
directo al singleton) o por referencias asignadas a mano en el Inspector
(`RoadManager.jugador`, `PlayerController.cuerpo/pivotCamara`,
`MenuManager.panelMenu/panelJuego/panelGameOver`). No hay eventos ni interfaces.

## Convenciones existentes (no inventadas, observadas en el código)

- Nombres de variables, métodos y comentarios **en español**.
- Campos públicos con `[Header("...")]` para agrupar en el Inspector; lo que es
  ajustable pero no debería tocarse desde otro script va como
  `[SerializeField] private`.
- Estado que otros scripts necesitan leer pero no escribir se expone como
  auto-propiedad con setter privado (`public bool juegoTerminado { get; private set; }`).
- Sin namespaces. Todos los scripts del proyecto viven en `Assets/Scripts/` (sin
  subcarpetas por feature); los prefabs en `Assets/Prefabs/`; el asset de Input
  (`InputSystem_Actions.inputactions`) y los perfiles de URP en `Assets/Settings/`.
  Materiales propios (cuando haya) van en `Assets/Materials/`.
- Comentarios mínimos, solo cuando algo no es obvio (ver el comentario sobre el
  obstáculo de prueba en `GameManager.cs`).
- Reinicio de partida = recargar la escena, nunca resetear campos a mano.
- `PlayerPrefs` se usa solo para preferencias que deben sobrevivir a cerrar y
  volver a abrir el juego (`"Volumen"`, `"VolumenMusica"`, `"VolumenEfectos"`,
  `"NombreJugador"`), nunca para flags de un
  solo uso entre una recarga de escena y la siguiente (eso se resuelve con un
  `static` en memoria — ver el bug que se corrigió en `MenuManager`, sección de
  decisiones). Los datos de juego (el ranking) van en JSON, no en `PlayerPrefs`.

## Qué NO tocar

- `Assets/TextMesh Pro/**` — paquete importado, no es código del proyecto.
- `Assets/Settings/**` (perfiles de URP) salvo que la tarea sea explícitamente de
  gráficos/rendering. Excepción: `Assets/Settings/InputSystem_Actions.inputactions`
  se movió acá en la reorganización y sí es un asset del proyecto.
- Cualquier `.meta` — los administra Unity, nunca a mano.
- `Packages/manifest.json` — no agregar/quitar dependencias sin que se pida.
- `ProjectSettings/**` — no tocar salvo que la tarea lo requiera explícitamente
  (y avisando, igual que con `.unity`/`.prefab`).

## Estado actual (resumen — detalle completo en docs/AUDITORIA.md)

**Funciona:** loop completo — en el menú ponés tu nombre → jugar → esquivar autos
que vienen de frente por 3 carriles (A-D, movimiento libre) → sostener wheelie
(Shift) para sumar puntaje a costa de casi no poder esquivar → chocar un auto →
Game Over con puntaje final → Continuar/Menú, todo recargando la escena. El
puntaje se guarda en un ranking persistente (`ranking.json`) que se ve en el
menú: Top 10 + "estás en el puesto N" + botón "Reiniciar tabla".

**A medias:** UI escalable configurada (CanvasScaler con "Scale With Screen
Size"); el HUD de puntaje, la tabla de ranking y todo el panel de Opciones están
armados por código (`Hud.cs`, `MenuRanking.cs`, `MenuOpciones.cs`), en greybox
sin estilo. El menú tiene 3 botones (Jugar / Opciones / Salir) y Opciones agrupa
en secciones los 3 volúmenes, el nombre, el reinicio del ranking y la ayuda de
controles. Los autos son cubos rojos creados en runtime (`TrafficManager.cs`),
sin arte. El camino ya no tiene huecos (el prefab `Tramo` se reescaló a
`{10, 1, 30}`) y tiene líneas de carril discontinuas, pero sigue siendo un cubo
gris sin textura ni arte.

**No existe todavía:** música de menú, sonido de click, ruidos de la partida
(motor de la moto, autos pasando) — solo suena el choque; partículas, luces de
efecto, efectos de cámara. **Decidido que NO habrá salto** (la mecánica vertical
es el wheelie) y que la **música va solo en el menú**, no durante la partida
(ver "Decisiones tomadas").

## Reglas de trabajo (del usuario, permanentes)

- Responder siempre en español.
- Código completo y listo para pegar, nunca fragmentos ni diffs.
- Un objetivo por vez — después de cada cambio, el usuario prueba en Unity antes
  de seguir.
- Si toco un `.unity` o `.prefab`, avisar explícitamente antes/después porque es
  YAML frágil y el usuario lo quiere revisar en el Editor.
- Arreglos chicos, evidentes y de bajo riesgo (bug claro, collider de más,
  convención de nombres rota): aplicarlos directo y después contar qué y por qué.
- Cambios de diseño, features nuevas, o cualquier cosa que toque escena/prefabs:
  proponer primero y esperar el OK.
- Si no está claro en cuál de las dos categorías cae algo: preguntar.
- Mantener este archivo actualizado en el momento en que algo lo desactualice
  (feature nueva, bug arreglado, decisión de diseño, archivo movido) — no esperar
  a que lo pidan.
- No proponer arquitectura empresarial, interfaces genéricas ni event buses: es
  un proyecto escolar de dos personas, la prioridad es que funcione y cumpla el
  checklist de la feria.

## Decisiones tomadas

- **2026-08-26** — Pase de robustez sin tocar escena/prefabs: `RoadManager` y
  `PlayerController` ahora validan referencias nulas del Inspector antes de
  usarlas (avisan por consola en vez de tirar `NullReferenceException`);
  `RoadManager` usa `while` en vez de `if` para el pooling de tramos; y
  `MenuManager` reemplazó el flag `PlayerPrefs("IrAJugar")` por un `static bool`
  en memoria, para que un cierre inesperado del juego nunca lo deje arrancando
  en partida la próxima vez que se abra.
- **2026-08-26** — Controlador de sonido: se agregó `AudioManager.cs` (sin
  archivos de audio reales todavía, los campos de `AudioClip` quedan vacíos a
  propósito hasta que se consigan) y se lo enganchó desde
  `GameManager.GameOver()`. Se decidió explícitamente **no** enganchar sonido
  de click en los botones de menú (Jugar/Continuar/Menú) porque todos recargan
  la escena y el sonido se cortaría antes de terminar de sonar — el método
  `ReproducirClick()` queda listo para un futuro panel que no recargue escena
  (ej. una pausa).
- **2026-08-26** — Slider de volumen: se agregó `VolumenSlider.cs` y, a pedido
  del usuario, esta vez armé yo mismo el `Slider` de UI directamente en el
  `.unity` (dentro de `PanelMenu`, debajo del botón Salir), con la jerarquía
  estándar (Background / Fill Area+Fill / Handle Slide Area+Handle) y el
  evento `On Value Changed` ya conectado a `CambiarVolumen` (modo "Dynamic
  float"). Los GUID de los scripts de Unity (`Slider`) los saqué del propio
  `Library/PackageCache` del proyecto en vez de confiar en memoria, para no
  arriesgar un "Missing Script". Controla `AudioListener.volume` directamente
  (no pasa por `AudioManager`) y persiste la preferencia en
  `PlayerPrefs("Volumen")` — a diferencia del bug de `"IrAJugar"`, este SÍ es
  un uso correcto de `PlayerPrefs` porque es una preferencia que debe
  sobrevivir entre sesiones.
- **2026-08-26** — El usuario agregó a mano el GameObject `AudioManager` en la
  escena (siguiendo los pasos que le pasé) y ya le asignó un clip real:
  `Assets/Sounds/Crash.mp3` en el campo `sonidoChoque`. El choque contra el
  obstáculo ya suena.
- **2026-08-27** — Reorganización del repo para que se vea ordenado en GitHub
  (Unity cerrado durante la operación). (1) Los 6 scripts pasaron de la raíz de
  `Assets/` a `Assets/Scripts/`; `Tramo.prefab` a `Assets/Prefabs/`;
  `InputSystem_Actions.inputactions` a `Assets/Settings/`. Cada archivo se movió
  con su `.meta` al lado (`git mv`), así los GUID no cambian y ni la escena ni el
  prefab pierden referencias. (2) Se borró la plantilla URP que no usa el juego:
  `Assets/TutorialInfo/**` y `Assets/Readme.asset` (12 archivos, no los
  referenciaba nada). (3) Se borró `Assets/Gris.mat` (material suelto sin ningún
  uso). (4) Se borró `My project.slnx` (solución autogenerada y obsoleta) y se
  agregó `*.slnx` al `.gitignore`, junto con `.idea/` y `.vscode/`
  (`.vscode/` además se dejó de trackear). (5) `docs/` nueva: se movieron ahí
  `AUDITORIA.md` y los dos `.docx` de requisitos. Todo en un solo commit en la
  rama `reorg/estructura-y-gitignore`.
- **2026-08-27** — Se creó `docs/ENTREGAS.md`: el cronograma completo de entregas
  de la feria (fechas, puntaje y estado de cada una). Mantenerlo actualizado cada
  vez que se cierra o cambia el estado de una entrega. Datos confirmados por el
  alumno ese día: (a) la fecha que cuenta es la **del documento de cada entrega**,
  no la del commit, así que las entregas del 6 y 13 de agosto están dentro de
  plazo; (b) el juego es de **1 jugador**, no multiplayer; (c) el **ranking** es
  una comparación de jugadores por puntaje, ordenada de mayor a menor (el más alto
  primero). Estado: 13, 20 y 27 de agosto cumplidas; la del 6 de agosto queda a
  falta solo del boceto del juego. Próxima con trabajo de código: 3 de septiembre
  (mecánica principal), que necesita la decisión de diseño del salto.
- **2026-08-27** — Mecánica principal (entrega del 3 de septiembre), decisiones
  del alumno: el juego es "una autopista en contramano", vienen autos de frente
  por 3 carriles (X = −3 / 0 / +3) y se esquivan **solo** con movimiento lateral
  libre A-D. **No hay salto**: la moto solo hace wheelie (tecla Shift). Mientras
  se sostiene el wheelie, el jugador se mueve mucho más lento de costado (riesgo)
  y **solo así se suma puntaje** (avanzar sin wheelie no da puntos). Se implementó
  todo por código sin tocar la escena: `PlayerController` reescrito, `GameManager`
  sin el `CrearObstaculoDePrueba` y con `puntaje`, y dos scripts nuevos
  (`TrafficManager`, `Hud`) que `GameManager` instancia en runtime. El alumno lo
  probó en Unity y funciona. Pendiente de pasar a arte/escena real: los autos
  (hoy cubos de `CreatePrimitive`) y el HUD (hoy texto TMP armado por código).
- **2026-08-27** — Datos persistentes (entrega del 10 de septiembre), decisiones
  del alumno: identificación por **nombre puesto una vez en el menú** (se guarda
  en `PlayerPrefs`); el ranking se ve **solo en el menú** (Top 10 + "estás en el
  puesto N" si quedás afuera); persistencia con **ScriptableObject + archivo
  JSON**. Implementado por código sin tocar la escena: `RankingData.cs`
  (ScriptableObject, sin `.asset`, backing JSON en `persistentDataPath`) y
  `MenuRanking.cs` (campo de nombre + tabla + botón "Reiniciar tabla", todo UI
  armada por código dentro de `PanelMenu`). `Agregar` mantiene una fila por
  jugador (se queda el mejor puntaje). Se descartó el `.asset` hecho a mano
  porque Unity lo importaba antes de compilar el script y quedaba "missing
  script". El alumno lo probó y funciona.
- **2026-08-31** — Puente Unity MCP configurado. El alumno habilitó en Project
  Settings → AI: el Unity MCP Server (bridge "Running", las 54 tools habilitadas)
  y el Gateway con provider "Claude Code". El archivo `.mcp.json` en la raíz del
  repo apunta al relay (`~/.unity/relay/relay_win.exe --mcp`). A partir de acá
  puedo leer y editar escena, prefabs, assets y consola de Unity directamente
  desde la sesión, sin que el alumno tenga que pegar código a mano. Las carpetas
  `Packages/com.unity.ai.assistant/` y `ProjectSettings/Packages/` aparecieron
  con esto. Se descartó Figma (el alumno prefiere pulir el proyecto antes de
  meter arte/modelos).
- **2026-08-31** — Se arregló el camino con huecos (bug 1.1 de `docs/AUDITORIA.md`).
  El prefab `Assets/Prefabs/Tramo.prefab` pasó de escala `{1.2, 1, 3}` a
  `{10, 1, 30}`: ahora cada tramo mide exactamente `largoTramo` (30), así que el
  pooling de `RoadManager` los coloca uno tras otro sin vacíos, y el ancho (10)
  cubre los 3 carriles. Se le quitó el `MeshCollider` (no cumplía ninguna función,
  bug 1.8) y se le puso la posición local en `{0,0,0}` (tenía un offset heredado).
  Editado en el `.prefab` YAML y reimportado por MCP; sin errores de consola.
  Queda pendiente que el alumno lo pruebe en Play y revise la altura del piso.
- **2026-08-31** — Líneas de carril. El alumno pidió líneas que separen los
  carriles y eligió **discontinuas tipo autopista**. Se agregaron al prefab
  `Tramo` 12 hijos `LineaCarril` (6 en X=−1.5 y 6 en X=+1.5), cubos finos
  (~0.15 × 0.06 × 2 en mundo) con material nuevo `Assets/Materials/LineaCarril.mat`
  (URP Unlit blanco, primer material propio del proyecto → se creó la carpeta
  `Assets/Materials/`). El período en Z es 5 (segmento 2 + hueco 3) y divide
  exacto los 30 del tramo, así las líneas no se cortan en las uniones entre
  tramos. Hecho con `Unity_RunCommand` (script que abre el prefab con
  `PrefabUtility.LoadPrefabContents`, agrega los hijos y guarda); el script borra
  y regenera los `LineaCarril` si se vuelve a correr. Verificado en Play: las
  líneas renderizan.
- **2026-08-31** — Decisión de diseño (audio): la **música va solo en el menú**.
  Durante la partida no hay música — solo se van a escuchar los sonidos "reales"
  de la moto (motor en loop) y de los autos pasando. **Implementado:** `AudioManager`
  ya no reproduce nada en `Start()` (se le sacó ese método); tiene
  `ReproducirMusicaMenu()` y `DetenerMusica()`. `MenuManager.MostrarMenu()` llama
  a `ReproducirMusicaMenu()` y `EmpezarJuego()` llama a `DetenerMusica()` por las
  dudas. Compila sin errores. Falta el archivo de música (campo `musicaFondo`
  sigue vacío) y los sonidos de moto/autos (ni archivos ni código todavía).
- **2026-08-31** — Menú de Opciones (pedido del alumno). Botón **"Opciones"** en
  el menú, entre Jugar y Salir, que abre un `PanelOpciones` con la config repartida
  en secciones, cada control con nombre descriptivo. Decisiones del alumno:
  volumen **separado en 3** (general / música / efectos), el campo "Tu nombre"
  **solo** en Opciones (ya no en el menú), y sección **"Controles"** informativa
  (no editable). Implementación: nuevo `MenuOpciones.cs` (arma el panel y el botón
  por código, patrón de `MenuRanking`); `AudioManager` maneja los 3 volúmenes;
  `MenuManager` gana `panelOpciones` + `MostrarOpciones()`/`VolverDeOpciones()`;
  `MenuRanking` perdió el campo de nombre y el botón "Reiniciar" (se fueron a
  Opciones); se eliminó `VolumenSlider.cs` y el objeto `SliderVolumen` de la
  escena. Cambios en la escena (`SampleScene.unity`, guardada por MCP): se quitó
  `SliderVolumen`, se reubicaron `BotonJugar` (y=120) y `BotonSalir` (y=−40); el
  botón "Opciones" NO está en la escena, lo agrega `MenuOpciones` en runtime (por
  eso en el Editor sin Play no se ve, igual que la tabla de ranking). Verificado
  en Play: el botón abre el panel, están los 3 sliders + campo de nombre + 2
  botones + textos de sección, sin errores de consola. Nota de MCP: `RunCommand`
  falla con "User interactions are not supported" si el script llama a
  `EditorSceneManager.SaveScene` o `AssetDatabase.DeleteAsset`; hay que guardar la
  escena con `Unity_ManageScene` (Action "Save") y borrar assets con
  `Unity_ManageAsset` (Action "Delete"), no desde `RunCommand`.

## Actividades futuras (tener en cuenta al programar, para no rehacer)

- **Manubrio con Arduino:** más adelante se va a armar un controlador físico
  (manubrio + botones) con Arduino para reemplazar el teclado y hacerlo más
  inmersivo. Por eso el input crudo del jugador está aislado en `LeerLateral()` y
  `LeerWheelie()` dentro de `PlayerController`: cuando llegue el Arduino se
  cambian esos dos métodos (leer del puerto serie) y nada más. No dispersar
  llamadas a `Input.*` por otros scripts.
- **Autos y UI a arte real:** cuando haya modelos/estilo, `TrafficManager` debe
  instanciar un prefab de auto en vez de `CreatePrimitive`, y toda la UI armada
  por código (`Hud.cs` en `PanelJuego`/`PanelGameOver`; `MenuRanking.cs` con la
  tabla de ranking; `MenuOpciones.cs` con el panel de Opciones entero y el botón
  "Opciones" del menú) debería pasar a ser objetos de UI en la escena.

## Pendiente de fecha (checklist de la feria)

Detalle completo y actualizado en `docs/ENTREGAS.md`. Resumen de lo que falta y
es decisión del alumno (no de código):

- El **boceto del juego** sigue sin hacerse (entrega del 6 de agosto). Multiplayer
  (1 jugador) y ranking (comparación de puntajes de mayor a menor) ya están
  definidos, pero conviene volcarlos al punto 1.6 de
  `docs/PROYECTO FERIA DE CIENCIAS.docx`, que hoy los tiene vacíos.
- `musicaFondo` y `sonidoClick` en `AudioManager` siguen vacíos (solo
  `sonidoChoque` tiene clip asignado). Hace falta para la entrega de sonido del
  10 de septiembre.
