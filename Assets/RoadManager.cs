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
        for (int i = 0; i < cantidadTramos; i++)
            CrearTramo();
    }

    void Update()
    {
        // Si el tramo más viejo quedó atrás del jugador, lo reubico adelante
        if (tramos.Count > 0 && jugador.position.z - tramos[0].position.z > largoTramo)
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