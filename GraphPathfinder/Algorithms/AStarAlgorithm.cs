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

            var openSet = new PriorityQueue<Vertex, double>();
            openSet.Enqueue(start, heuristic(start, end));

            var cameFrom = new Dictionary<Vertex, Vertex?>();
            var gScore = vertices.ToDictionary(v => v, v => double.PositiveInfinity);
            gScore[start] = 0;

            var fScore = vertices.ToDictionary(v => v, v => double.PositiveInfinity);
            fScore[start] = heuristic(start, end);

            var closedSet = new HashSet<Vertex>();

            int verticesVisited = 0;
            int edgeRelaxations = 0;

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (!closedSet.Add(current))
                    continue;

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

                var neighbors = edges.Where(e =>
                    e.IsDirected
                        ? e.Source == current && !closedSet.Contains(e.Target)
                        : (e.Source == current && !closedSet.Contains(e.Target)) || (e.Target == current && !closedSet.Contains(e.Source))
                );

                foreach (var e in neighbors)
                {
                    Vertex neighbor = e.Source == current ? e.Target : e.Source;
                    double tentativeGScore = gScore[current] + (e.Weight ?? 1);

                    if (tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + heuristic(neighbor, end);
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                        edgeRelaxations++;
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
