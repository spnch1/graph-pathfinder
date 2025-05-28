using System;
using System.Collections.Generic;
using System.Linq;
using GraphPathfinder.Models;

namespace GraphPathfinder.Algorithms
{
    public static class AStarAlgorithm
    {
        public static AlgorithmResult FindPath(Vertex start, Vertex end, IEnumerable<Vertex> vertices, IEnumerable<Edge> edges, Func<Vertex, Vertex, double> heuristic, bool isDirectedGraph = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var openSet = new HashSet<Vertex> { start };
            var cameFrom = new Dictionary<Vertex, Vertex?>();
            var gScore = vertices.ToDictionary(v => v, v => double.PositiveInfinity);
            gScore[start] = 0;
            var fScore = vertices.ToDictionary(v => v, v => double.PositiveInfinity);
            fScore[start] = heuristic(start, end);
            int verticesVisited = 0;
            int edgeRelaxations = 0;
            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(v => fScore[v]).First();
                verticesVisited++;
                if (current == end)
                {
                    var path = new List<Vertex>();
                    var vAStar = end;
                    while (cameFrom.ContainsKey(vAStar))
                    {
                        path.Insert(0, vAStar);
                        var next = cameFrom[vAStar];
                        if (next == null) break;
                        vAStar = next;
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
                openSet.Remove(current);
                foreach (var e in edges)
                {
                    if (e.IsDirected)
                    {
                        if (e.Source == current)
                        {
                            var tentative_gScore = gScore[current] + (e.Weight ?? 1);
                            if (tentative_gScore < gScore[e.Target])
                            {
                                cameFrom[e.Target] = current;
                                gScore[e.Target] = tentative_gScore;
                                fScore[e.Target] = gScore[e.Target] + heuristic(e.Target, end);
                                openSet.Add(e.Target);
                                edgeRelaxations++;
                            }
                        }
                    }
                    else
                    {
                        if (e.Source == current)
                        {
                            var tentative_gScore = gScore[current] + (e.Weight ?? 1);
                            if (tentative_gScore < gScore[e.Target])
                            {
                                cameFrom[e.Target] = current;
                                gScore[e.Target] = tentative_gScore;
                                fScore[e.Target] = gScore[e.Target] + heuristic(e.Target, end);
                                openSet.Add(e.Target);
                                edgeRelaxations++;
                            }
                        }
                        if (e.Target == current)
                        {
                            var tentative_gScore = gScore[current] + (e.Weight ?? 1);
                            if (tentative_gScore < gScore[e.Source])
                            {
                                cameFrom[e.Source] = current;
                                gScore[e.Source] = tentative_gScore;
                                fScore[e.Source] = gScore[e.Source] + heuristic(e.Source, end);
                                openSet.Add(e.Source);
                                edgeRelaxations++;
                            }
                        }
                    }
                }
            }
            stopwatch.Stop();
            return new AlgorithmResult
            {
                Path = new List<Vertex>(),
                ExecutionTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                VerticesVisited = verticesVisited,
                EdgeRelaxations = edgeRelaxations
            };
        }
    }
}
