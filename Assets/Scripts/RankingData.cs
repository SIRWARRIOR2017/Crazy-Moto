using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Datos persistentes del ranking. La estructura de datos es un ScriptableObject,
// pero la lista real de puntajes se guarda en un archivo JSON en disco: un
// ScriptableObject por sí solo NO conserva los cambios hechos en tiempo de
// ejecución dentro del juego compilado, así que el JSON es lo que persiste.
public class RankingData : ScriptableObject
{
    [System.Serializable]
    public class Entrada
    {
        public string nombre;
        public int puntaje;
    }

    [Tooltip("Cuántos puntajes se conservan como máximo (los mejores).")]
    public int maxEntradas = 200;

    // Los datos reales viven en el JSON, no en la instancia del ScriptableObject.
    [System.NonSerialized] private List<Entrada> entradas = new List<Entrada>();
    [System.NonSerialized] private bool cargado = false;

    // ---- Acceso estático (el static sobrevive a la recarga de escena) ----
    private static RankingData instancia;
    public static RankingData Instance
    {
        get
        {
            if (instancia == null)
                instancia = CreateInstance<RankingData>();

            instancia.CargarSiHaceFalta();
            return instancia;
        }
    }

    private string RutaArchivo => Path.Combine(Application.persistentDataPath, "ranking.json");

    // ---- API ----

    public IReadOnlyList<Entrada> Entradas
    {
        get { CargarSiHaceFalta(); return entradas; }
    }

    // Agrega un puntaje. Un jugador = una sola fila: si el nombre ya está, se
    // queda con su MEJOR puntaje (no lo pisa el último). Después reordena de
    // mayor a menor, recorta al máximo y guarda.
    public void Agregar(string nombre, int puntaje)
    {
        CargarSiHaceFalta();

        if (string.IsNullOrWhiteSpace(nombre)) nombre = "Jugador";
        nombre = nombre.Trim();

        Entrada existente = entradas.Find(e => MismoNombre(e.nombre, nombre));
        if (existente != null)
        {
            if (puntaje > existente.puntaje) existente.puntaje = puntaje;
            existente.nombre = nombre;   // adopta la última forma de escribir el nombre
        }
        else
        {
            entradas.Add(new Entrada { nombre = nombre, puntaje = puntaje });
        }

        Ordenar();

        if (entradas.Count > maxEntradas)
            entradas.RemoveRange(maxEntradas, entradas.Count - maxEntradas);

        Guardar();
    }

    // Mejor puesto (1 = primero) del jugador con ese nombre, o -1 si no figura.
    public int PuestoDe(string nombre)
    {
        CargarSiHaceFalta();
        if (string.IsNullOrWhiteSpace(nombre)) return -1;
        nombre = nombre.Trim();

        for (int i = 0; i < entradas.Count; i++)
            if (MismoNombre(entradas[i].nombre, nombre))
                return i + 1;
        return -1;
    }

    private static bool MismoNombre(string a, string b)
    {
        return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
    }

    // ---- Persistencia (JSON) ----

    [System.Serializable]
    private class Envoltorio { public List<Entrada> entradas = new List<Entrada>(); }

    private void CargarSiHaceFalta()
    {
        if (!cargado) Cargar();
    }

    public void Cargar()
    {
        entradas = new List<Entrada>();
        try
        {
            if (File.Exists(RutaArchivo))
            {
                Envoltorio e = JsonUtility.FromJson<Envoltorio>(File.ReadAllText(RutaArchivo));
                if (e != null && e.entradas != null) entradas = e.entradas;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("RankingData: no pude leer el ranking (" + ex.Message + "). Arranco vacío.");
            entradas = new List<Entrada>();
        }

        Deduplicar();   // limpia nombres repetidos de datos viejos
        Ordenar();
        cargado = true;
    }

    // Deja una sola entrada por nombre, con el mejor puntaje.
    private void Deduplicar()
    {
        Dictionary<string, Entrada> mejores = new Dictionary<string, Entrada>(System.StringComparer.OrdinalIgnoreCase);

        foreach (Entrada e in entradas)
        {
            if (string.IsNullOrWhiteSpace(e.nombre)) e.nombre = "Jugador";
            e.nombre = e.nombre.Trim();

            if (!mejores.TryGetValue(e.nombre, out Entrada actual) || e.puntaje > actual.puntaje)
                mejores[e.nombre] = e;
        }

        entradas = new List<Entrada>(mejores.Values);
    }

    public void Guardar()
    {
        try
        {
            File.WriteAllText(RutaArchivo, JsonUtility.ToJson(new Envoltorio { entradas = entradas }, true));
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("RankingData: no pude guardar el ranking (" + ex.Message + ").");
        }
    }

    private void Ordenar()
    {
        entradas.Sort((a, b) => b.puntaje.CompareTo(a.puntaje));
    }

    // Borra el ranking guardado (llamable desde otro script si se quiere un botón de reset).
    public void BorrarTodo()
    {
        entradas = new List<Entrada>();
        cargado = true;
        Guardar();
        Debug.Log("RankingData: ranking borrado.");
    }
}
