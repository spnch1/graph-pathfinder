using GraphPathfinder.Models;

namespace GraphPathfinder.Algorithms
{
    public class AlgorithmResult
    {
        public List<Vertex> Path { get; set; } = new();
        public double ExecutionTimeSeconds { get; set; }
        public int VerticesVisited { get; set; }
        public int EdgeRelaxations { get; set; }
        public bool HasNegativeCycle { get; set; }
        public List<Vertex>? NegativeCycle { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(StatusMessage))
            {
                return StatusMessage;
            }
            
            if (HasNegativeCycle)
            {
                var cyclePath = NegativeCycle != null ? string.Join(" -> ", NegativeCycle.Select(v => v.Id)) : "unknown";
                return $"Negative weight cycle detected! Cycle: {cyclePath}";
            }
            
            if (Path.Count == 0)
            {
                return "No path found.";
            }
            
            return $"Path: {string.Join(" -> ", Path.Select(v => v.Id))}" +
                   $"\nTime: {ExecutionTimeSeconds:F4}s, " +
                   $"Vertices: {VerticesVisited}, " +
                   $"Relaxations: {EdgeRelaxations}";
        }
    }
}
