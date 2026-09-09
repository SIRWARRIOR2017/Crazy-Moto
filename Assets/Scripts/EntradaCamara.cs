using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// Recibe el control por cámara web que manda el script de Python
// (vision/deteccion.py) por UDP a localhost.
//
// El paquete es una línea de texto con 3 campos separados por ';':
//     lateral;wheelie;detectado      ej: "-0.35;1;1"
//   lateral   = float -1..1  (negativo = izquierda, positivo = derecha)
//   wheelie   = 0 o 1        (los dos brazos tirados hacia el cuerpo)
//   detectado = 0 o 1        (hay una persona en cuadro)
//
// El socket se lee en un hilo aparte porque Receive() bloquea; el hilo NO toca
// la API de Unity (no se puede desde otro hilo), solo escribe campos volatile
// que Update() copia a las propiedades públicas.
//
// A diferencia del resto de los objetos del juego, este SÍ usa DontDestroyOnLoad:
// es dueño de un recurso del sistema operativo (el puerto UDP 5005) y reabrirlo
// en cada recarga de escena —que en este juego pasa cada vez que empezás una
// partida— puede fallar con "address already in use" si el anterior todavía no
// se cerró. Sobrevivir a la recarga evita ese problema del todo.
public class EntradaCamara : MonoBehaviour
{
    public const int Puerto = 5005;

    // Si no llega ningún paquete en este tiempo, damos la cámara por caída y el
    // juego vuelve solo al teclado.
    private const float SegundosSinSenal = 0.5f;

    public static EntradaCamara Instance { get; private set; }

    // ---- Lo que lee el resto del juego ----
    public float Lateral { get; private set; }
    public bool Wheelie { get; private set; }

    // Están llegando paquetes del script de Python.
    public bool HaySenal { get; private set; }
    // Además de llegar paquetes, la cámara está viendo a una persona.
    public bool PersonaDetectada { get; private set; }

    // Solo mandamos la cámara al juego si las dos cosas se cumplen. Si no, el
    // teclado sigue funcionando como siempre (ver PlayerController).
    public bool Activa => HaySenal && PersonaDetectada;

    // ---- Estado compartido con el hilo ----
    private volatile float lateralCrudo;
    private volatile bool wheelieCrudo;
    private volatile bool detectadoCrudo;
    private volatile bool datoNuevo;
    private volatile bool corriendo;

    private Socket socket;
    private Thread hilo;
    private float ultimoDato = -999f;

    // Lo crea GameManager en Start() si no existe (mismo patrón que TrafficManager
    // y Hud), así está disponible tanto en el menú como en la partida.
    public static void CrearSiNoExiste()
    {
        if (Instance != null) return;
        new GameObject("EntradaCamara").AddComponent<EntradaCamara>();
    }

    void Awake()
    {
        // Al recargar la escena, GameManager vuelve a llamar a CrearSiNoExiste();
        // el que ya existe gana y el duplicado se destruye sin tocar el socket.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Arrancar();
    }

    void Arrancar()
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Sin esto, reabrir el puerto justo después de cerrarlo puede fallar.
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.ReceiveTimeout = 500;   // así el hilo despierta y puede salir
            socket.Bind(new IPEndPoint(IPAddress.Loopback, Puerto));
        }
        catch (SocketException e)
        {
            Debug.LogWarning("EntradaCamara: no pude abrir el puerto " + Puerto +
                             " (" + e.Message + "). El juego sigue con teclado.");
            socket = null;
            return;
        }

        corriendo = true;
        hilo = new Thread(Escuchar);
        hilo.IsBackground = true;   // no impide que se cierre el juego
        hilo.Start();
    }

    // Corre en el hilo secundario: nada de API de Unity acá adentro.
    void Escuchar()
    {
        byte[] buffer = new byte[128];
        EndPoint desde = new IPEndPoint(IPAddress.Any, 0);

        while (corriendo)
        {
            try
            {
                int largo = socket.ReceiveFrom(buffer, ref desde);
                string linea = System.Text.Encoding.ASCII.GetString(buffer, 0, largo);
                string[] partes = linea.Split(';');
                if (partes.Length < 3) continue;

                // InvariantCulture obligatorio: la PC está en español y sin esto
                // "0.35" se interpretaría mal (acá la coma es el separador decimal).
                if (!float.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float lat))
                    continue;

                lateralCrudo = Mathf.Clamp(lat, -1f, 1f);
                wheelieCrudo = partes[1].Trim() == "1";
                detectadoCrudo = partes[2].Trim() == "1";
                datoNuevo = true;
            }
            catch (SocketException)
            {
                // Timeout del Receive (normal) o socket cerrado al salir: seguimos
                // o cortamos según el flag.
            }
            catch (System.ObjectDisposedException)
            {
                break;   // cerramos el socket desde el hilo principal
            }
        }
    }

    void Update()
    {
        // unscaledTime y no time: el juego está en timeScale 0 en el menú y en la
        // pausa, que es justo donde se usa la pantalla de "Probar cámara".
        if (datoNuevo)
        {
            datoNuevo = false;
            Lateral = lateralCrudo;
            Wheelie = wheelieCrudo;
            PersonaDetectada = detectadoCrudo;
            ultimoDato = Time.unscaledTime;
        }

        HaySenal = (Time.unscaledTime - ultimoDato) < SegundosSinSenal;

        if (!HaySenal)
        {
            // Sin señal no dejamos valores viejos pegados: la moto quedaría
            // doblando sola.
            Lateral = 0f;
            Wheelie = false;
            PersonaDetectada = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Detener();
    }

    void OnApplicationQuit()
    {
        Detener();
    }

    void Detener()
    {
        corriendo = false;

        if (socket != null)
        {
            socket.Close();   // desbloquea el ReceiveFrom del hilo
            socket = null;
        }

        if (hilo != null)
        {
            hilo.Join(300);
            hilo = null;
        }
    }
}
