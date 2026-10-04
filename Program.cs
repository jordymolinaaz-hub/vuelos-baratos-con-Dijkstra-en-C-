// Practica #04 - Estructura de Datos (UEA)
// Tema: Encuentro de vuelos baratos con grafos ponderados y el algoritmo de Dijkstra.
// Estructura: grafo dirigido con lista de adyacencia. Requiere .NET 6 o superior.
using System.Diagnostics;

var grafo = new GrafoVuelos();
string ruta = args.Length > 0 ? args[0] : "vuelos.txt";
if (File.Exists(ruta)) grafo.CargarDesdeArchivo(ruta);
else { Console.WriteLine($"No se encontro '{ruta}'."); return; }

while (true)
{
    Console.WriteLine("\n=== SISTEMA DE VUELOS BARATOS ===");
    Console.WriteLine("1. Listar aeropuertos");
    Console.WriteLine("2. Listar vuelos (reporteria completa)");
    Console.WriteLine("3. Vuelos directos desde un aeropuerto");
    Console.WriteLine("4. Ruta mas BARATA entre dos aeropuertos");
    Console.WriteLine("5. Ruta mas RAPIDA entre dos aeropuertos");
    Console.WriteLine("6. Medir tiempo de ejecucion");
    Console.WriteLine("0. Salir");
    Console.Write("Opcion: ");
    switch (Console.ReadLine()?.Trim())
    {
        case "1": grafo.ListarAeropuertos(); break;
        case "2": grafo.ListarVuelos(); break;
        case "3": grafo.VuelosDesde(Pedir("Origen")); break;
        case "4": grafo.MostrarRuta(Pedir("Origen"), Pedir("Destino"), porPrecio: true); break;
        case "5": grafo.MostrarRuta(Pedir("Origen"), Pedir("Destino"), porPrecio: false); break;
        case "6": grafo.Benchmark(); break;
        case "0": return;
        default: Console.WriteLine("Opcion no valida."); break;
    }
}

static string Pedir(string etiqueta)
{
    Console.Write($"{etiqueta} (codigo IATA, ej. UIO): ");
    return (Console.ReadLine() ?? "").Trim().ToUpper();
}

record Vuelo(string Origen, string Destino, int Precio, int Minutos, string Aerolinea);

class GrafoVuelos
{
    // Lista de adyacencia: aeropuerto -> vuelos que salen de el
    private readonly Dictionary<string, List<Vuelo>> ady = new();

    public void AgregarVuelo(Vuelo v)
    {
        if (!ady.ContainsKey(v.Origen)) ady[v.Origen] = new List<Vuelo>();
        if (!ady.ContainsKey(v.Destino)) ady[v.Destino] = new List<Vuelo>();
        ady[v.Origen].Add(v);
    }

    public void CargarDesdeArchivo(string ruta)
    {
        foreach (var linea in File.ReadLines(ruta))
        {
            if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("#")) continue;
            var p = linea.Split(',');
            AgregarVuelo(new Vuelo(p[0].Trim(), p[1].Trim(), int.Parse(p[2]), int.Parse(p[3]), p[4].Trim()));
        }
    }

    // ---------- Reporteria ----------
    public void ListarAeropuertos()
    {
        Console.WriteLine($"Aeropuertos ({ady.Count}):");
        foreach (var a in ady.Keys.OrderBy(x => x))
            Console.WriteLine($"  {a}  -> {ady[a].Count} vuelos de salida");
    }

    public void ListarVuelos()
    {
        Console.WriteLine($"{"Origen",-7}{"Destino",-9}{"Precio",-9}{"Min",-6}Aerolinea");
        foreach (var v in ady.Values.SelectMany(x => x).OrderBy(v => v.Origen).ThenBy(v => v.Destino))
            Console.WriteLine($"{v.Origen,-7}{v.Destino,-9}${v.Precio,-8}{v.Minutos,-6}{v.Aerolinea}");
    }

    public void VuelosDesde(string o)
    {
        if (!ady.ContainsKey(o)) { Console.WriteLine("Aeropuerto no existe."); return; }
        foreach (var v in ady[o].OrderBy(v => v.Precio))
            Console.WriteLine($"  {v.Origen} -> {v.Destino}  ${v.Precio}  {v.Minutos} min  ({v.Aerolinea})");
    }

    // ---------- Dijkstra con cola de prioridad (min-heap) ----------
    // Complejidad: O((V + E) log V)
    public (int costo, List<string> camino)? Dijkstra(string origen, string destino, bool porPrecio)
    {
        if (!ady.ContainsKey(origen) || !ady.ContainsKey(destino)) return null;

        var dist = ady.Keys.ToDictionary(k => k, _ => int.MaxValue);
        var previo = new Dictionary<string, string?>();
        var pq = new PriorityQueue<string, int>();

        dist[origen] = 0;
        pq.Enqueue(origen, 0);

        while (pq.TryDequeue(out var u, out var d))
        {
            if (d > dist[u]) continue;           // entrada obsoleta
            if (u == destino) break;             // ya se encontro el optimo
            foreach (var v in ady[u])
            {
                int peso = porPrecio ? v.Precio : v.Minutos;
                if (dist[u] + peso < dist[v.Destino])
                {
                    dist[v.Destino] = dist[u] + peso;
                    previo[v.Destino] = u;
                    pq.Enqueue(v.Destino, dist[v.Destino]);
                }
            }
        }

        if (dist[destino] == int.MaxValue) return null;
        var camino = new List<string>();
        for (string? n = destino; n != null; n = previo.GetValueOrDefault(n)) camino.Add(n);
        camino.Reverse();
        return (dist[destino], camino);
    }

    public void MostrarRuta(string o, string d, bool porPrecio)
    {
        var r = Dijkstra(o, d, porPrecio);
        if (r == null) { Console.WriteLine("No hay ruta o los codigos no existen."); return; }
        string unidad = porPrecio ? "USD" : "min";
        Console.WriteLine($"Ruta: {string.Join(" -> ", r.Value.camino)}");
        Console.WriteLine($"Total: {r.Value.costo} {unidad}  |  Escalas: {r.Value.camino.Count - 2}");
    }

    public void Benchmark()
    {
        var codigos = ady.Keys.ToList();
        const int repeticiones = 10000;
        var sw = Stopwatch.StartNew();
        int consultas = 0;
        for (int i = 0; i < repeticiones; i++)
            foreach (var a in codigos) { Dijkstra(a, codigos[(i + 3) % codigos.Count], true); consultas++; }
        sw.Stop();
        Console.WriteLine($"{consultas} consultas en {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Promedio por consulta: {sw.Elapsed.TotalMilliseconds * 1000 / consultas:F2} microsegundos");
    }
}
