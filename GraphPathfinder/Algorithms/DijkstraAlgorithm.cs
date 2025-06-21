using GraphPathfinder.Models;

namespace GraphPathfinder.Algorithms
{
    public static class DijkstraAlgorithm
    {
        public static AlgorithmResult FindPath(Vertex start, Vertex end, IEnumerable<Vertex> vertices, IEnumerable<Edge> edges, bool isDirectedGraph = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var dist = new Dictionary<Vertex, long>();
            var prev = new Dictionary<Vertex, Vertex?>();
            var visited = new HashSet<Vertex>();
            int verticesVisited = 0;
            int edgeRelaxations = 0;

            foreach (var v in vertices)
                dist[v] = long.MaxValue;
            dist[start] = 0;

            var pq = new PriorityQueue<Vertex, long>();
            pq.Enqueue(start, 0);

            while (pq.Count > 0)
            {
                var u = pq.Dequeue();

                if (!visited.Add(u))
                    continue;

                verticesVisited++;

                if (u == end)
                    break;

                var neighbors = edges.Where(e => 
                    e.IsDirected 
                        ? e.Source == u && !visited.Contains(e.Target) 
                        : (e.Source == u && !visited.Contains(e.Target)) || (e.Target == u && !visited.Contains(e.Source))
                );

                foreach (var e in neighbors)
                {
                    Vertex neighbor = e.Source == u ? e.Target : e.Source;

                    long alt = dist[u] + (e.Weight ?? 1);
                    if (alt < dist[neighbor])
                    {
                        dist[neighbor] = alt;
                        prev[neighbor] = u;
                        pq.Enqueue(neighbor, alt);
                        edgeRelaxations++;
                    }
                }
            }

            var path = new List<Vertex>();
            if (dist[end] != long.MaxValue)
            {
                var current = end;
                while (prev.ContainsKey(current))
                {
                    path.Insert(0, current);
                    current = prev[current]!;
                }
                if (path.Count == 0 || path[0] != start)
                    path.Insert(0, start);
            }

            stopwatch.Stop();

            return new AlgorithmResult
            {
                Path = path,
                ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                VerticesVisited = verticesVisited,
                EdgeRelaxations = edgeRelaxations
            };
        }
    }
}
