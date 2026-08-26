using UnityEngine;
using System.Collections.Generic;

public class RoadManager : MonoBehaviour
{
    public Transform jugador;
    public GameObject prefabTramo;
    public float largoTramo = 30f;   // largo en Z de cada tramo
    public int cantidadTramos = 5;

    private List<Transform> tramos = new List<Transform>();
    private float zSpawn = 0f;

    void Start()
    {
        if (jugador == null || prefabTramo == null)
        {
            Debug.LogError("RoadManager: falta asignar 'jugador' o 'prefabTramo' en el Inspector.");
            enabled = false;
            return;
        }

        for (int i = 0; i < cantidadTramos; i++)
            CrearTramo();
    }

    void Update()
    {
        // Uso "while" (no "if"): si en un frame el jugador avanza más de un tramo
        // entero (pico de lag, o si a futuro se sube la velocidad), reubico todos
        // los tramos que hagan falta en vez de quedarme un frame atrás.
        while (tramos.Count > 0 && jugador.position.z - tramos[0].position.z > largoTramo)
        {
            Transform t = tramos[0];
            tramos.RemoveAt(0);
            t.position = new Vector3(0, 0, zSpawn);
            zSpawn += largoTramo;
            tramos.Add(t);
        }
    }

    void CrearTramo()
    {
        GameObject go = Instantiate(prefabTramo, new Vector3(0, 0, zSpawn), Quaternion.identity);
        tramos.Add(go.transform);
        zSpawn += largoTramo;
    }
}