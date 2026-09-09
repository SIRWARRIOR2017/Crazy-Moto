using System.Collections.Generic;
using UnityEngine;

// Hace girar las ruedas de un auto de tráfico según lo que se mueve. Va en la
// raíz del prefab del auto (`Bugatti.prefab`, `McLaren.prefab`). Mide la
// velocidad sola por cuánto se desplazó entre frames.
//
// Detalles del rig de Blender:
//   - a veces hay un transform "Wheel" adentro de otro "Wheel": se toma solo el
//     de más afuera (si no, la rueda gira dos veces).
//   - el pivote NO está en el centro de la rueda: se gira con RotateAround sobre
//     el centro visual (bounds de sus mallas).
//   - NO se usa `Rotate` incremental: acumula error y la rueda se va en espiral.
//     Se guarda la pose inicial, se acumula un ángulo escalar mod 360, y cada
//     frame se resetea y se aplica el total.
public class RuedasAuto : MonoBehaviour
{
    [Tooltip("Radio aproximado de la rueda, en metros. Si es 0 se calcula solo.")]
    public float radioRueda = 0f;
    [Tooltip("Multiplica la velocidad de giro visual (bajalo si estroboscopia).")]
    [Range(0.05f, 1f)] public float factorVisual = 0.5f;

    private readonly List<Rueda> ruedas = new List<Rueda>();
    private Vector3 posAnterior;

    class Rueda
    {
        public Transform t;
        public Quaternion rotInicial;
        public Vector3 posInicial;
        public Vector3 centroLocal;
        public float angulo;

        public Rueda(Transform tr)
        {
            t = tr;
            rotInicial = tr.localRotation;
            posInicial = tr.localPosition;
            centroLocal = tr.InverseTransformPoint(CentroideVertices(tr));
        }

        // Centroide de los vértices: cae sobre el eje del semieje. El centro de
        // la bounding box no, y eso hace temblar la rueda.
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
            angulo = Mathf.Repeat(angulo + delta, 360f);
            t.localRotation = rotInicial;
            t.localPosition = posInicial;
            t.RotateAround(t.TransformPoint(centroLocal), ejeMundo, angulo);
        }
    }

    void Awake()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!EsRueda(t)) continue;
            if (t.parent != null && EsRueda(t.parent)) continue;
            if (t.GetComponentsInChildren<MeshFilter>().Length == 0) continue;

            ruedas.Add(new Rueda(t));
        }

        if (radioRueda <= 0f) radioRueda = EstimarRadio();
    }

    void OnEnable()
    {
        posAnterior = transform.position;   // el pool reubica el auto de golpe al reciclarlo
    }

    void Update()
    {
        if (Time.deltaTime <= 0f || ruedas.Count == 0) return;

        float velocidad = (transform.position - posAnterior).magnitude / Time.deltaTime;
        posAnterior = transform.position;

        if (velocidad <= 0.01f) return;
        velocidad = Mathf.Min(velocidad, 200f);

        float delta = velocidad / Mathf.Max(radioRueda, 0.05f) * Mathf.Rad2Deg * factorVisual * Time.deltaTime;

        foreach (Rueda r in ruedas)
            r.Aplicar(delta, transform.right);
    }

    static bool EsRueda(Transform t)
    {
        string n = t.name.ToLowerInvariant();
        return n.Contains("wheel") && !n.Contains("brake");
    }

    static Bounds? BoundsDeMallas(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return null;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    float EstimarRadio()
    {
        foreach (Rueda r in ruedas)
        {
            Bounds? b = BoundsDeMallas(r.t);
            if (b != null) return Mathf.Clamp(b.Value.size.y * 0.5f, 0.2f, 0.6f);
        }
        return 0.35f;
    }
}
