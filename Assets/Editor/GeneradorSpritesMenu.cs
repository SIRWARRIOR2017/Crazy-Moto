using System.IO;
using UnityEditor;
using UnityEngine;

// Dibuja por código los PNG del fondo del menú ("Ruta de noche") y los deja como
// sprites de verdad en Assets/Resources/Sprites/. El juego después carga esos archivos
// como
// cualquier otro sprite: esto corre UNA vez, en el Editor, no en la partida.
//
// Está acá y no en Assets/Scripts/ porque los scripts que usan UnityEditor tienen
// que vivir en una carpeta llamada "Editor" o el juego no compila al buildear.
//
// Para regenerarlos (por ejemplo si cambiás un color): menú Crazy Moto > Generar
// sprites del menú.
public static class GeneradorSpritesMenu
{
    private const string Carpeta = "Assets/Resources/Sprites";

    // --- Paleta de "Ruta de noche" (la del boceto) ---
    private static readonly Color NocheArriba = Hex("#080A1E");
    private static readonly Color NocheMedio1 = Hex("#141043");
    private static readonly Color NocheMedio2 = Hex("#2C1454");
    private static readonly Color NocheMedio3 = Hex("#150B2C");
    private static readonly Color NocheAbajo = Hex("#08061A");

    private static readonly Color SolArriba = Hex("#FFE3A3");
    private static readonly Color SolMedio1 = Hex("#FFB454");
    private static readonly Color SolMedio2 = Hex("#FF7A5A");
    private static readonly Color SolAbajo = Hex("#FF3D9A");

    private static readonly Color Cyan = Hex("#37E0FF");
    private static readonly Color Magenta = Hex("#FF3D9A");

    [MenuItem("Crazy Moto/Generar sprites del menú")]
    public static void GenerarTodo()
    {
        if (!Directory.Exists(Carpeta))
            Directory.CreateDirectory(Carpeta);

        GuardarSprite("FondoNoche.png", FondoNoche(), 0);
        GuardarSprite("SolRetro.png", SolRetro(), 0);
        GuardarSprite("HaloSol.png", HaloSol(), 0);
        GuardarSprite("GrillaNeon.png", GrillaNeon(), 0);
        GuardarSprite("ResplandorHorizonte.png", ResplandorHorizonte(), 0);
        GuardarSprite("LineaHorizonte.png", LineaHorizonte(), 0);
        GuardarSprite("Vineta.png", Vineta(), 0);
        GuardarSprite("MarcoUI.png", MarcoUI(), 6);

        AssetDatabase.Refresh();
        Debug.Log("Sprites del menú generados en " + Carpeta);
    }

    // ---- Los dibujos ----

    // Degradado vertical de toda la pantalla. Angosto a propósito: se estira en X
    // sin perder nada, y pesa unos pocos KB.
    private static Texture2D FondoNoche()
    {
        const int ancho = 8, alto = 512;
        Texture2D t = NuevaTextura(ancho, alto);

        for (int y = 0; y < alto; y++)
        {
            // v = 0 arriba de la pantalla, 1 abajo.
            float v = 1f - (float)y / (alto - 1);
            Color c = Rampa(v,
                new[] { 0f, 0.36f, 0.53f, 0.74f, 1f },
                new[] { NocheArriba, NocheMedio1, NocheMedio2, NocheMedio3, NocheAbajo });

            for (int x = 0; x < ancho; x++)
                t.SetPixel(x, y, c);
        }

        t.Apply();
        return t;
    }

    // El sol: círculo con degradado cálido y, de la mitad para abajo, cortado en
    // franjas horizontales (el truco retro de toda la vida).
    private static Texture2D SolRetro()
    {
        const int lado = 512;
        Texture2D t = NuevaTextura(lado, lado);
        float radio = lado * 0.5f - 1f;
        float centro = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            float v = 1f - (float)y / (lado - 1);   // 0 arriba, 1 abajo

            // Franjas: sólo de la mitad del sol para abajo, cada vez más juntas.
            bool cortado = false;
            if (v > 0.5f)
            {
                float d = (v - 0.5f) / 0.5f;                  // 0..1 hacia abajo
                float periodo = Mathf.Lerp(34f, 16f, d);      // arriba anchas, abajo finitas
                float corte = periodo * 0.36f;
                cortado = Mathf.Repeat(lado - 1 - y, periodo) < corte;
            }

            Color c = Rampa(v,
                new[] { 0f, 0.38f, 0.68f, 1f },
                new[] { SolArriba, SolMedio1, SolMedio2, SolAbajo });

            for (int x = 0; x < lado; x++)
            {
                float dx = x - centro + 0.5f;
                float dy = y - centro + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Borde suave de 1,5 px para que no quede escalonado.
                float alfa = Mathf.Clamp01((radio - dist) / 1.5f);
                if (cortado) alfa = 0f;

                t.SetPixel(x, y, new Color(c.r, c.g, c.b, alfa));
            }
        }

        t.Apply();
        return t;
    }

    // Halo alrededor del sol: un degradado radial que se apaga. Da la sensación de
    // que el sol "ilumina" el cielo.
    private static Texture2D HaloSol()
    {
        const int lado = 256;
        Texture2D t = NuevaTextura(lado, lado);
        float centro = lado * 0.5f;
        float radio = lado * 0.5f;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                float dx = (x - centro + 0.5f) / radio;
                float dy = (y - centro + 0.5f) / radio;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alfa = Mathf.Clamp01(1f - dist);
                alfa = alfa * alfa * alfa;   // cae rápido, queda un halo suave y no una mancha

                Color c = Color.Lerp(Magenta, SolMedio2, Mathf.Clamp01(1f - dist));
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, alfa * 0.55f));
            }
        }

        t.Apply();
        return t;
    }

    // La grilla en perspectiva del piso. En vez de deformar un cuadriculado con un
    // transform (que en UI se complica), la perspectiva se calcula acá: la fila de
    // abajo es la más cercana, y hacia arriba todo se junta hacia el punto de fuga.
    private static Texture2D GrillaNeon()
    {
        const int ancho = 1024, alto = 256;
        Texture2D t = NuevaTextura(ancho, alto);
        Limpiar(t);

        float centroX = ancho * 0.5f;
        const float separacionCerca = 190f;   // separación de las líneas verticales abajo de todo
        const int columnas = 9;               // a cada lado del centro
        const float grosor = 2.2f;

        for (int y = 0; y < alto; y++)
        {
            // cerca = 1 abajo (fila más próxima), tiende a 0 arriba (horizonte).
            float cerca = (float)(y + 1) / alto;
            cerca = 1f - cerca;
            if (cerca <= 0.001f) continue;

            float alfa = Mathf.Pow(cerca, 0.75f) * 0.30f;   // se apaga hacia el horizonte

            for (int i = -columnas; i <= columnas; i++)
            {
                float x = centroX + i * separacionCerca * cerca;
                Pintar(t, x, y, grosor, Cyan, alfa);
            }
        }

        // Líneas horizontales: en perspectiva caen como 1/n, así que se amontonan
        // hacia arriba solas.
        for (int n = 1; n <= 26; n++)
        {
            float cerca = 1f / n;
            float y = alto - 1 - (alto - 1) * cerca;
            float alfa = Mathf.Pow(cerca, 0.75f) * 0.26f;

            for (int x = 0; x < ancho; x++)
                PintarFila(t, x, y, grosor, Cyan, alfa);
        }

        t.Apply();
        return t;
    }

    // Resplandor magenta que sube desde el horizonte.
    private static Texture2D ResplandorHorizonte()
    {
        const int ancho = 8, alto = 256;
        Texture2D t = NuevaTextura(ancho, alto);

        for (int y = 0; y < alto; y++)
        {
            float v = 1f - (float)y / (alto - 1);          // 0 arriba, 1 abajo
            float alfa = Mathf.Pow(1f - v, 1.6f) * 0.30f;  // fuerte arriba (el horizonte), se apaga abajo

            for (int x = 0; x < ancho; x++)
                t.SetPixel(x, y, new Color(Magenta.r, Magenta.g, Magenta.b, alfa));
        }

        t.Apply();
        return t;
    }

    // La línea del horizonte: cyan en el medio, que se desvanece hacia los dos
    // costados para que no termine cortada contra el borde de la pantalla.
    private static Texture2D LineaHorizonte()
    {
        const int ancho = 512, alto = 4;
        Texture2D t = NuevaTextura(ancho, alto);

        for (int x = 0; x < ancho; x++)
        {
            float u = (float)x / (ancho - 1);              // 0 izquierda, 1 derecha
            float desdeElCentro = Mathf.Abs(u - 0.5f) * 2f;   // 0 en el medio, 1 en las puntas
            float alfa = Mathf.Pow(1f - desdeElCentro, 1.4f);

            for (int y = 0; y < alto; y++)
                t.SetPixel(x, y, new Color(Cyan.r, Cyan.g, Cyan.b, alfa));
        }

        t.Apply();
        return t;
    }

    // Vineta: transparente en el centro y oscura hacia los bordes. Va encima de
    // todo el fondo y antes del contenido. Sin esto el sol y el resplandor
    // compiten con el texto y el menu se vuelve dificil de leer.
    private static Texture2D Vineta()
    {
        const int lado = 256;
        Texture2D t = NuevaTextura(lado, lado);

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                // Centrada un poco mas arriba de la mitad, como en el boceto.
                float dx = (x - lado * 0.5f + 0.5f) / (lado * 0.5f);
                float dy = (y - lado * 0.45f + 0.5f) / (lado * 0.5f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Arranca a oscurecer recien pasado el 45% del radio.
                float alfa = Mathf.Clamp01((dist - 0.45f) / 0.85f);
                alfa = alfa * alfa * 0.72f;

                t.SetPixel(x, y, new Color(NocheAbajo.r, NocheAbajo.g, NocheAbajo.b, alfa));
            }
        }

        t.Apply();
        return t;
    }

    // Marco de 1 px, hueco por dentro. Va en modo Sliced, asi el mismo archivo
    // sirve para el borde de un botón y para el del panel de ranking sin deformarse.
    private static Texture2D MarcoUI()
    {
        const int lado = 32;
        Texture2D t = NuevaTextura(lado, lado);
        Limpiar(t);

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                bool esBorde = x < 2 || y < 2 || x >= lado - 2 || y >= lado - 2;
                if (esBorde)
                    t.SetPixel(x, y, Color.white);
            }
        }

        t.Apply();
        return t;
    }

    // ---- Herramientas de dibujo ----

    private static Texture2D NuevaTextura(int ancho, int alto)
    {
        return new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
    }

    private static void Limpiar(Texture2D t)
    {
        Color vacio = new Color(0f, 0f, 0f, 0f);
        Color[] pix = new Color[t.width * t.height];
        for (int i = 0; i < pix.Length; i++) pix[i] = vacio;
        t.SetPixels(pix);
    }

    // Pinta una línea vertical de ancho 'grosor' centrada en x, sobre la fila y.
    private static void Pintar(Texture2D t, float x, int y, float grosor, Color color, float alfa)
    {
        int desde = Mathf.FloorToInt(x - grosor * 0.5f);
        int hasta = Mathf.CeilToInt(x + grosor * 0.5f);

        for (int px = desde; px <= hasta; px++)
        {
            if (px < 0 || px >= t.width) continue;

            float cobertura = 1f - Mathf.Clamp01(Mathf.Abs(px + 0.5f - x) / (grosor * 0.5f + 0.5f));
            if (cobertura <= 0f) continue;

            Mezclar(t, px, y, color, alfa * cobertura);
        }
    }

    // Igual pero para una línea horizontal: se difumina en Y.
    private static void PintarFila(Texture2D t, int x, float y, float grosor, Color color, float alfa)
    {
        int desde = Mathf.FloorToInt(y - grosor * 0.5f);
        int hasta = Mathf.CeilToInt(y + grosor * 0.5f);

        for (int py = desde; py <= hasta; py++)
        {
            if (py < 0 || py >= t.height) continue;

            float cobertura = 1f - Mathf.Clamp01(Mathf.Abs(py + 0.5f - y) / (grosor * 0.5f + 0.5f));
            if (cobertura <= 0f) continue;

            Mezclar(t, x, py, color, alfa * cobertura);
        }
    }

    // Suma color sobre lo que ya había (las líneas se cruzan y no deben pisarse).
    private static void Mezclar(Texture2D t, int x, int y, Color color, float alfa)
    {
        Color previo = t.GetPixel(x, y);
        float nuevaAlfa = Mathf.Clamp01(previo.a + alfa);
        t.SetPixel(x, y, new Color(color.r, color.g, color.b, nuevaAlfa));
    }

    // Degradado de varios tramos: devuelve el color en la posición v (0..1).
    private static Color Rampa(float v, float[] paradas, Color[] colores)
    {
        v = Mathf.Clamp01(v);

        for (int i = 0; i < paradas.Length - 1; i++)
        {
            if (v <= paradas[i + 1])
            {
                float t = Mathf.InverseLerp(paradas[i], paradas[i + 1], v);
                return Color.Lerp(colores[i], colores[i + 1], t);
            }
        }

        return colores[colores.Length - 1];
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    // ---- Guardado e importación ----

    private static void GuardarSprite(string nombre, Texture2D textura, int borde)
    {
        string ruta = Path.Combine(Carpeta, nombre).Replace('\\', '/');
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null)
        {
            Debug.LogWarning("GeneradorSpritesMenu: no pude configurar la importación de " + nombre);
            return;
        }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Bilinear;
        // Sin comprimir: los degradados con compresión quedan con bandas feas.
        imp.textureCompression = TextureImporterCompression.Uncompressed;

        if (borde > 0)
            imp.spriteBorder = new Vector4(borde, borde, borde, borde);

        imp.SaveAndReimport();
    }
}
