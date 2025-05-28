using System.Collections.ObjectModel;
using System.ComponentModel;
using QuikGraph;
using GraphPathfinder.Models;
using GraphPathfinder.Algorithms;

namespace GraphPathfinder.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _isDirected;
        public bool IsDirected
        {
            get => _isDirected;
            set
            {
                if (_isDirected != value)
                {
                    _isDirected = value;
                    OnPropertyChanged(nameof(IsDirected));
                    if (!_isDirected)
                    {
                        foreach (var edge in Edges)
                            edge.IsDirected = false;
                    }
                }
            }
        }
        private bool _isWeighted;
        public bool IsWeighted
        {
            get => _isWeighted;
            set
            {
                if (_isWeighted != value)
                {
                    _isWeighted = value;
                    foreach (var edge in Edges)
                    {
                        edge.Weight = _isWeighted ? (edge.Weight ?? 1) : null;
                    }
                    OnPropertyChanged(nameof(IsWeighted));
                }
            }
        }

        public BidirectionalGraph<Vertex, Edge> Graph { get; } = new();

        public ObservableCollection<Vertex> Vertices { get; } = new();
        public ObservableCollection<Edge> Edges { get; } = new();

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private Vertex? _startVertex;
        public Vertex? StartVertex
        {
            get => _startVertex;
            set
            {
                if (_startVertex != value)
                {
                    _startVertex = value;
                    if (_startVertex != null && _startVertex == EndVertex)
                    {
                        EndVertex = null;
                        OnPropertyChanged(nameof(EndVertex));
                    }
                    OnPropertyChanged(nameof(StartVertex));
                }
            }
        }

        private Vertex? _endVertex;
        public Vertex? EndVertex
        {
            get => _endVertex;
            set
            {
                if (_endVertex != value)
                {
                    _endVertex = value;
                    if (_endVertex != null && _endVertex == StartVertex)
                    {
                        StartVertex = null;
                        OnPropertyChanged(nameof(StartVertex));
                    }
                    OnPropertyChanged(nameof(EndVertex));
                }
            }
        }

        public List<string> Algorithms { get; } = ["Dijkstra", "Bellman-Ford", "A*"];
        private string _selectedAlgorithm = "Dijkstra";
        public string SelectedAlgorithm
        {
            get => _selectedAlgorithm;
            set { _selectedAlgorithm = value; OnPropertyChanged(nameof(SelectedAlgorithm)); }
        }

        public RelayCommand SolveCommand { get; }

        public MainViewModel()
        {
            SolveCommand = new RelayCommand(_ => Solve());
        }

        private AlgorithmResult? _lastAlgorithmResult;
        public AlgorithmResult? LastAlgorithmResult => _lastAlgorithmResult;

        private void Solve()
        {
            if (StartVertex == null || EndVertex == null)
            {
                Status = "Please select both start and end vertices.";
                _lastAlgorithmResult = null;
                return;
            }
            AlgorithmResult result = new AlgorithmResult();
            switch (SelectedAlgorithm)
            {
                case "Dijkstra":
                    result = DijkstraAlgorithm.FindPath(StartVertex, EndVertex, Vertices, Edges, IsDirected);
                    break;
                case "Bellman-Ford":
                    result = BellmanFordAlgorithm.FindPath(StartVertex, EndVertex, Vertices, Edges, IsDirected);
                    break;
                case "A*":
                    result = AStarAlgorithm.FindPath(
                        StartVertex, EndVertex, Vertices, Edges,
                        (a, b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)),
                        IsDirected);
                    break;
            }
            _lastAlgorithmResult = result;
            Status = result.Path.Count == 0
                ? $"No path found from {StartVertex?.Id} to {EndVertex?.Id} using {SelectedAlgorithm}.\n" +
                  $"Time: {result.ExecutionTimeSeconds:F4}s, " +
                  $"Vertices: {result.VerticesVisited}, " +
                  $"Relaxations: {result.EdgeRelaxations}"
                : $"Path: {string.Join(" -> ", result.Path.Select(v => v.Id))}\n" +
                  $"Time: {result.ExecutionTimeSeconds:F4}s, " +
                  $"Vertices: {result.VerticesVisited}, " +
                  $"Relaxations: {result.EdgeRelaxations}";
        }

        public void AddVertex(Vertex v)
        {
            Graph.AddVertex(v);
            Vertices.Add(v);
            Status = $"Vertex {v.Id} added.";
        }

        public void RemoveVertex(Vertex v)
        {
            Graph.RemoveVertex(v);
            Vertices.Remove(v);
            for (int i = Edges.Count - 1; i >= 0; i--)
                if (Edges[i].Source == v || Edges[i].Target == v)
                    Edges.RemoveAt(i);
            Status = $"Vertex {v.Id} removed.";
        }

        public void AddEdge(Vertex from, Vertex to, bool isDirected = false, long? weight = null)
        {
            long? finalWeight = IsWeighted ? 1 : null;
            var toRemove = Edges.Where(e => (e.Source == from && e.Target == to) || (e.Source == to && e.Target == from)).ToList();
            foreach (var oldEdge in toRemove)
            {
                Graph.RemoveEdge(oldEdge);
                Edges.Remove(oldEdge);
            }
            var edge = new Edge(from, to, isDirected, finalWeight);
            Graph.AddEdge(edge);
            Edges.Add(edge);
            Status = $"Edge {from.Id} → {to.Id} added.";
        }

        public void RemoveEdge(Edge e)
        {
            Graph.RemoveEdge(e);
            Edges.Remove(e);
            Status = $"Edge {e.Source.Id} → {e.Target.Id} removed.";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
