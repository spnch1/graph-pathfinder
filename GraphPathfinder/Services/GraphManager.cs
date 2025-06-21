using GraphPathfinder.Models;
using QuikGraph;

namespace GraphPathfinder.Services
{
    public class GraphManager
    {
        private readonly BidirectionalGraph<Vertex, Edge> _graph = new();

        public IReadOnlyCollection<Vertex> Vertices => _graph.Vertices.ToList();
        public IReadOnlyCollection<Edge> Edges => _graph.Edges.ToList();

        public event Action? GraphChanged;

        public bool AddVertex(Vertex v)
        {
            if (_graph.Vertices.Contains(v))
                return false;

            _graph.AddVertex(v);
            GraphChanged?.Invoke();
            return true;
        }

        public bool RemoveVertex(Vertex v)
        {
            if (!_graph.Vertices.Contains(v))
                return false;

            var edgesToRemove = _graph.Edges.Where(e => e.Source == v || e.Target == v).ToList();
            foreach (var edge in edgesToRemove)
            {
                _graph.RemoveEdge(edge);
            }

            bool removed = _graph.RemoveVertex(v);
            if (removed)
                GraphChanged?.Invoke();
            return removed;
        }

        public bool AddEdge(Vertex from, Vertex to, bool isDirected = false, long? weight = null)
        {
            if (!_graph.Vertices.Contains(from) || !_graph.Vertices.Contains(to))
                throw new ArgumentException("Both vertices must exist in the graph.");

            bool edgeExists = _graph.Edges.Any(e =>
                (e.Source == from && e.Target == to) || (e.Source == to && e.Target == from));

            if (edgeExists)
                return false;

            var edge = new Edge(from, to, isDirected, weight);
            bool added = _graph.AddEdge(edge);
            if (added)
                GraphChanged?.Invoke();
            return added;
        }

        public bool RemoveEdge(Edge e)
        {
            if (!_graph.Edges.Contains(e))
                return false;

            bool removed = _graph.RemoveEdge(e);
            if (removed)
                GraphChanged?.Invoke();
            return removed;
        }

        public void Clear()
        {
            _graph.Clear();
            GraphChanged?.Invoke();
        }
    }
}
