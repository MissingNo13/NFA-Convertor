using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NFA_Convertor.Classes;
public class Node
{
    private int _index;
    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            Label.Content = value;
        }
    }
    public readonly Shape Shape = new Ellipse() { Height = 55, Width = 55, Stroke = Brushes.Black, StrokeThickness = 2, Fill = Brushes.LightGray};
    public readonly Label Label = new Label(){IsHitTestVisible = false};
    public bool IsFinal, IsStarter, HasSubNodes;
    public List<Node> SubNodes = new List<Node>();
    public readonly List<Transition> Transitions = new List<Transition>();
}