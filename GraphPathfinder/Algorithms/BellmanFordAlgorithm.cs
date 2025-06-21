using GraphPathfinder.Models;

namespace GraphPathfinder.Algorithms
{
    public static class BellmanFordAlgorithm
    {
        public static AlgorithmResult FindPath(Vertex start, Vertex end, IEnumerable<Vertex> vertices, IEnumerable<Edge> edges, bool isDirectedGraph = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var dist = vertices.ToDictionary(v => v, v => long.MaxValue);
            var prev = new Dictionary<Vertex, Vertex?>();
            int verticesVisited = 0;
            int edgeRelaxations = 0;
            bool hasNegativeCycle = false;

            dist[start] = 0;
            var vertexList = vertices.ToList();

            for (int i = 0; i < vertexList.Count - 1; i++)
            {
                bool anyRelaxation = false;

                foreach (var e in edges)
                {
                    if (e.Weight == null) continue;

                    if (e.IsDirected || isDirectedGraph)
                    {
                        if (dist[e.Source] != long.MaxValue && dist[e.Source] + e.Weight.Value < dist[e.Target])
                        {
                            dist[e.Target] = dist[e.Source] + e.Weight.Value;
                            prev[e.Target] = e.Source;
                            edgeRelaxations++;
                            anyRelaxation = true;
                        }
                    }
                    else
                    {
                        if (dist[e.Source] != long.MaxValue && dist[e.Source] + e.Weight.Value < dist[e.Target])
                        {
                            dist[e.Target] = dist[e.Source] + e.Weight.Value;
                            prev[e.Target] = e.Source;
                            edgeRelaxations++;
                            anyRelaxation = true;
                        }
                        if (dist[e.Target] != long.MaxValue && dist[e.Target] + e.Weight.Value < dist[e.Source])
                        {
                            dist[e.Source] = dist[e.Target] + e.Weight.Value;
                            prev[e.Source] = e.Target;
                            edgeRelaxations++;
                            anyRelaxation = true;
                        }
                    }
                }

                verticesVisited++;
                if (!anyRelaxation) break;
            }

            foreach (var e in edges)
            {
                if (e.Weight == null) continue;

                if (e.IsDirected || isDirectedGraph)
                {
                    if (dist[e.Source] != long.MaxValue && dist[e.Source] + e.Weight.Value < dist[e.Target])
                    {
                        hasNegativeCycle = true;
                        break;
                    }
                }
                else
                {
                    if ((dist[e.Source] != long.MaxValue && dist[e.Source] + e.Weight.Value < dist[e.Target]) ||
                        (dist[e.Target] != long.MaxValue && dist[e.Target] + e.Weight.Value < dist[e.Source]))
                    {
                        hasNegativeCycle = true;
                        break;
                    }
                }
            }

            var path = new List<Vertex>();
            if (!hasNegativeCycle && prev.ContainsKey(end))
            {
                var current = end;
                var visitedInPath = new HashSet<Vertex>();
                while (current != null && !visitedInPath.Contains(current) && prev.ContainsKey(current))
                {
                    path.Insert(0, current);
                    visitedInPath.Add(current);
                    current = prev[current];
                }

                if (current != null && visitedInPath.Contains(current))
                {
                    int cycleStart = path.IndexOf(current);
                    var cycle = path.Skip(cycleStart).ToList();
                    cycle.Add(current);

                    stopwatch.Stop();
                    return new AlgorithmResult
                    {
                        Path = new List<Vertex>(),
                        ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                        VerticesVisited = verticesVisited,
                        EdgeRelaxations = edgeRelaxations,
                        HasNegativeCycle = true,
                        NegativeCycle = cycle,
                        StatusMessage = $"No shortest path exists due to a negative weight cycle: {string.Join(" -> ", cycle.Select(v => v.Id))} -> {cycle[0].Id}"
                    };
                }

                if (path.Count == 0 || path[0] != start)
                    path.Insert(0, start);
            }

            stopwatch.Stop();
            return new AlgorithmResult
            {
                Path = hasNegativeCycle ? new List<Vertex>() : path,
                ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                VerticesVisited = verticesVisited,
                EdgeRelaxations = edgeRelaxations,
                HasNegativeCycle = hasNegativeCycle,
                NegativeCycle = hasNegativeCycle ? FindNegativeCycle(vertices, edges, dist, prev, isDirectedGraph) : null
            };
        }

        private static List<Vertex>? FindNegativeCycle(IEnumerable<Vertex> vertices, IEnumerable<Edge> edges,
            Dictionary<Vertex, long> dist, Dictionary<Vertex, Vertex?> prev, bool isDirectedGraph)
        {
            var relaxedEdges = edges.Where(e =>
                e.Weight != null &&
                ((e.IsDirected || isDirectedGraph) ? prev.ContainsKey(e.Target) && prev[e.Target] == e.Source
                : (prev.ContainsKey(e.Target) && prev[e.Target] == e.Source) || (prev.ContainsKey(e.Source) && prev[e.Source] == e.Target))
            ).ToList();

            var visited = new HashSet<Vertex>();
            var recursionStack = new HashSet<Vertex>();
            var parent = new Dictionary<Vertex, Vertex>();

            foreach (var v in vertices)
            {
                if (!visited.Contains(v))
                {
                    var cycle = FindCycleDfs(v, visited, recursionStack, parent, relaxedEdges, isDirectedGraph);
                    if (cycle != null)
                        return cycle;
                }
            }
            return null;
        }

        private static List<Vertex>? FindCycleDfs(Vertex v, HashSet<Vertex> visited, HashSet<Vertex> recursionStack,
            Dictionary<Vertex, Vertex> parent, List<Edge> edges, bool isDirectedGraph)
        {
            visited.Add(v);
            recursionStack.Add(v);

            foreach (var e in edges)
            {
                Vertex? next = null;
                if (e.Source == v && (e.IsDirected || isDirectedGraph || !recursionStack.Contains(e.Target)))
                {
                    next = e.Target;
                }
                else if (!e.IsDirected && !isDirectedGraph && e.Target == v && !recursionStack.Contains(e.Source))
                {
                    next = e.Source;
                }

                if (next != null)
                {
                    if (!visited.Contains(next))
                    {
                        parent[next] = v;
                        var cycle = FindCycleDfs(next, visited, recursionStack, parent, edges, isDirectedGraph);
                        if (cycle != null)
                            return cycle;
                    }
                    else if (recursionStack.Contains(next))
                    {
                        var cycle = new List<Vertex> { next };
                        for (var u = v; u != next && u != null; u = parent.ContainsKey(u) ? parent[u] : null)
                        {
                            cycle.Add(u);
                        }
                        cycle.Reverse();
                        return cycle;
                    }
                }
            }

            recursionStack.Remove(v);
            return null;
        }
    }
}
