using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GraphPathfinder.Models;
using GraphPathfinder.ViewModels;
using System.Globalization;
using System.Windows.Shapes;

namespace GraphPathfinder.Views
{
    public partial class MainWindow : Window
    {
        private const int MaxVertices = 99;
        private const int MaxWeight = 99999;
        private const int MinWeight = -99999;
        private const int MaxWeightDigits = 5;
        private const double VertexRadius = 22;

        private readonly SortedSet<int> _freeVertexIds = new();

        private int _nextVertexId = 1;
        private int _previewFrameCounter = 0;
        private int _vertexFrameCounter = 0;
        private bool _isDraggingEdge = false;
        private bool _dragDirected = false;
        private bool _lastDragDirected = false;
        
        private Vertex? _draggedVertex = null;
        private Vertex? _edgeStartVertex = null;
        private Edge? _selectedEdge = null;

        private Point _dragOffset;
        private Point _edgeDragCurrent;

        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext ??= new MainViewModel();

            GraphCanvas.SizeChanged += (s, e) => RedrawGraph();
            GraphCanvas.MouseLeftButtonDown += GraphCanvas_MouseLeftButtonDown;
            GraphCanvas.MouseMove += GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp += GraphCanvas_MouseLeftButtonUp;
            GraphCanvas.MouseRightButtonDown += GraphCanvas_MouseRightButtonDown;
            GraphCanvas.MouseLeftButtonDown += GraphCanvas_MouseLeftButtonDown_SelectEdge;
            GraphCanvas.PreviewMouseDown += GraphCanvas_PreviewMouseDown;
            KeyDown += Window_KeyDown;
            KeyUp += MainWindow_KeyUp;
            Loaded += (s, e) => RedrawGraph();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            ShowTextInputButton.Click += (s, e) =>
            {
                GraphTextInputPanel.Visibility = GraphTextInputPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            };

            GraphTextInputPanel.ParseRequested += text =>
            {
                var warnings = new List<string>();
                try
                {
                    ViewModel.ClearGraph();
                    _freeVertexIds.Clear();
                    _nextVertexId = 1;
                    _selectedEdge = null;

                    var vertexDict = new Dictionary<int, Vertex>();
                    var seenVertexIds = new HashSet<int>();
                    var seenEdges = new HashSet<string>();
                    bool inVertices = false, inEdges = false, hasVertices = false, hasEdges = false;
                    var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                    bool anyExplicitWeight = lines.Any(l => l.Contains("[w="));
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        {
                            if (line.ToLower().StartsWith("# vertices"))
                            {
                                inVertices = true;
                                inEdges = false;
                            }

                            if (line.ToLower().StartsWith("# edges"))
                            {
                                inEdges = true;
                                inVertices = false;
                            }

                            continue;
                        }

                        if (inVertices)
                        {
                            hasVertices = true;
                            var parts = line.Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int id))
                            {
                                int expectedId = seenVertexIds.Count + 1;
                                if (id != expectedId)
                                {
                                    warnings.Add($"Vertex IDs must be sequential starting from 1. Expected ID: {expectedId}, got: {id}");
                                    continue;
                                }

                                if (!seenVertexIds.Add(id))
                                {
                                    warnings.Add($"Duplicate vertex ID: {id}");
                                    continue;
                                }

                                var coords = parts[1].Split(',');
                                if (coords.Length == 2 &&
                                    double.TryParse(coords[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out double x) &&
                                    double.TryParse(coords[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                                        out double y))
                                {
                                    var v = new Vertex { Id = id, X = x, Y = y };
                                    ViewModel.AddVertex(v);
                                    vertexDict[id] = v;
                                }
                                else
                                {
                                    warnings.Add($"Malformed or missing coordinates for vertex ID {id}: '{line}'");
                                }
                            }
                            else
                            {
                                warnings.Add($"Malformed vertex line: '{line}'");
                            }
                        }
                        else if (inEdges)
                        {
                            hasEdges = true;
                            var edgePart = line.Split('[')[0].Trim();
                            int? weight = null;
                            bool hasExplicitWeight = false;
                            if (line.Contains("[w="))
                            {
                                var weightSection = line.Substring(line.IndexOf("[w=", StringComparison.Ordinal) + 3);
                                weightSection = weightSection.TrimEnd(']', ' ');
                                if (int.TryParse(weightSection, out int w))
                                {
                                    weight = w;
                                    hasExplicitWeight = true;
                                }
                                else
                                {
                                    warnings.Add($"Edge weight must be an integer: '{line}'");
                                    continue;
                                }
                            }

                            if (!hasExplicitWeight && ViewModel.IsWeighted && anyExplicitWeight)
                            {
                                weight = 1;
                            }

                            if (!ViewModel.IsWeighted && !hasExplicitWeight)
                            {
                                weight = null;
                            }

                            bool isDirected = edgePart.Contains("->");
                            string[] edgeVertices = isDirected ? edgePart.Split(["->"], StringSplitOptions.None) : edgePart.Split(["--"], StringSplitOptions.None);
                            if (edgeVertices.Length == 2 && int.TryParse(edgeVertices[0].Trim(), out int fromId) &&
                                int.TryParse(edgeVertices[1].Trim(), out int toId))
                            {
                                string edgeKey = $"{fromId}-{(isDirected ? ">" : "-")}-{toId}";
                                if (!seenEdges.Add(edgeKey))
                                {
                                    warnings.Add($"Duplicate edge: {line}");
                                    continue;
                                }

                                if (!vertexDict.ContainsKey(fromId) || !vertexDict.TryGetValue(toId, out var value))
                                {
                                    warnings.Add($"Edge refers to missing vertex: '{line}'");
                                    continue;
                                }

                                ViewModel.AddEdge(vertexDict[fromId], value, isDirected, weight);
                            }
                            else
                            {
                                warnings.Add($"Malformed edge line: '{line}'");
                            }
                        }
                    }

                    if (!hasVertices)
                        warnings.Add("No vertices section or no vertices defined.");
                    if (!hasEdges)
                        warnings.Add("No edges section or no edges defined.");
                    ViewModel.IsDirected = text.Contains("->");
                    ViewModel.IsWeighted = text.Contains("[w=");
                    
                    if (ViewModel.Vertices.Count > 0)
                    {
                        _nextVertexId = ViewModel.Vertices.Max(v => v.Id) + 1;
                    }
                    
                    RedrawGraph();
                    ViewModel.Status =
                        $"Graph loaded from text: {ViewModel.Vertices.Count} vertices, {ViewModel.Edges.Count} edges.";
                    if (warnings.Count > 0)
                    {
                        MessageBox.Show(string.Join("\n", warnings),
                            "Graph Input Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing graph input:\n{ex.Message}",
                        "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsWeighted) ||
                e.PropertyName == nameof(MainViewModel.IsDirected))
                RedrawGraph();
        }

        private void RedrawGraph()
        {
            GraphCanvas.Children.Clear();
            double vertexRadius = VertexRadius;
            if (_isDraggingEdge && _edgeStartVertex != null)
            {
                double arrowLen = _dragDirected ? 16 : 0;
                var dx = _edgeDragCurrent.X - _edgeStartVertex.X;
                var dy = _edgeDragCurrent.Y - _edgeStartVertex.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                double ux = len > 0 ? dx / len : 0;
                double uy = len > 0 ? dy / len : 0;
                double endX = _edgeDragCurrent.X;
                double endY = _edgeDragCurrent.Y;
                if (_dragDirected && len > 0)
                {
                    endX = _edgeDragCurrent.X - ux * arrowLen;
                    endY = _edgeDragCurrent.Y - uy * arrowLen;
                }

                if (_edgeStartVertex == null)
                    return;
                {
                    var tempLine = new Line
                    {
                        X1 = _edgeStartVertex.X,
                        Y1 = _edgeStartVertex.Y,
                        X2 = endX,
                        Y2 = endY,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 2,
                        StrokeDashArray = [4, 2]
                    };
                    GraphCanvas.Children.Add(tempLine);
                    if (_dragDirected)
                    {
                        DrawArrowhead(_edgeStartVertex.X, _edgeStartVertex.Y, _edgeDragCurrent.X, _edgeDragCurrent.Y,
                            false, true);
                    }
                }
            }

            foreach (var edge in ViewModel.Edges)
            {
                bool selected = edge == _selectedEdge;
                bool drawDirected = ViewModel.IsDirected;
                double arrowLen = drawDirected ? 16 : 0;
                var dx = edge.Target.X - edge.Source.X;
                var dy = edge.Target.Y - edge.Source.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                double ux = len > 0 ? dx / len : 0;
                double uy = len > 0 ? dy / len : 0;
                double endX = edge.Target.X;
                double endY = edge.Target.Y;
                if (ViewModel.IsDirected && edge.IsDirected && len > 0)
                {
                    endX = edge.Target.X - ux * (arrowLen + vertexRadius);
                    endY = edge.Target.Y - uy * (arrowLen + vertexRadius);
                }

                var line = new Line
                {
                    X1 = edge.Source.X,
                    Y1 = edge.Source.Y,
                    X2 = endX,
                    Y2 = endY,
                    Stroke = selected ? Brushes.Red : Brushes.Black,
                    StrokeThickness = 2
                };
                GraphCanvas.Children.Add(line);
                if (ViewModel.IsDirected && edge.IsDirected)
                {
                    DrawArrowhead(edge.Source.X, edge.Source.Y, edge.Target.X, edge.Target.Y, selected);
                }

                if (ViewModel.IsWeighted && edge.Weight.HasValue)
                {
                    DrawEdgeWeight(edge, selected);
                }
            }

            foreach (var vertex in ViewModel.Vertices)
            {
                var ellipse = new Ellipse
                {
                    Width = vertexRadius * 2,
                    Height = vertexRadius * 2,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                };
                Canvas.SetLeft(ellipse, vertex.X - vertexRadius);
                Canvas.SetTop(ellipse, vertex.Y - vertexRadius);
                GraphCanvas.Children.Add(ellipse);
                var label = new TextBlock
                {
                    Text = vertex.Id.ToString(),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = label.DesiredSize;
                Canvas.SetLeft(label, vertex.X - size.Width / 2);
                Canvas.SetTop(label, vertex.Y - size.Height / 2);
                GraphCanvas.Children.Add(label);
            }
        }

        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedEdge != null)
            {
                _selectedEdge = null;
                RedrawGraph();
            }
            
            var pos = e.GetPosition(GraphCanvas);
            var v = FindVertexAt(pos);
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (ViewModel.Vertices.Count >= MaxVertices)
                    {
                        MessageBox.Show($"Maximum number of vertices ({MaxVertices}) reached.", "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    int id;
                    if (_freeVertexIds.Count > 0)
                    {
                        id = _freeVertexIds.Min;
                        _freeVertexIds.Remove(id);
                        
                        if (id >= _nextVertexId)
                        {
                            _nextVertexId = id + 1;
                        }
                    }
                    else
                    {
                        id = _nextVertexId++;
                    }
                    double margin = 24;
                    double x = Math.Max(margin, Math.Min(GraphCanvas.ActualWidth - margin, pos.X));
                    double y = Math.Max(margin, Math.Min(GraphCanvas.ActualHeight - margin, pos.Y));
                    var newVertex = new Vertex { Id = id, X = x, Y = y };
                    const double minDist = 50;
                    bool tooClose = ViewModel.Vertices.Any(vtx =>
                        Math.Sqrt((vtx.X - newVertex.X) * (vtx.X - newVertex.X) +
                                  (vtx.Y - newVertex.Y) * (vtx.Y - newVertex.Y)) < minDist);
                    if (tooClose)
                    {
                        return;
                    }

                    ViewModel.AddVertex(newVertex);
                    RedrawGraph();
                }
                else if (v != null)
                {
                    _draggedVertex = v;
                    _dragOffset = new Point(pos.X - v.X, pos.Y - v.Y);
                    GraphCanvas.CaptureMouse();
                }
            }
        }

        private void GraphCanvas_MouseLeftButtonDown_SelectEdge(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas && Equals(e.OriginalSource, canvas))
            {
                if (_selectedEdge != null)
                {
                    _selectedEdge = null;
                    RedrawGraph();
                }
                return;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                var pos = e.GetPosition(GraphCanvas);
                var edge = FindEdgeAt(pos);
                if (edge != null)
                {
                    _selectedEdge = edge;
                    RedrawGraph();
                }
                else
                {
                    if (_selectedEdge != null)
                    {
                        _selectedEdge = null;
                        RedrawGraph();
                    }
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && _selectedEdge != null)
            {
                e.Handled = true;
                string currentWeightStr = _selectedEdge.Weight?.ToString() ?? "0";
                string nextStr = currentWeightStr.StartsWith("-") ? 
                    currentWeightStr.TrimStart('-') : 
                    "-" + currentWeightStr;
                
                if (int.TryParse(nextStr, out int result))
                {
                    if (result > 99999) result = 99999;
                    else if (result < -99999) result = -99999;
                    _selectedEdge.Weight = result;
                    RedrawGraph();
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            
            if (_selectedEdge != null)
            {
                if (e.Key >= Key.D0 && e.Key <= Key.D9)
                {
                    e.Handled = true;
                    string currentWeightStr = _selectedEdge.Weight?.ToString() ?? "0";
                    string nextStr;
                    
                    if (currentWeightStr == "0" || currentWeightStr == "-0")
                    {
                        nextStr = (e.Key - Key.D0).ToString();
                    }
                    else if (currentWeightStr.Replace("-", "").Length >= MaxWeightDigits)
                    {
                        return;
                    }
                    else
                    {
                        nextStr = currentWeightStr + (e.Key - Key.D0);
                    }
                    
                    if (int.TryParse(nextStr, out int result))
                    {
                        if (result > MaxWeight) result = MaxWeight;
                        else if (result < MinWeight) result = MinWeight;
                        _selectedEdge.Weight = result;
                        RedrawGraph();
                    }
                }
                else if (e.Key == Key.Back)
                {
                    e.Handled = true;
                    string currentWeightStr = _selectedEdge.Weight?.ToString() ?? "0";
                    string nextStr;
                    
                    if (currentWeightStr == "0" || currentWeightStr == "-0" || currentWeightStr.Length == 1)
                    {
                        nextStr = "0";
                    }
                    else if (currentWeightStr == "-1")
                    {
                        nextStr = "0";
                    }
                    else
                    {
                        nextStr = currentWeightStr[..^1];
                        if (nextStr == "-")
                        {
                            nextStr = "0";
                        }
                    }
                    
                    if (int.TryParse(nextStr, out int result))
                    {
                        _selectedEdge.Weight = result;
                        RedrawGraph();
                    }
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    e.Handled = true;
                    string currentWeightStr = _selectedEdge.Weight?.ToString() ?? "0";
                    string nextStr = currentWeightStr.StartsWith("-") ? 
                        currentWeightStr.TrimStart('-') : 
                        "-" + currentWeightStr;
                    
                    if (int.TryParse(nextStr, out int result))
                    {
                        if (result > MaxWeight) result = MaxWeight;
                        else if (result < MinWeight) result = MinWeight;
                        _selectedEdge.Weight = result;
                        RedrawGraph();
                    }
                }
            }
            
            if (_isDraggingEdge && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                bool newDragDirected = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (newDragDirected != _dragDirected)
                {
                    _dragDirected = newDragDirected;
                    RedrawGraph();
                }
            }
        }


        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (_isDraggingEdge && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                bool newDragDirected = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (newDragDirected != _dragDirected)
                {
                    _dragDirected = newDragDirected;
                    RedrawGraph();
                }
            }
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(GraphCanvas);
            if (_draggedVertex is not null && e.LeftButton == MouseButtonState.Pressed)
            {
                double margin = 24;
                double newX = pos.X - _dragOffset.X;
                double newY = pos.Y - _dragOffset.Y;
                newX = Math.Max(margin, Math.Min(GraphCanvas.ActualWidth - margin, newX));
                newY = Math.Max(margin, Math.Min(GraphCanvas.ActualHeight - margin, newY));
                const double minDist = 80;
                double slideX = newX;
                double slideY = newY;
                foreach (var vtx in ViewModel.Vertices)
                {
                    if (vtx == _draggedVertex) continue;
                    double dx = slideX - vtx.X;
                    double dy = slideY - vtx.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < minDist && dist > 0.01)
                    {
                        double scale = minDist / dist;
                        slideX = vtx.X + dx * scale;
                        slideY = vtx.Y + dy * scale;
                    }
                }

                if (_draggedVertex.X != slideX || _draggedVertex.Y != slideY)
                {
                    if (_draggedVertex is not null)
                    {
                        _draggedVertex.X = slideX;
                        _draggedVertex.Y = slideY;
                    }
                }

                _vertexFrameCounter++;
                if (_vertexFrameCounter % 4 == 0)
                {
                    RedrawGraph();
                }
            }
            else if (_isDraggingEdge && e.RightButton == MouseButtonState.Pressed)
            {
                _dragDirected = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                double snapRadius = 24;
                Vertex? snapVertex = null;
                double minDist = snapRadius;
                foreach (var v in ViewModel.Vertices)
                {
                    if (v == _edgeStartVertex) continue;
                    double dist = Math.Sqrt(Math.Pow(pos.X - v.X, 2) + Math.Pow(pos.Y - v.Y, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        snapVertex = v;
                    }
                }

                if (snapVertex != null)
                {
                    if (_edgeStartVertex != null)
                    {
                        var dx = snapVertex.X - _edgeStartVertex.X;
                        var dy = snapVertex.Y - _edgeStartVertex.Y;
                        double len = Math.Sqrt(dx * dx + dy * dy);
                        double ux = len > 0 ? dx / len : 0;
                        double uy = len > 0 ? dy / len : 0;
                        _edgeDragCurrent = new Point(snapVertex.X - ux * 22, snapVertex.Y - uy * 22);
                    }
                }
                else
                {
                    _edgeDragCurrent = pos;
                }

                if (_dragDirected != _lastDragDirected)
                {
                    RedrawGraph();
                    _previewFrameCounter = 0;
                }
                else
                {
                    _previewFrameCounter++;
                    if (_previewFrameCounter % 4 == 0)
                    {
                        RedrawGraph();
                    }
                }

                _lastDragDirected = _dragDirected;
            }
            else
            {
                new Point(double.NaN, double.NaN);
                _previewFrameCounter = 0;
                _vertexFrameCounter = 0;
                _lastDragDirected = false;
            }
        }

        private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedVertex != null)
            {
                _draggedVertex = null;
                GraphCanvas.ReleaseMouseCapture();
            }
        }

        private void GraphCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                e.Handled = true;
                var pos = e.GetPosition(GraphCanvas);
                var v = FindVertexAt(pos);
                if (v != null)
                {
                    _freeVertexIds.Add(v.Id);
                    if (v.Id == _nextVertexId - 1)
                    {
                        _nextVertexId = 1;
                        if (ViewModel.Vertices.Count > 1)
                        {
                            _nextVertexId = ViewModel.Vertices
                                .Where(vertex => vertex != v)
                                .Max(vertex => vertex.Id) + 1;
                        }
                    }
                    
                    ViewModel.RemoveVertex(v);
                    RedrawGraph();
                    return;
                }

                var edge = FindEdgeAt(pos);
                if (edge != null)
                {
                    ViewModel.RemoveEdge(edge);
                    RedrawGraph();
                }
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.ChangedButton == MouseButton.Right)
            {
                var pos = e.GetPosition(GraphCanvas);
                var v = FindVertexAt(pos);
                if (v != null)
                {
                    _edgeStartVertex = v;
                    _edgeDragCurrent = pos;
                    _isDraggingEdge = true;
                    _dragDirected = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                    GraphCanvas.CaptureMouse();
                }
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            if (e.ChangedButton == MouseButton.Right && _isDraggingEdge)
            {
                var pos = e.GetPosition(GraphCanvas);
                var v = FindVertexAt(pos);
                if (_edgeStartVertex != null && v != null && v != _edgeStartVertex)
                {
                    ViewModel.AddEdge(_edgeStartVertex, v, _dragDirected);
                    var edge = ViewModel.Edges.FirstOrDefault(e => e.Source == _edgeStartVertex && e.Target == v);
                    _selectedEdge = edge;
                    ViewModel.Status = $"Edge {_edgeStartVertex.Id} → {v.Id} added.";
                    ViewModel.IsDirected = ViewModel.Edges.Any(e => e.IsDirected);
                    ViewModel.IsWeighted = ViewModel.Edges.Any(e => e.Weight.HasValue);
                }

                _isDraggingEdge = false;
                _edgeStartVertex = null;
                _dragDirected = false;
                GraphCanvas.ReleaseMouseCapture();
                RedrawGraph();
            }
        }

        private void DrawArrowhead(double x1, double y1, double x2, double y2, bool selected, bool isPreview = false)
        {
            double arrowLength = 16;
            double arrowWidth = 8;
            var dx = x2 - x1;
            var dy = y2 - y1;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;
            double ux = dx / length, uy = dy / length;
            double tipOffset = isPreview ? 0 : 22;
            double px = x2 - ux * tipOffset;
            double py = y2 - uy * tipOffset;
            double leftX = px - uy * arrowWidth / 2 - ux * arrowLength;
            double leftY = py + ux * arrowWidth / 2 - uy * arrowLength;
            double rightX = px + uy * arrowWidth / 2 - ux * arrowLength;
            double rightY = py - ux * arrowWidth / 2 - uy * arrowLength;

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(px, py), IsClosed = true };
            figure.Segments.Add(new LineSegment(new Point(leftX, leftY), true));
            figure.Segments.Add(new LineSegment(new Point(rightX, rightY), true));
            geometry.Figures.Add(figure);
            var path = new Path
            {
                Data = geometry,
                Fill = isPreview ? Brushes.Gray : (selected ? Brushes.Red : Brushes.Black),
                Stroke = null
            };
            GraphCanvas.Children.Add(path);
        }

        private void DrawEdgeWeight(Edge edge, bool selected)
        {
            double mx = (edge.Source.X + edge.Target.X) / 2;
            double my = (edge.Source.Y + edge.Target.Y) / 2;
            double dx = edge.Target.X - edge.Source.X;
            double dy = edge.Target.Y - edge.Source.Y;
            double norm = Math.Sqrt(dx * dx + dy * dy);
            double offsetX = 0, offsetY = 0;
            if (norm > 0)
            {
                offsetX = -dy / norm * 18;
                offsetY = dx / norm * 18;
            }

            var label = new TextBlock
            {
                Text = edge.Weight.ToString(),
                Foreground = selected ? Brushes.Red : Brushes.DarkSlateGray,
                Background = Brushes.White,
                FontWeight = FontWeights.DemiBold,
                FontSize = 13,
                Padding = new Thickness(2, 0, 2, 0)
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = label.DesiredSize;
            Canvas.SetLeft(label, mx + offsetX - size.Width / 2);
            Canvas.SetTop(label, my + offsetY - size.Height / 2);
            GraphCanvas.Children.Add(label);
        }

        private void GraphCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private Vertex? FindVertexAt(Point pos)
        {
            double radius = 22;
            foreach (var v in ViewModel.Vertices)
            {
                double dx = v.X - pos.X, dy = v.Y - pos.Y;
                if (dx * dx + dy * dy <= radius * radius)
                    return v;
            }

            return null;
        }

        private Edge? FindEdgeAt(Point pos)
        {
            foreach (var edge in ViewModel.Edges)
            {
                if (DistancePointToSegment(pos, edge.Source, edge.Target) < 12)
                    return edge;
            }

            return null;
        }

        private double DistancePointToSegment(Point p, Vertex v1, Vertex v2)
        {
            double x = p.X, y = p.Y, x1 = v1.X, y1 = v1.Y, x2 = v2.X, y2 = v2.Y;
            double dx = x2 - x1, dy = y2 - y1;
            if (dx == 0 && dy == 0) return Math.Sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1));
            double t = ((x - x1) * dx + (y - y1) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double projX = x1 + t * dx;
            double projY = y1 + t * dy;
            return Math.Sqrt((x - projX) * (x - projX) + (y - projY) * (y - projY));
        }

        private void ClearGraphButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearGraph();
            _freeVertexIds.Clear();
            _nextVertexId = 1;
            _selectedEdge = null;
            RedrawGraph();
            ViewModel.Status = "Graph cleared.";
        }

        private void SaveResultButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.StartVertex == null || ViewModel.EndVertex == null)
            {
                MessageBox.Show("Please select both start and end vertices before saving.", "Cannot Save",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ViewModel.GetType()
                .GetMethod("Solve", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(ViewModel, null);

            string defaultFileName = $"PathResult_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var fileName = SaveFileDialogHelper.ShowSaveDialog(defaultFileName);
            if (fileName == null)
                return;

            var vm = ViewModel;
            var lines = new System.Collections.Generic.List<string>();
            lines.Add($"# Pathfinding Result");
            lines.Add(
                $"Date of operation: {(vm.LastAlgorithmResult != null && vm.LastAlgorithmResult.Path != null && vm.LastAlgorithmResult.Path.Count > 0 ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "-")}");
            lines.Add($"Date of export: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.Add($"Algorithm: {vm.SelectedAlgorithm}");
            lines.Add($"Directed: {vm.IsDirected}");
            lines.Add($"Weighted: {vm.IsWeighted}");
            lines.Add($"Vertices: {vm.Vertices.Count}");
            lines.Add($"Edges: {vm.Edges.Count}");
            lines.Add("");
            lines.Add("# Vertices");
            foreach (var v in vm.Vertices)
                lines.Add($"{v.Id}: {v.X},{v.Y}");
            lines.Add("");
            lines.Add("# Edges");
            foreach (var edge in vm.Edges)
            {
                string dir = edge.IsDirected ? "->" : "--";
                string weight = edge.Weight.HasValue ? $" [w={edge.Weight}]" : "";
                lines.Add($"{edge.Source.Id}{dir}{edge.Target.Id}{weight}");
            }

            lines.Add("");
            lines.Add($"Start Vertex: {(vm.StartVertex != null ? vm.StartVertex.Id.ToString() : "(not set)")}");
            lines.Add($"End Vertex: {(vm.EndVertex != null ? vm.EndVertex.Id.ToString() : "(not set)")}");
            lines.Add("");
            lines.Add("# Path");
            if (vm.LastAlgorithmResult != null && vm.LastAlgorithmResult.Path != null &&
                vm.LastAlgorithmResult.Path.Count > 0)
            {
                lines.Add(string.Join(" -> ", vm.LastAlgorithmResult.Path.Select(v => v.Id)));
                lines.Add("");
                lines.Add($"Execution time: {vm.LastAlgorithmResult.ExecutionTimeSeconds:F4} seconds");
                lines.Add($"Vertices visited: {vm.LastAlgorithmResult.VerticesVisited}");
                lines.Add($"Edge relaxations: {vm.LastAlgorithmResult.EdgeRelaxations}");
            }
            else
            {
                lines.Add("No path found.");
            }

            lines.Add("");
            lines.Add($"Status: {vm.Status}");
            System.IO.File.WriteAllLines(fileName, lines);
            MessageBox.Show($"Pathfinding result saved to:\n{fileName}", "Saved", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}