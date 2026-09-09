using UnityEngine;

// Anima la moto del jugador: hace girar las dos ruedas según la velocidad del
// juego y mueve el manubrio de izquierda a derecha según la entrada lateral.
// Va en `Cuerpo/MotoModelo` (el wrapper del modelo `DirtBike`).
//
// El modelo `DirtBike` trae `wheel_f`, `wheel_r` (ruedas) y `handle_main` (todo
// el conjunto manubrio + horquilla + rueda delantera) como transforms separados.
// El pivote de las ruedas NO está en su centro (rig de Blender). Para girarlas
// sin que se vayan volando: se guarda la pose inicial, se acumula UN ángulo
// escalar, y cada frame se resetea la rueda y se aplica ese ángulo total con
// RotateAround sobre el centro visual. Nada de `Rotate` incremental (acumula
// error y la rueda se espirala).
public class MotoAnimada : MonoBehaviour
{
    [Header("Ruedas")]
    [Tooltip("Radio de la rueda en metros. Más grande = gira más lento.")]
    public float radioRueda = 0.32f;
    [Tooltip("Multiplica la velocidad de giro visual (bajalo si estroboscopia).")]
    [Range(0.05f, 1f)] public float factorVisual = 0.5f;

    [Header("Manubrio")]
    public float anguloMaxDireccion = 16f;
    public float suavizadoDireccion = 0.10f;

    private Rueda del, tras;
    private Transform manubrio;
    private PlayerController jugador;
    private Quaternion manubrioRotInicial;
    private Vector3 manubrioPosInicial;
    private float direccionActual, velDireccion;

    class Rueda
    {
        public Transform t;
        public Quaternion rotInicial;
        public Vector3 posInicial;
        public Vector3 centroLocal;   // centro visual en local de la rueda
        public float angulo;          // acumulado

        public Rueda(Transform tr)
        {
            t = tr;
            rotInicial = tr.localRotation;
            posInicial = tr.localPosition;
            centroLocal = tr.InverseTransformPoint(CentroideVertices(tr));
        }

        // Centroide de los vértices de la rueda: para una rueda (simétrica de
        // revolución) cae sobre el eje del semieje. El centro de la bounding box
        // NO — se corre por el disco de freno, la corona, etc., y eso hace
        // temblar la rueda al girar.
        static Vector3 CentroideVertices(Transform wheel)
        {
            Vector3 suma = Vector3.zero;
            int n = 0;
            foreach (MeshFilter mf in wheel.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                Vector3[] v = mf.sharedMesh.vertices;
                int paso = Mathf.Max(1, v.Length / 300);
                for (int i = 0; i < v.Length; i += paso) { suma += mf.transform.TransformPoint(v[i]); n++; }
            }
            return n > 0 ? suma / n : wheel.position;
        }

        public void Aplicar(float delta, Vector3 ejeMundo)
        {
            angulo = Mathf.Repeat(angulo + delta, 360f);   // mod 360: no explota nunca
            t.localRotation = rotInicial;
            t.localPosition = posInicial;
            Vector3 pivote = t.TransformPoint(centroLocal);
            t.RotateAround(pivote, ejeMundo, angulo);
        }
    }

    void Start()
    {
        Transform tf = BuscarHijo("wheel_f");
        Transform tr = BuscarHijo("wheel_r");
        manubrio = BuscarHijo("handle_main");
        jugador = FindAnyObjectByType<PlayerController>();

        if (tf != null) del = new Rueda(tf);
        if (tr != null) tras = new Rueda(tr);

        if (manubrio != null)
        {
            manubrioRotInicial = manubrio.localRotation;
            manubrioPosInicial = manubrio.localPosition;
        }

        if (del == null || tras == null || manubrio == null)
            Debug.LogWarning("MotoAnimada: no encontré wheel_f / wheel_r / handle_main en el modelo.");
    }

    void Update()
    {
        if (Time.deltaTime <= 0f) return;

        // --- Manubrio primero (mueve wheel_f, que es su hijo) ---
        if (manubrio != null)
        {
            float entrada = jugador != null ? jugador.EntradaLateral : 0f;
            float objetivo = entrada * anguloMaxDireccion;
            direccionActual = Mathf.SmoothDamp(direccionActual, objetivo, ref velDireccion, suavizadoDireccion);

            manubrio.localRotation = manubrioRotInicial;
            manubrio.localPosition = manubrioPosInicial;
            manubrio.RotateAround(manubrio.position, transform.up, direccionActual);
        }

        // --- Ruedas ---
        float velocidad = GameManager.Instance != null ? GameManager.Instance.velocidadActual : 0f;
        float delta = velocidad / Mathf.Max(radioRueda, 0.05f) * Mathf.Rad2Deg * factorVisual * Time.deltaTime;

        del?.Aplicar(delta, transform.right);
        tras?.Aplicar(delta, transform.right);
    }

    Transform BuscarHijo(string nombre)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>())
            if (t.name == nombre) return t;
        return null;
    }
}
