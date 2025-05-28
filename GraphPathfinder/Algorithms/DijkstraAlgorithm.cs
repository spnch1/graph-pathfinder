using System;
using System.Collections.Generic;
using System.Linq;
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
            var unvisited = new HashSet<Vertex>(vertices);
            int verticesVisited = 0;
            int edgeRelaxations = 0;
            foreach (var v in vertices)
                dist[v] = long.MaxValue;
            dist[start] = 0;
            while (unvisited.Count > 0)
            {
                var u = unvisited.OrderBy(x => dist[x]).First();
                unvisited.Remove(u);
                verticesVisited++;
                if (u == end || dist[u] == long.MaxValue)
                    break;
                foreach (var e in edges)
                {
                    if (e.IsDirected)
                    {
                        if (e.Source == u && unvisited.Contains(e.Target))
                        {
                            var alt = dist[u] + (e.Weight ?? 1);
                            if (alt < dist[e.Target])
                            {
                                dist[e.Target] = alt;
                                prev[e.Target] = u;
                                edgeRelaxations++;
                            }
                        }
                    }
                    else
                    {
                        if (e.Source == u && unvisited.Contains(e.Target))
                        {
                            var alt = dist[u] + (e.Weight ?? 1);
                            if (alt < dist[e.Target])
                            {
                                dist[e.Target] = alt;
                                prev[e.Target] = u;
                                edgeRelaxations++;
                            }
                        }
                        if (e.Target == u && unvisited.Contains(e.Source))
                        {
                            var alt = dist[u] + (e.Weight ?? 1);
                            if (alt < dist[e.Source])
                            {
                                dist[e.Source] = alt;
                                prev[e.Source] = u;
                                edgeRelaxations++;
                            }
                        }
                    }
                }
            }
            var path = new List<Vertex>();
            var vDijkstra = end;
            while (prev.ContainsKey(vDijkstra))
            {
                path.Insert(0, vDijkstra);
                var next = prev[vDijkstra];
                if (next == null) break;
                vDijkstra = next;
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
