using System;
using System.Windows;
using System.Windows.Controls;

namespace GraphPathfinder.Views
{
    public partial class GraphTextInput : UserControl
    {
        public event Action<string>? ParseRequested;

        public GraphTextInput()
        {
            InitializeComponent();
            VerticesInputBox.Text = "1: 100,200\n2: 300,400\n3: 200,100\n4: 350,150";
            EdgesInputBox.Text = "1->2\n2--3\n1->3 [w=5]\n3--4 [w=10]\n4->1 [w=2]";
            ParseGraphButton.Click += (s, e) =>
            {
                var text = "# Vertices\n" + VerticesInputBox.Text + "\n# Edges\n" + EdgesInputBox.Text;
                ParseRequested?.Invoke(text);
            };
        }
    }
}
