using System.Collections.ObjectModel;
using System.ComponentModel;
using GraphPathfinder.Models;
using GraphPathfinder.Algorithms;
using GraphPathfinder.Services;

namespace GraphPathfinder.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly GraphManager _graphManager = new();

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

        public ObservableCollection<Vertex> Vertices { get; } = [];
        public ObservableCollection<Edge> Edges { get; } = [];

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

        private bool _hasNegativeWeights = false;
        public bool HasNegativeWeights
        {
            get => _hasNegativeWeights;
            private set
            {
                if (_hasNegativeWeights != value)
                {
                    _hasNegativeWeights = value;
                    OnPropertyChanged(nameof(HasNegativeWeights));
                    OnPropertyChanged(nameof(AvailableAlgorithms));

                    if (value && SelectedAlgorithm != "Bellman-Ford")
                    {
                        SelectedAlgorithm = "Bellman-Ford";
                    }
                }
            }
        }

        public List<string> AvailableAlgorithms => HasNegativeWeights
            ? ["Bellman-Ford"] : ["Dijkstra", "Bellman-Ford", "A*"];

        private string _selectedAlgorithm = "Dijkstra";
        public string SelectedAlgorithm
        {
            get => _selectedAlgorithm;
            set
            {
                if (_selectedAlgorithm != value && AvailableAlgorithms.Contains(value))
                {
                    _selectedAlgorithm = value;
                    OnPropertyChanged(nameof(SelectedAlgorithm));
                }
            }
        }

        private AlgorithmResult? _lastAlgorithmResult;
        public AlgorithmResult? LastAlgorithmResult => _lastAlgorithmResult;

        public RelayCommand SolveCommand { get; }

        public MainViewModel()
        {
            SolveCommand = new RelayCommand(_ => Solve());

            _graphManager.GraphChanged += OnGraphChanged;

            SyncCollectionsWithGraph();
        }

        private void OnGraphChanged()
        {
            SyncCollectionsWithGraph();
            CheckForNegativeWeights();
        }

        private void SyncCollectionsWithGraph()
        {
            foreach (var edge in Edges)
            {
                edge.PropertyChanged -= Edge_PropertyChanged;
            }

            Vertices.Clear();
            foreach (var v in _graphManager.Vertices)
                Vertices.Add(v);

            Edges.Clear();
            foreach (var e in _graphManager.Edges)
            {
                e.PropertyChanged += Edge_PropertyChanged;
                Edges.Add(e);
            }

            OnPropertyChanged(nameof(Vertices));
            OnPropertyChanged(nameof(Edges));
            
            CheckForNegativeWeights();
        }
        
        private void Edge_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Edge.Weight))
            {
                CheckForNegativeWeights();
            }
        }

        private void CheckForNegativeWeights()
        {
            if (!IsWeighted)
            {
                HasNegativeWeights = false;
                return;
            }

            bool hasNegative = Edges.Any(e => e.Weight < 0);
            if (_hasNegativeWeights != hasNegative)
            {
                _hasNegativeWeights = hasNegative;
                OnPropertyChanged(nameof(HasNegativeWeights));
                OnPropertyChanged(nameof(AvailableAlgorithms));

                if (hasNegative && SelectedAlgorithm != "Bellman-Ford")
                {
                    SelectedAlgorithm = "Bellman-Ford";
                }
            }
        }

        private void Solve()
        {
            if (!ValidateStartAndEndVertices())
                return;

            switch (SelectedAlgorithm)
            {
                case "Dijkstra":
                    RunDijkstra();
                    break;
                case "Bellman-Ford":
                    RunBellmanFord();
                    break;
                case "A*":
                    RunAStar();
                    break;
                default:
                    Status = "Selected algorithm is not supported.";
                    _lastAlgorithmResult = null;
                    break;
            }

            if (_lastAlgorithmResult != null)
                Status = _lastAlgorithmResult.ToString();
        }

        private bool ValidateStartAndEndVertices()
        {
            if (StartVertex == null || EndVertex == null)
            {
                Status = "Please select both start and end vertices.";
                _lastAlgorithmResult = null;
                return false;
            }
            return true;
        }

        private void RunDijkstra()
        {
            _lastAlgorithmResult = DijkstraAlgorithm.FindPath(StartVertex!, EndVertex!, Vertices, Edges, IsDirected);
        }

        private void RunBellmanFord()
        {
            _lastAlgorithmResult = BellmanFordAlgorithm.FindPath(StartVertex!, EndVertex!, Vertices, Edges, IsDirected);
        }

        private void RunAStar()
        {
            _lastAlgorithmResult = AStarAlgorithm.FindPath(
                StartVertex!, EndVertex!, Vertices, Edges,
                (a, b) => 0,
                // (a, b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)), // Euclidean distance
                // (a, b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y), // Manhattan distance
                IsDirected);
        }

        public void AddVertex(Vertex v)
        {
            if (v.X < 0 || v.Y < 0)
            {
                Status = "Error: Vertex coordinates cannot be negative.";
                return;
            }

            if (_graphManager.AddVertex(v))
                Status = $"Vertex {v.Id} added.";
        }

        public void RemoveVertex(Vertex v)
        {
            if (_graphManager.RemoveVertex(v))
                Status = $"Vertex {v.Id} removed.";
        }

        public void AddEdge(Vertex from, Vertex to, bool isDirected = false, long? weight = null)
        {
            long? finalWeight = weight ?? (IsWeighted ? 1 : null);

            if (_graphManager.AddEdge(from, to, isDirected, finalWeight))
                Status = $"Edge {from.Id} → {to.Id} added.";
        }

        public void RemoveEdge(Edge e)
        {
            if (_graphManager.RemoveEdge(e))
                Status = $"Edge {e.Source.Id} → {e.Target.Id} removed.";
        }

        public void ClearGraph()
        {
            _graphManager.Clear();
            Status = "Graph cleared.";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
