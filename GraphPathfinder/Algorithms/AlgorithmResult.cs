using System.Collections.Generic;
using GraphPathfinder.Models;

namespace GraphPathfinder.Algorithms
{
    public class AlgorithmResult
    {
        public List<Vertex> Path { get; set; } = new();
        public double ExecutionTimeSeconds { get; set; }
        public int VerticesVisited { get; set; }
        public int EdgeRelaxations { get; set; }
    }
}
