using System;
using System.Collections.Generic;
using System.Linq;
using GraphPathfinder.Models;

namespace GraphPathfinder.Algorithms
{
    public static class BellmanFordAlgorithm
    {
        public static AlgorithmResult FindPath(Vertex start, Vertex end, IEnumerable<Vertex> vertices, IEnumerable<Edge> edges, bool isDirectedGraph = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var dist = new Dictionary<Vertex, long>();
            var prev = new Dictionary<Vertex, Vertex?>();
            int verticesVisited = 0;
            int edgeRelaxations = 0;
            foreach (var v in vertices)
                dist[v] = long.MaxValue;
            dist[start] = 0;
            for (int i = 0; i < vertices.Count() - 1; i++)
            {
                foreach (var e in edges)
                {
                    if (e.IsDirected)
                    {
                        if (dist[e.Source] != long.MaxValue)
                        {
                            if (dist[e.Source] + (e.Weight ?? 1) < dist[e.Target])
                            {
                                dist[e.Target] = dist[e.Source] + (e.Weight ?? 1);
                                prev[e.Target] = e.Source;
                                edgeRelaxations++;
                            }
                        }
                    }
                    else
                    {
                        if (dist[e.Source] != long.MaxValue)
                        {
                            if (dist[e.Source] + (e.Weight ?? 1) < dist[e.Target])
                            {
                                dist[e.Target] = dist[e.Source] + (e.Weight ?? 1);
                                prev[e.Target] = e.Source;
                                edgeRelaxations++;
                            }
                        }
                        if (dist[e.Target] != long.MaxValue)
                        {
                            if (dist[e.Target] + (e.Weight ?? 1) < dist[e.Source])
                            {
                                dist[e.Source] = dist[e.Target] + (e.Weight ?? 1);
                                prev[e.Source] = e.Target;
                                edgeRelaxations++;
                            }
                        }
                    }
                }
                verticesVisited++;
            }
            var path = new List<Vertex>();
            var vBellman = end;
            while (prev.ContainsKey(vBellman))
            {
                path.Insert(0, vBellman);
                var next = prev[vBellman];
                if (next == null) break;
                vBellman = next;
            }
            if (path.Count > 0 && path[0] != start)
                path.Insert(0, start);
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
