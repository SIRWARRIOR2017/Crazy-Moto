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

Todo vive en una sola escena (`Assets/Scenes/ESCENA 1.unity`) que se recarga
completa (`SceneManager.LoadScene`) para "reiniciar" el juego — no hay reseteo manual
de estado. (La escena se llamaba `SampleScene.unity` hasta el 2026-09-08; el
alumno la renombró desde Unity, así que conserva el mismo GUID
`99c9720ab356a0642a771bea13969a05` y las referencias del build no se rompieron.)

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
  **Shift** = wheelie. `cuerpo` (GameObject "Cuerpo") tiene como hijo
  `MotoModelo` — el wrapper con el modelo 3D de la moto (`DirtBike.glb`), que se
  inclina con el wheelie; antes ahí había un cubo llamado `Modelo` que se borró.
  `MotoModelo` también lleva el componente `MotoAnimada` (ruedas + manubrio).
  El wheelie **no es salto** (no hay cambio de Y): es una
  inclinación visual que además (a) hace que el jugador se mueva mucho más lento
  de costado (`factorLateralEnWheelie`) y (b) es la única forma de sumar puntaje.
  Todo el input crudo se lee solo en `LeerLateral()` y `LeerWheelie()` (para
  cambiarlo por el manubrio de Arduino a futuro sin tocar la lógica). Expone
  `EntradaLateral` (la entrada lateral cruda −1..1) para que la lea quien la
  necesite (ej. `MotoAnimada` para el manubrio) sin dispersar `Input.*`. Detecta
  derrota con `OnTriggerEnter` chequeando `tag == "Obstaculo"`. La cámara **ya no
  es hija de `pivotCamara`**: desde el 2026-09-09 es primera persona y la maneja
  `CamaraJugador.cs` (ver abajo). `pivotCamara` quedó sin hijos y sin uso real
  (el código que lo inclina en `AnimarWheelie` sigue ahí pero no hace nada
  visible); `PlayerController` todavía lo pide en el Inspector para no tirar el
  error de referencia nula.
- **`EntradaCamara.cs`** — Recibe el control por **cámara web** que manda
  `vision/deteccion.py` por UDP a `127.0.0.1:5005`. El paquete es una línea de
  texto `lateral;wheelie;detectado` (ej. `-0.350;0;1`). Lee el socket en un
  **hilo aparte** (`ReceiveFrom` bloquea); el hilo no toca la API de Unity, solo
  escribe campos `volatile` que `Update()` copia a las propiedades públicas.
  `Activa` = están llegando paquetes **y** la cámara ve a alguien; si deja de
  llegar algo por 0,5 s, se limpia todo y `PlayerController` vuelve solo al
  teclado. Es el **único objeto del proyecto con `DontDestroyOnLoad`**, y a
  propósito: es dueño de un recurso del sistema operativo (el puerto UDP) y
  reabrirlo en cada recarga de escena —que acá pasa en cada partida— puede fallar
  con "address already in use". Lo crea `GameManager.Start()` con
  `EntradaCamara.CrearSiNoExiste()`, que no hace nada si ya existe.
- **`TrafficManager.cs`** — Se crea en runtime desde `GameManager`. Autos con
  tag `Obstaculo` que vienen de frente ("autopista en contramano") por los 3
  carriles. Pool circular igual que `RoadManager`. Genera "filas" de **1 o 2
  autos, nunca los 3**, así siempre queda un carril libre para esquivar. La
  distancia entre filas baja de `distanciaEntreFilasInicial` a `...Minima` a
  medida que sube `velocidadActual`. Los modelos de auto se cargan con
  `Resources.LoadAll<GameObject>("Autos")` de `Assets/Resources/Autos/`: cada
  auto del pool instancia uno de esos prefabs al azar. Si esa carpeta está vacía
  cae a un **cubo rojo** de `CreatePrimitive` (`tamanoCuboFallback`) para que el
  juego siga andando sin arte. Cada auto lleva el tag `Obstaculo` en la raíz y,
  si el prefab no trajo `Collider`, se le agrega un `BoxCollider` genérico con
  aviso por consola. El jugador detecta el choque con su propio trigger
  (`OnTriggerEnter`), así que al auto le alcanza con un collider sólido, no
  trigger. Todos los valores son ajustables en el Inspector del GameObject que
  crea en runtime.
  Los prefabs de `Resources/Autos/` (`Bugatti.prefab`, `McLaren.prefab`) tienen
  la misma estructura: **raíz = GameObject vacío** con `BoxCollider` + tag +
  escala + giro de juego, y el `.glb` como **hijo sin tocar** (mantiene la
  rotación de conversión de glTFast, `270,0,0`). Nunca setear la rotación en la
  raíz del `.glb` directamente: se pierde esa conversión y el modelo queda de
  costado. Los `.glb` crudos están en `Assets/Models/`, no en `Resources`.
  Cada prefab de auto lleva además `RuedasAuto` (hace girar las 4 ruedas).
- **`RuedasAuto.cs`** — Va en la raíz de los prefabs de auto (`Bugatti.prefab`,
  `McLaren.prefab`). Hace girar las 4 ruedas según cuánto se desplaza el auto
  (lo mide solo, entre frames — el `TrafficManager` lo mueve desde afuera).
  Encuentra las ruedas por nombre ("Wheel", sin "Brake"), deduplica anidadas, y
  gira cada una con `RotateAround` sobre su **centro visual** (el pivote del
  hueso no está centrado). Se resetea en `OnEnable` para el reciclado del pool.
- **`MotoAnimada.cs`** — Va en `Cuerpo/MotoModelo`. (a) Gira `wheel_f` y
  `wheel_r` de la `DirtBike` según `GameManager.velocidadActual` (mismo truco de
  `RotateAround` sobre el centro visual). (b) Dobla `handle_main` (todo el
  conjunto manubrio + horquilla + rueda delantera) hacia el lado de
  `PlayerController.EntradaLateral`, con `SmoothDamp` (`anguloMaxDireccion` 16°,
  `suavizadoDireccion` 0.10s). Reset del manubrio a su pose inicial cada frame +
  `RotateAround` sobre `transform.up`.
- **`CamaraJugador.cs`** — Vive en la `Main Camera` (que ahora cuelga de la raíz
  de la escena, no del jugador). Sigue a `PuntoCamara` (un vacío hijo del
  `Jugador`, en `local (0, 1.45, -0.55)` — primera persona, apenas detrás del
  manubrio). **La posición va pegada al objetivo, sin retardo** (en 1ª persona el
  retardo de posición hace que todo lo cercano —manubrio, rueda— se deslice de
  lado y maree). Lo "cómodo" va solo en la **rotación**, con `Slerp`:
  (a) **roll** — se tumba `gradosRoll` (5°) hacia el lado según
  `PlayerController.EntradaLateral` suavizada (NO la velocidad, que temblaba);
  (b) **wheelie** — mientras `PlayerController.haciendoWheelie` la vista sube
  `gradosPitchWheelie` (18°) hacia el cielo. `pitchBase` (3°) = cuánto mira a la
  calle en reposo. Todo ajustable en el Inspector. La `Main Camera` tiene FOV 72.
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
  que llama a `MenuRanking.Refrescar()` cuando cambian. Desde el 2026-09-15 usa
  `EstiloUI` (colores + tipografías) y está armada con Layout Groups en vez de
  posiciones absolutas: panel de 500×664 pegado al borde derecho, con marco de
  neón, encabezado "RANKING / TOP 10" y 10 filas de puesto / nombre / puntaje. El
  primer puesto va resaltado en magenta.
- **`MenuOpciones.cs`** — Se crea en runtime desde `MenuManager`. Arma por código
  el `PanelOpciones` (hermano de los otros paneles, hijo del `Canvas`) y lo
  registra en `MenuManager.panelOpciones`. Contenido en secciones apiladas con
  `VerticalLayoutGroup`: **AUDIO** (3 sliders: "Volumen general" →
  `AudioListener.volume`, "Volumen música" → `AudioManager.musica.volume`,
  "Volumen efectos" → `AudioManager.efectos.volume`), **PERFIL** (campo "Tu nombre
  (para el ranking)" → `PlayerPrefs("NombreJugador")`), **DATOS** (botón "Reiniciar
  tabla de ranking" → `RankingData.Instance.BorrarTodo()`), **CÁMARA** (botón
  "Probar cámara" → `MenuManager.MostrarPruebaCamara()`), **CONTROLES** (texto
  informativo con los gestos de cámara y las teclas, no editable), y un
  botón "Volver". El **botón "Opciones"** del menú principal **ya no lo crea este
  script**: desde el 2026-09-15 es un objeto de la escena
  (`PanelMenu/BotonOpciones`), igual que Jugar y Salir, con su estilo puesto y su
  `OnClick` cableado en el Inspector. Lo único que quedó acá es
  `VerificarBotonOpciones()`, que avisa por consola si alguien lo borró.
- **`MenuCamara.cs`** — Se crea en runtime desde `MenuManager`. Arma por código el
  `PanelCamara` (hermano de los otros paneles) y lo registra en
  `MenuManager.panelCamara`. Es la pantalla de **"Probar cámara"**: sirve para que
  el jugador se acomode antes de jugar. Muestra en vivo, leyendo de
  `EntradaCamara`: el estado de la conexión (sin señal / conectada pero no te veo
  / listo), una barra con la dirección, una luz que se prende con el wheelie y la
  ayuda de los gestos. No abre la webcam por su cuenta (la tiene el script de
  Python), solo dibuja lo que llega por UDP. Se ve con el juego en
  `timeScale = 0`, por eso `EntradaCamara` usa `Time.unscaledTime`.
- **`EstiloUI.cs`** — Clase estática con la paleta, las tipografías y los sprites
  de la dirección visual **"Ruta de noche"**. Existe para que la UI que se arma
  por código (ranking, Opciones, pausa, HUD) use los mismos colores y fuentes que
  los objetos puestos a mano en la escena; si cambia el estilo se toca acá y no en
  cinco scripts. Carga las fuentes de `Resources/Fuentes/` y los sprites de
  `Resources/Sprites/` (mismo mecanismo que `TrafficManager` con
  `Resources/Autos/`: es la única forma de que un script llegue a un asset sin
  arrastrarlo en el Inspector).
- **`Assets/Editor/GeneradorSpritesMenu.cs`** — Script **de Editor** (no va al
  build) que dibuja por código los PNG del fondo del menú y los deja en
  `Assets/Resources/Sprites/`: `FondoNoche`, `SolRetro`, `HaloSol`, `GrillaNeon`,
  `ResplandorHorizonte`, `LineaHorizonte`, `Vineta` y `MarcoUI`. Se corre a mano
  desde el menú **Crazy Moto → Generar sprites del menú** y sólo hace falta
  volver a correrlo si se cambia un color. El juego en runtime carga los PNG como
  cualquier sprite: no genera nada en la partida. `MarcoUI` va **siempre** en modo
  Sliced (tiene `spriteBorder` 6) o se deforma.
  **Ojo:** después de editar este script hay que esperar a que Unity recompile
  antes de ejecutar el menú, si no corre la versión vieja y parece que no pasó
  nada (pasó: faltó `Vineta.png` y la capa quedó como un rectángulo blanco
  tapando todo el fondo).
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
- **`MenuManager.cs`** — Controla 6 paneles (menú / juego / game over con
  `SetActive`, y `panelOpciones` + `panelPausa` + `panelCamara` que se arman por
  código — ver `MenuOpciones`, `MenuPausa` y `MenuCamara`).
  Usa un `static bool irAJugar` en memoria (no `PlayerPrefs`) para saber, al
  recargar la escena, si debe arrancar jugando directo o mostrar el menú. Los
  botones que ya estaban en la escena (Jugar/Salir/Continuar/Menú) llaman a sus
  métodos públicos vía `OnClick` del Inspector; los botones "Opciones", "Volver"
  y "Reiniciar tabla" los cablea `MenuOpciones` por código. `MostrarOpciones()` /
  `VolverDeOpciones()` alternan menú ↔ opciones, y `MostrarPruebaCamara()` /
  `VolverDePruebaCamara()` alternan opciones ↔ probar cámara (esa pantalla siempre
  se abre y se cierra contra Opciones). En `Start()` crea el
  `MenuRanking`, el `MenuOpciones`, el `MenuPausa` y el `MenuCamara` si no
  existen. Expone
  `estaPausado { get; private set; }` — la variable que distingue *por qué* el
  juego está en `Time.timeScale = 0` (menú / game over / pausa), lo que resuelve
  el punto 1.7 de `docs/AUDITORIA.md`. `Pausar()` / `Reanudar()` /
  `AlternarPausa()` solo actúan si `panelJuego` está activo y el juego no
  terminó; `VolverDeOpciones()` vuelve a la pausa si Opciones se abrió desde ahí.
- **`MenuPausa.cs`** — Se crea en runtime desde `MenuManager` (patrón de
  `MenuOpciones`). Arma por código el `PanelPausa` (hermano de los otros paneles,
  hijo del `Canvas`) y lo registra en `MenuManager.panelPausa`: título "PAUSA" +
  3 botones centrados apilados **Continuar** (`MenuManager.Reanudar()`) /
  **Opciones** (`MenuManager.MostrarOpciones()`, el mismo `PanelOpciones` del
  menú; su "Volver" regresa a la pausa) / **Salir** (`MenuManager.VolverAlMenu()`,
  vuelve al menú principal). Además agrega el botón **"II"** en la esquina
  superior derecha de `MenuManager.panelJuego` (aparece/desaparece con él, como
  el texto de puntaje del HUD). En `Update()` lee **Escape**: si Opciones está
  abierto hace de "Volver", si no llama a `AlternarPausa()`. Es el único lugar
  del proyecto que lee `Input` de Escape.
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
  desde `GameManager.GameOver()`) y `ReproducirClick()` (lo llama
  `MenuManager.Reanudar()` — el botón "Continuar" de la pausa no recarga escena,
  así que el click sí llega a sonar). Los tres campos de `AudioClip` ya tienen
  clip asignado en la escena (2026-09-09): `musicaFondo` =
  `Sounds/Breakneck_Boulevard.mp3`, `sonidoChoque` = `Sounds/Crash.mp3`,
  `sonidoClick` = `Sounds/mouse-click-sound.mp3`. (El viejo `VolumenSlider.cs` se
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
  Materiales propios (cuando haya) van en `Assets/Materials/`. El código Python
  del control por cámara va en `vision/`, en la **raíz del repo y fuera de
  `Assets/`**, para que Unity no lo importe como assets.
  **Excepción obligada:** los scripts que usan `UnityEditor` van en
  `Assets/Editor/` (si no, el juego no compila al buildear) — hoy sólo está
  `GeneradorSpritesMenu.cs`. Y lo que un script tenga que cargar en runtime va
  bajo `Assets/Resources/`: `Autos/` (prefabs de tráfico), `Sprites/` (fondo del
  menú) y `Fuentes/` (los TMP Font Assets). Los `.ttf` crudos viven en
  `Assets/Fonts/`, fuera de Resources.
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
que vienen de frente por 3 carriles (A-D o **cámara web**, movimiento libre) →
sostener wheelie (Shift o los dos brazos tirados hacia el cuerpo) para sumar
puntaje a costa de casi no poder esquivar → chocar un auto →
Game Over con puntaje final → Continuar/Menú, todo recargando la escena. El
puntaje se guarda en un ranking persistente (`ranking.json`) que se ve en el
menú: Top 10 + "estás en el puesto N" + botón "Reiniciar tabla". Durante la
partida se puede **pausar** con Escape o con el botón "II" del HUD: pantalla con
Continuar / Opciones / Salir (al menú).

**A medias:** UI escalable configurada (CanvasScaler con "Scale With Screen
Size"). El **menú principal ya tiene arte** (dirección "Ruta de noche": fondo
nocturno, sol retro a rayas, grilla de neón en perspectiva, título en Chakra
Petch con degradado, botones de neón y tabla de ranking con marco) — ver la
decisión del 2026-09-15. El HUD de puntaje, el panel de Opciones, la pausa y el
Game Over **siguen en greybox** (`Hud.cs`, `MenuOpciones.cs`, `MenuPausa.cs`):
les toca la próxima pasada y ya tienen de dónde sacar el estilo (`EstiloUI`).
Opciones agrupa en secciones los 3 volúmenes, el nombre, el reinicio del ranking
y la ayuda de controles. Los autos y la moto del jugador ya son **modelos 3D reales con rig**
(`.glb` vía glTFast): `TrafficManager` instancia `Bugatti.prefab` / `McLaren.prefab`
de `Assets/Resources/Autos/` (fallback a cubo rojo si la carpeta está vacía) y la
moto es `Cuerpo/MotoModelo` (`DirtBike.glb`) en la escena. Las 4 ruedas de cada
auto y las 2 de la moto giran (`RuedasAuto` / `MotoAnimada`), y el manubrio de la
moto dobla con el input. La cámara es **primera persona** (`CamaraJugador` en la
`Main Camera`): posición pegada al `PuntoCamara` de la moto, roll al inclinarse y
la vista sube al cielo mientras se sostiene el wheelie. El camino ya no tiene
huecos (el prefab `Tramo` se reescaló a `{10, 1, 30}`) y tiene líneas de carril
discontinuas, pero sigue siendo un cubo gris sin textura ni arte.

**No existe todavía:** ruidos ambientales de la partida (motor de la moto, autos
pasando) — la música de menú, el click y el choque ya suenan; partículas, luces de
efecto. (La cámara ya tiene efectos propios —roll, subida al cielo en el
wheelie— pero no hay Cinemachine ni shake de choque.) **Decidido que NO habrá
salto** (la mecánica vertical
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
  y el Gateway con provider "Claude Code". A partir de acá puedo leer y editar
  escena, prefabs, assets y consola de Unity directamente desde la sesión, sin
  que el alumno tenga que pegar código a mano. Se descartó Figma (el alumno
  prefiere pulir el proyecto antes de meter arte/modelos). (La config exacta del
  cliente cambió — ver 2026-09-08: el paquete es `com.unity.ai.assistant` y la
  config vive en `~/.claude.json`, no en un `.mcp.json` del repo. `.gitignore`
  ignora `Packages/com.unity.ai.assistant/` y `ProjectSettings/Packages/`, así
  que cada máquina reinstala el paquete por su cuenta.)
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
  `Unity_ManageAsset` (Action "Delete"), no desde `RunCommand`. (Ampliación
  2026-09-08: `RunCommand` tampoco deja usar `System.Reflection` —rechaza el
  namespace—; `Unity_ManageAsset` "Move" reporta error pero igual mueve el
  archivo, hay que verificar en disco; y en Play mode el juego solo avanza si
  `Application.runInBackground = true` o el Editor tiene foco.)
- **2026-09-03** — Pausa durante la partida (pedido del alumno). Se activa con
  **Escape** y con un botón **"II"** en la esquina superior derecha del HUD.
  Decisiones del alumno: al pausar aparece una pantalla con 3 botones centrados
  —**Continuar**, **Opciones**, **Salir**— donde "Opciones" abre el mismo
  `PanelOpciones` del menú (su "Volver" regresa a la pausa) y "Salir" vuelve al
  menú principal. Implementado por código sin tocar escena ni prefabs: nuevo
  `MenuPausa.cs` (arma el panel y el botón "II" en runtime, patrón de
  `MenuOpciones`); `MenuManager` gana `panelPausa`, `estaPausado` y
  `Pausar()`/`Reanudar()`/`AlternarPausa()`. De paso se resolvió el punto 1.7 de
  `docs/AUDITORIA.md`: `estaPausado` es la variable que faltaba para que menú,
  game over y pausa no se pisen al compartir `Time.timeScale = 0`. El botón
  "Continuar" engancha `AudioManager.ReproducirClick()` (mudo hasta que haya
  clip). Verificado en Play por el alumno.
- **2026-09-08** — Modelos 3D con glTFast (pedido del alumno). (1) Se agregó el
  paquete `com.unity.cloud.gltfast` (`6.20.0`) al `manifest.json` para importar
  `.glb`. (2) El alumno renombró la escena `SampleScene.unity` → `ESCENA 1.unity`
  desde Unity (mismo GUID); se corrigió el `path` en
  `ProjectSettings/EditorBuildSettings.asset` y el `templateDefaultScene` de
  `ProjectSettings.asset`, que habían quedado apuntando al nombre viejo. (3)
  Modelos en `Assets/Models/`: `Moto.glb` (12,9 MB), `Auto.glb` (13,7 MB) y
  `Toyota Prius 2012.glb` (3,6 MB) — low-poly, importados sin warnings. (Primero
  el alumno bajó modelos de 150/47/43 MB: el de 150 MB ni siquiera pushea a
  GitHub —límite de 100 MB por archivo— y eran demasiado pesados en texturas; los
  reemplazó por estos. Regla: modelos de juego por debajo de ~5 MB; si hace falta
  achicar, `gltf-transform resize`.) **Decisión: los `.glb` van commiteados al
  repo** (no Git LFS, no `.gitignore`) porque el compañero clona el proyecto y
  tiene que funcionar sin bajar nada aparte. (4) Se rehízo el puente Unity MCP:
  el alumno reinstaló el paquete `com.unity.ai.assistant` (`2.19.0-pre.2`) y
  activó el "Unity MCP Server" en Project Settings → AI; la config de cliente
  quedó en `~/.claude.json` (clave global `mcpServers.unity-mcp`, apunta a
  `~/.unity/relay/relay_win.exe --mcp`), NO en un `.mcp.json` del repo. Con eso
  Claude volvió a poder leer/editar la escena. Esta vez `com.unity.ai.assistant`
  quedó como dependencia de registro en `Packages/manifest.json` (antes era
  embebido/gitignoreado); es un `pre.2` y necesita cuenta de Unity con IA — si le
  molesta a Santiago al clonar, se puede borrar esa línea (glTFast sí queda).
  (5) **Swap de modelos hecho por
  MCP:** `TrafficManager` reescrito para instanciar prefabs de
  `Assets/Resources/Autos/` (`Resources.LoadAll`, fallback a cubo rojo). Los 3
  `.glb` estaban en unidades y ejes distintos (Auto y Moto en milímetros y
  apuntando al eje X; Prius con el largo en Y) y con la rotación de conversión de
  glTFast en la raíz (`270,0,0`). Solución: cada modelo va **envuelto en un
  GameObject vacío** que lleva la escala, el giro de juego y el `BoxCollider`; el
  `.glb` hijo NO se toca (conserva su `270,0,0`). Auto y Prius quedaron como
  `Auto.prefab` / `Prius.prefab` en `Resources/Autos/` (~4,3 / 4,5 m de largo,
  tag `Obstaculo` en la raíz, collider ajustado a la malla, mirando −Z = de
  frente al jugador). La moto es `Cuerpo/MotoModelo` en la escena (~2 m, mirando
  +Z = el jugador la ve de atrás), reemplaza al cubo `Modelo` que se borró. Los
  `.glb` crudos viven en `Assets/Models/` (fuera de `Resources` para que
  `LoadAll` no los duplique). Verificado por MCP: compila sin errores, los
  prefabs cargan, el tráfico spawnea filas de 1-2 autos. El alumno lo probó
  jugando y **funciona bien** (el choque dispara el Game Over). Commiteado y
  pusheado en `11856ab` (rama `crazymoto-rama`). Pendiente de afinar si molesta:
  el hitbox del `Jugador` sigue en 1×1×1 frente a la moto de 2 m; el pool son 20
  autos con modelos detallados (bajar a ~10 si caen los FPS en la notebook).
- **2026-09-09** — Se sacó una **capa gris** que tapaba el juego al apretar
  Jugar: `PanelJuego` traía un componente `Image` de pantalla completa (blanco al
  39 %, la basura por defecto de un "UI Panel"). `PanelJuego` es solo el
  contenedor del HUD, así que se le quitó el `Image` (queda `RectTransform` +
  `CanvasRenderer` vacío). **Ojo:** `PanelGameOver` tiene el **mismo** `Image`
  blanco 39 % — ahí hace de dim del Game Over y quedó, pero si se quiere que se
  vea mejor conviene cambiarlo a negro ~55 %. Editado por MCP en `ESCENA 1.unity`.
- **2026-09-09** — Cambio de modelos por unos **con rig / partes separadas** (el
  alumno borró los anteriores y bajó nuevos). En `Assets/Models/`:
  `DirtBike.glb` (0,7 MB, moto del jugador), `bugatti_veyron_fully_rigged..glb`
  (7,7 MB) y `mclaren_mp4_rigged.glb` (3,2 MB) para tráfico. Se **descartó**
  `lexus_rx_350__rigged__rigged_driver_human.glb` (27 MB / 480k vértices +
  conductor humano que se separaba): demasiado pesado para el pool de tráfico.
  Los 3 usados tienen las ruedas como transforms separados: los autos tienen
  `.../Car Rig_XX/DEF-Wheel.Ft.L/R` y `DEF-Wheel.Bk.L/R`; la `DirtBike` tiene
  `wheel_f`, `wheel_r` y `handle_main` (todo el conjunto manubrio+horquilla, para
  girar la dirección) bajo
  `DirtBike_GBL/global_bone/root/root.001/...`. Prefabs nuevos armados con el
  mismo patrón wrapper: `Assets/Resources/Autos/Bugatti.prefab` y `McLaren.prefab`
  (~4,5 m, tag `Obstaculo`, collider a la malla, mirando −Z). La moto es
  `Cuerpo/MotoModelo` con `DirtBike` de hijo (~2,1 m, mirando +Z). Se borraron
  `Auto.prefab` y `Prius.prefab` viejos. Verificado por MCP: sin errores, los 2
  prefabs cargan, tráfico spawnea. Pendiente (pedido del alumno, en curso):
  cámara en 1ª persona + wheelie que tape la vista; ruedas girando; manubrio de
  la moto moviéndose con el input.
- **2026-09-09** — Cámara en **primera persona** (pedido del alumno). Nuevo
  `CamaraJugador.cs` en la `Main Camera` + un vacío `PuntoCamara` hijo del
  `Jugador`. La `Main Camera` se sacó de `PivotCamara` y ahora cuelga de la raíz;
  la maneja el script (sigue a `PuntoCamara` con suavizado en X/Y, pegada en Z).
  Decisiones probadas por MCP con capturas: vista con el manubrio abajo y la
  calle despejada; al hacer wheelie la vista sube 18° al cielo (queda una franja
  de horizonte, no ciego del todo); se tumba ~6° hacia donde te movés; FOV 72.
  `PivotCamara` quedó sin uso pero se dejó (ver `PlayerController`). Falta que el
  alumno lo pruebe jugando con teclado (sensación del suavizado y el roll).
- **2026-09-09** — Ruedas girando + manubrio (pedido del alumno). Los modelos
  nuevos traen las ruedas como transforms separados, pero **con el pivote fuera
  del centro de la rueda** (rig de Blender) → hay que girarlas con
  `RotateAround` alrededor del **centro visual** (bounds combinado de sus
  mallas), no de su pivote, y **deduplicar** (a veces hay un transform "Wheel"
  adentro de otro). Dos scripts nuevos: `RuedasAuto.cs` (en `Bugatti.prefab` y
  `McLaren.prefab`; mide su propia velocidad por el desplazamiento entre frames,
  gira las 4 ruedas) y `MotoAnimada.cs` (en `Cuerpo/MotoModelo`; gira `wheel_f` y
  `wheel_r` según `GameManager.velocidadActual` y dobla `handle_main` —todo el
  conjunto manubrio+horquilla— según `PlayerController.EntradaLateral`, con
  `SmoothDamp`). `PlayerController` ahora expone `EntradaLateral` (la entrada
  lateral cruda) para que la use `MotoAnimada` sin duplicar llamadas a `Input`.
  Verificado por MCP: las ruedas giran en su lugar sin salirse ni derivar, el
  manubrio dobla para el lado correcto (D → derecha).
  **Corrección (mismo día):** la primera versión usaba `Rotate` incremental y la
  rueda de la moto "salía volando" / temblaba de lado (el `RotateAround` acumula
  error de FP frame a frame y se espirala). Se cambió a: guardar la pose inicial,
  acumular un **ángulo escalar mod 360**, y cada frame resetear la rueda y
  aplicar el total con un solo `RotateAround`. Además `factorVisual` (0.5) baja la
  velocidad de giro visual para que no estroboscopie. El pivote del `RotateAround`
  es el **centroide de los vértices** de la rueda (cae sobre el eje), no el centro
  de la bounding box (se corría por el disco de freno / la corona). El eje real
  de las 3 ruedas es X del mundo (medido: ±0.3°). Verificado: el centro queda
  fijo, no deriva. El otro "temblar de lado a lado" que reportó el alumno NO era
  la rueda sino la **cámara** (seguía la posición con retardo lateral → todo lo
  cercano se deslizaba): se corrigió dejando la posición pegada (ver
  `CamaraJugador`) y el roll ahora sale de la entrada suavizada, no de la
  velocidad. Falta que el alumno confirme jugando.
- **2026-09-09** — Audio completo (cierra la entrega del 10 de septiembre). El
  alumno consiguió y asignó en el Inspector los clips que faltaban:
  `Assets/Sounds/Breakneck_Boulevard.mp3` en `musicaFondo` (música del menú) y
  `Assets/Sounds/mouse-click-sound.mp3` en `sonidoClick`. Con `Crash.mp3` ya
  puesto, los 3 campos de `AudioManager` tienen clip. `docs/ENTREGAS.md`
  actualizado: la entrega del 10 de septiembre pasa a ✅ cumplida.
- **2026-09-09** — **Control por cámara web** (reemplaza al manubrio de Arduino,
  que salía caro). El jugador se sienta frente a la cámara con los brazos como si
  agarrara un manubrio: **tira un brazo y empuja el otro para doblar**, y **tira
  los dos a la vez para el wheelie**. Decisión técnica clave: **no se usa la
  coordenada Z** de MediaPipe (con una sola cámara la profundidad es estimada y
  tiembla). Se mide el **ángulo del codo** (hombro-codo-muñeca) en 2D, que es lo
  mismo pero por el eje confiable: tirar el brazo lo dobla. De ahí salen las dos
  señales, y son independientes: `dirección = flexión_der − flexión_izq` (la
  diferencia) y `wheelie = promedio de las dos flexiones`. Doblar mueve la
  diferencia y deja quieto el promedio, y al revés, así un gesto no pisa al otro.
  Se descartaron: girar el puño (necesita un segundo modelo y la mano cerrada se
  detecta peor) y abrir la boca (confiable pero incómodo de sostener, y el
  wheelie es una acción sostenida). Archivos nuevos: `vision/deteccion.py`
  (Python 3.14 + MediaPipe 1.0.1 + OpenCV), `vision/modelo/pose_landmarker_lite.task`
  (5,5 MB, **commiteado** igual que los `.glb` para que Santiago no baje nada),
  `vision/README.md`, `vision/requirements.txt`, `Assets/Scripts/EntradaCamara.cs`
  y `Assets/Scripts/MenuCamara.cs`. Editados: `PlayerController` (los dos métodos
  de input), `GameManager` (crea `EntradaCamara`), `MenuManager` (`panelCamara` +
  2 métodos) y `MenuOpciones` (sección CÁMARA). **Sin tocar escena ni prefabs.**
  Ojo con MediaPipe: la **1.0.1 eliminó `mp.solutions`**, hay que usar la API de
  Tasks (`vision.PoseLandmarker` + archivo `.task`); y `cv2.putText` no dibuja
  acentos ni eñes, por eso los textos de la ventana de OpenCV van sin tildes.
  Verificado: los 16 scripts compilan con el Roslyn de Unity, la API de MediaPipe
  corre, la matemática da los signos correctos y el UDP llega con el formato que
  espera `EntradaCamara`. **Falta que el alumno lo pruebe con la cámara real.**
- **2026-09-09** — El alumno probó el control por cámara: la dirección anda
  perfecto, pero **el wheelie se activaba al principio y después dejaba de
  tomar**, y recalibrar con `c` no lo arreglaba. Causa: la `flexion()` va de 0 a 1
  y **se clava en 1**, así que el margen que te queda para doblar depende de cómo
  estés sentado. El umbral era **fijo** (`extra > 0.16`): si calibrabas con los
  brazos ya doblados (reposo ≈ 0.86), te quedaban 0.14 de recorrido y el gesto era
  **imposible** — y recalibrar en esa misma postura lo dejaba igual de muerto.
  Arreglo: ahora hay **dos maneras de activarlo y alcanza con cumplir una**,
  `WHEELIE_DELTA` (cuánto doblaste en absoluto, sirve para el que se sienta
  estirado) y `WHEELIE_FRACCION` (qué parte del recorrido que te queda usaste,
  sirve para el que se sienta encogido); se toma la que más favorece. Verificado
  con una simulación en 9 posturas (codo en reposo de 130° a 65°) × 3 tamaños de
  tirón: la fórmula vieja fallaba en 70° y 65°, la nueva funciona en las 27
  combinaciones y no se dispara sola con ruido de ±3°. De paso se arreglaron dos
  cosas más del mismo script: el **timestamp** de `detect_for_video` ahora es
  estrictamente creciente (si dos cuadros caían en el mismo milisegundo, MediaPipe
  tiraba excepción y se cortaba todo), y se agregó **medio segundo de gracia** más
  visibilidad mínima más baja para muñecas y codos, porque al tirar los brazos se
  tapan contra el torso y la detección parpadeaba, tirándote al teclado en pleno
  wheelie. La ventana ahora dibuja una **barra de wheelie** con la distancia al
  umbral, y avisa si calibraste con los brazos demasiado cerrados. **Nada de esto
  tocó código C#**: el bug era todo del lado de Python.
- **2026-09-09** — Limpieza de dependencias, a pedido del alumno ("que solo se
  suba lo necesario; lo que no sirve o hace más lenta la instalación, no"). Se
  sacaron de `Packages/manifest.json` **6 paquetes con 0 usos reales**:
  `ai.navigation`, `collab-proxy`, `multiplayer.center`, `test-framework`,
  `timeline` y `visualscripting`. `packages-lock.json` bajó bastante porque varios
  arrastraban dependencias propias. Verificado antes de tocar nada: los 18 scripts
  que referencian la escena y los prefabs siguen resolviendo, la escena tiene 0
  componentes rotos sobre 52 objetos, y los 16 scripts compilan.
  **`com.unity.ai.assistant` SE QUEDA** (el puente Unity MCP). En el intento
  original lo saqué junto con los otros y eso desinstaló el paquete, dejando sin
  MCP al alumno, que lo usa. Él lo restauró y pidió que en vez de eliminar cosas
  se las excluyera del repo. **Aclaración técnica que hay que tener presente:
  `.gitignore` funciona sobre archivos, no sobre líneas de un archivo versionado**,
  así que una dependencia listada en `manifest.json` no se puede "gitignorar"
  sola; ignorar el `manifest.json` entero dejaría al que clona sin ningún paquete
  (ni URP, ni glTFast) y el proyecto no abriría. La regla
  `/Packages/com.unity.ai.assistant/` que está en el `.gitignore` es de cuando el
  paquete era **embebido** (una carpeta); hoy es dependencia de registro y esa
  regla no hace nada. Decisión tomada: queda en el repo, y si a Santiago le
  molesta al clonar borra esa línea (está documentado en la tabla de problemas del
  `README.md`).
  **`com.unity.inputsystem` SE QUEDA, y es importante entender por qué:** una
  búsqueda por texto en la escena da 0 resultados y parece no usarse, pero el
  `EventSystem` referencia `InputSystemUIInputModule` **por GUID**, no por nombre.
  Sin ese paquete dejan de funcionar todos los botones de los menús. Moraleja:
  para saber si un paquete se usa hay que resolver los **GUID** de `m_Script` de
  la escena y los prefabs, no hacer `grep` por el nombre.

- **2026-09-15** — **Arte del menú principal: dirección "Ruta de noche"**. Antes
  se le pasaron al alumno cuatro bocetos (la UI de ese momento + tres
  direcciones) y eligió la **C, "Ruta de noche"** (arcade nocturno). Sus
  decisiones: fondo **opaco** (el degradado tapa la vista del juego, no se ve la
  ruta atrás), **sólo el menú principal** en esta pasada, y bajó él mismo
  **Chakra Petch** de Google Fonts. Sobre cómo hacer el fondo preguntó qué usan
  los juegos de verdad: la respuesta es **PNG** (casi sin excepción; generar
  texturas por código es cosa de prototipos), así que se hizo el punto medio —
  los PNG los **dibuja un script de Editor** (`GeneradorSpritesMenu`) y quedan
  como archivos de verdad en `Assets/Resources/Sprites/`, que el juego carga como
  cualquier sprite y que el alumno puede retocar a mano si quiere.
  Trabajo hecho en la rama **`integracion-ui`**, todo por MCP:
  (1) 8 PNG generados (~119 KB en total);
  (2) 3 TMP Font Assets de Chakra Petch (Bold, BoldItalic, SemiBold) creados con
      `TMP_FontAsset.CreateFontAsset` en modo **Dynamic**, guardados en
      `Assets/Resources/Fuentes/` (los `.ttf`, ~220 KB, en `Assets/Fonts/`);
  (3) **escena tocada** — `PanelMenu` pasó de 3 hijos a 12: seis capas de fondo
      (`Fondo`, `HaloSol`, `SolRetro`, `ResplandorHorizonte`, `GrillaNeon`,
      `LineaHorizonte`), la `Vineta` encima, y después `Titulo`, `Subtitulo` y los
      tres botones. El `Image` del propio `PanelMenu` dejó de ser blanco al 39 % y
      ahora es `#08061A` opaco, de respaldo por si un sprite no cargara;
  (4) los botones son de **480×88** con `localScale` 1 (antes eran 160×30 con
      escala 2,5 — un lío para calcular posiciones); Jugar va relleno magenta y
      los otros dos con marco de neón (`MarcoUI` en Sliced);
  (5) `EstiloUI.cs` nuevo, y `MenuRanking.cs` reescrito con Layout Groups.
  Verificado por captura: el menú renderiza como el boceto. **Falta que el alumno
  lo pruebe en Play** (la tabla de ranking sólo se arma en runtime, así que en el
  Editor sin Play no se ve, igual que antes).
  Dos cosas que se ajustaron sobre la marcha después de mirar la captura: se
  agregó la **viñeta** (sin ella el sol y el resplandor competían con el texto) y
  el **sol se bajó** de y=109 a y=55 y se achicó a 440, porque su borde superior
  caía justo sobre el subtítulo y el texto cyan se perdía contra el amarillo.

- **Nota de trabajo (2026-09-15)** — La rama activa es **`integracion-ui`**
  (sale de `crazymoto-rama`, va 6 commits adelante de `main`). No estaba
  documentada hasta hoy.
  Dos trampas del puente MCP que costaron tiempo y conviene recordar:
  **(a)** cada vez que se edita un `.cs` Unity recompila y recarga el dominio, y
  durante esos segundos **todas** las tools contestan "Unity not detected"; hay
  que reintentar, no es que se cayó. **(b)** Por eso mismo, después de editar un
  script de Editor hay que **esperar a que termine de compilar antes de ejecutar
  su `MenuItem`** — si no, corre el binario viejo y el cambio no se aplica sin
  ningún error visible.
  Tercera: los `instanceID` que devuelven las tools (`GetHierarchy`, etc.) vienen
  **truncados** por precisión de JSON (son mayores que 2^53), así que no sirven
  para pasarlos a otra tool; hay que buscar los objetos por nombre.

- **Manubrio con Arduino:** quedó **en pausa** — el alumno lo vio muy caro y lo
  reemplazó por el control con cámara web (ver la decisión del 2026-09-09). No
  está descartado del todo, pero solo se retoma si el de cámara no convence. La
  arquitectura sigue lista para los dos: el input crudo está aislado en
  `LeerLateral()` y `LeerWheelie()` dentro de `PlayerController`, y agregar un
  mando nuevo es tocar solo esos dos métodos. No dispersar llamadas a `Input.*`
  por otros scripts.
- **UI a arte real:** el **menú principal ya está hecho** (objetos de la escena
  bajo `PanelMenu`, con el estilo "Ruta de noche"). Falta llevar el mismo estilo
  a las otras pantallas, que se siguen armando por código y en greybox: `Hud.cs`
  (puntaje en `PanelJuego`/`PanelGameOver`), `MenuOpciones.cs` (panel de
  Opciones), `MenuPausa.cs` (panel de pausa y el botón "II" del HUD) y
  `MenuCamara.cs` (probar cámara). `MenuRanking.cs` ya usa `EstiloUI`; las demás
  deberían hacer lo mismo. (Los autos ya usan modelos reales vía
  `TrafficManager` + `Assets/Resources/Autos/`.)

## Pendiente de fecha (checklist de la feria)

Detalle completo y actualizado en `docs/ENTREGAS.md`. Resumen de lo que falta y
es decisión del alumno (no de código):

- El **boceto del juego** sigue sin hacerse (entrega del 6 de agosto). Multiplayer
  (1 jugador) y ranking (comparación de puntajes de mayor a menor) ya están
  definidos, pero conviene volcarlos al punto 1.6 de
  `docs/PROYECTO FERIA DE CIENCIAS.docx`, que hoy los tiene vacíos.
- La entrega del **10 de septiembre** quedó cumplida: datos persistentes +
  sonido (los 3 clips de `AudioManager` asignados el 2026-09-09). Lo que sigue es
  la del **17 de septiembre**: partículas, efectos e iluminación.
