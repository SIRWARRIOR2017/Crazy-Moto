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
  feria. Describe un loop de "saltar obstáculos controlando la inclinación en el
  aire", puntaje, y comparación contra un ranking de otros jugadores (dice que el
  juego "es competitivo"). Esto es mucho más grande que lo que hay hoy: no hay
  salto real, no hay puntaje, no hay ranking, no hay sonido, ni partículas, ni luces,
  ni datos persistentes. Ver `docs/AUDITORIA.md` para el detalle punto por punto.

## Arquitectura real

Todo vive en una sola escena (`Assets/Scenes/SampleScene.unity`) que se recarga
completa (`SceneManager.LoadScene`) para "reiniciar" el juego — no hay reseteo manual
de estado.

- **`GameManager.cs`** — Singleton (`GameManager.Instance`, sin `DontDestroyOnLoad`,
  se reinicia solo porque la escena entera se recarga). Lleva `velocidadActual`
  (acelera con el tiempo hasta `velocidadMaxima`) y `juegoTerminado`. En `Start()`
  crea un **cubo rojo hardcodeado** como único obstáculo de prueba
  (`CrearObstaculoDePrueba`), a una Z fija. `GameOver()` pone `Time.timeScale = 0`
  y busca un `MenuManager` en la escena (`FindFirstObjectByType`) para pedirle que
  muestre el panel de Game Over.
- **`PlayerController.cs`** — Vive en el GameObject "Jugador". Cada `Update()` avanza
  el transform en Z según `GameManager.Instance.velocidadActual`, mueve
  lateralmente con `Input.GetAxis("Horizontal")` clampeado a `limiteLateral`, e
  inclina dos transforms (`cuerpo` y `pivotCamara`) al mantener Space (wheelie es
  **puramente visual**, no hay salto ni cambio de Y). Detecta derrota con
  `OnTriggerEnter` chequeando `tag == "Obstaculo"` y llama a
  `GameManager.Instance.GameOver()`. La cámara es hija de `pivotCamara`, que es
  hijo del jugador — el seguimiento de cámara es gratis por jerarquía, no hay
  script de cámara.
- **`RoadManager.cs`** — Pool circular de tramos de camino (prefab
  `Assets/Prefabs/Tramo.prefab`):
  crea `cantidadTramos` al `Start()`, y en `Update()` cuando el tramo más viejo
  queda a más de `largoTramo` detrás del jugador, lo reubica adelante de todo
  (`Queue` implementada a mano con `List<Transform>`).
- **`MenuManager.cs`** — Controla 3 paneles (menú / juego / game over) con
  `SetActive`. Usa un `static bool irAJugar` en memoria (no `PlayerPrefs`) para
  saber, al recargar la escena, si debe arrancar jugando directo o mostrar el
  menú. Todos los botones de UI llaman a sus métodos públicos vía `OnClick`
  configurado en el Inspector (no hay listeners por código).
- **`AudioManager.cs`** — Singleton (`AudioManager.Instance`, mismo patrón que
  `GameManager`, se recrea con cada recarga de escena). Tiene un `AudioSource`
  para música (loop) y otro para efectos (`PlayOneShot`); si no se asignan a
  mano en el Inspector, se crean solos en `Awake()`. Expone
  `ReproducirChoque()` (enganchado desde `GameManager.GameOver()`) y
  `ReproducirClick()` (todavía sin usar — ver nota más abajo). Los campos de
  `AudioClip` (`musicaFondo`, `sonidoChoque`, `sonidoClick`) están vacíos a
  propósito: no hay archivos de audio en el proyecto todavía, hay que
  arrastrarlos en el Inspector cuando existan.
- **`VolumenSlider.cs`** — Vive en el Slider de volumen dentro de `PanelMenu`.
  Al arrancar lee el volumen guardado en `PlayerPrefs("Volumen")` (default 1) y
  lo aplica a `AudioListener.volume` (afecta todo el audio del juego, no pasa
  por `AudioManager`). Su método `CambiarVolumen(float)` está enganchado al
  evento `On Value Changed` del Slider en el Inspector.

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
  volver a abrir el juego (ej: `"Volumen"`), nunca para flags de un solo uso
  entre una recarga de escena y la siguiente (eso se resuelve con un `static`
  en memoria — ver el bug que se corrigió en `MenuManager`, sección de
  decisiones).

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

**Funciona:** loop completo de la primera entrega — menú → jugar → moverse
lateral + wheelie visual → chocar contra el cubo de prueba → Game Over →
Continuar/Menú, todo recargando la escena.

**A medias:** UI escalable configurada (CanvasScaler con "Scale With Screen
Size") pero sin HUD real durante el juego (no hay velocidad, distancia ni
puntaje en pantalla); el pooling de `RoadManager` funciona pero el prefab
`Tramo` es visualmente mucho más corto (3 unidades) que la distancia entre
spawns (`largoTramo = 30`), así que el camino se ve con huecos grandes.

**No existe todavía:** salto real (el wheelie no cambia la posición Y),
generación de obstáculos variados/progresivos (solo hay uno fijo), puntaje,
ranking/competitivo, partículas, luces de efectos, datos persistentes vía
ScriptableObject, y archivos de audio reales (el controlador de sonido está
armado y probado, pero suena en silencio hasta que se le asignen clips).

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
  de la feria (fechas, puntaje y estado de cada una contra lo que hay en el
  repo). Mantenerlo actualizado cada vez que se cierra o cambia el estado de una
  entrega. Puntos a tener presentes: el profesor pone CERO a lo entregado fuera
  de tiempo; en el repo, `docs/documento.docx` figura subido el 18 de agosto
  (fecha límite era el 13) y `docs/PROYECTO FERIA DE CIENCIAS.docx` el 13 de
  agosto (fecha límite era el 6) — hay que confirmar con el profesor qué fecha
  cuenta. Entregas del 20 y 27 de agosto: cumplidas. Próxima en riesgo: 3 de
  septiembre (mecánica principal), que necesita la decisión de diseño del salto.

## Pendiente de fecha (checklist de la feria)

Detalle completo y actualizado en `docs/ENTREGAS.md`. Resumen de lo que falta y
es decisión del alumno (no de código):

- Boceto del juego, definición de multijugador, y detalle del ranking/puntaje
  siguen sin escribirse en `docs/PROYECTO FERIA DE CIENCIAS.docx` (entrega del
  6 de agosto, todavía incompleta).
- `musicaFondo` y `sonidoClick` en `AudioManager` siguen vacíos (solo
  `sonidoChoque` tiene clip asignado). Hace falta para la entrega de sonido del
  10 de septiembre.
