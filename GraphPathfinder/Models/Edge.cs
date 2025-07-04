using QuikGraph;

namespace GraphPathfinder.Models
{
    public class Edge : Edge<Vertex>, System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isDirected = false;
        public bool IsDirected
        {
            get => _isDirected;
            set
            {
                if (_isDirected != value)
                {
                    _isDirected = value;
                    OnPropertyChanged(nameof(IsDirected));
                }
            }
        }

        public const int MaxWeight = 99999;
        public const int MinWeight = -99999;
        
        private long? _weight = null;
        public long? Weight
        {
            get => _weight;
            set
            {
                if (value != null)
                {
                    if (value > MaxWeight) value = MaxWeight;
                    else if (value < MinWeight) value = MinWeight;
                }
                
                if (_weight != value)
                {
                    _weight = value;
                    OnPropertyChanged(nameof(Weight));
                }
            }
        }

        public Edge(Vertex source, Vertex target, bool isDirected = false, long? weight = null)
            : base(source, target)
        {
            IsDirected = isDirected;
            Weight = weight;
        }

        public Edge(Vertex source, Vertex target) : base(source, target) { }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
