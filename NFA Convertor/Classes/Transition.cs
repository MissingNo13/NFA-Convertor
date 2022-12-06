using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NFA_Convertor.Classes;
public class Transition
{
    public Node To;
    public Node From;
    public List<string> Letters = new List<string>();
    public Label Label {get;} = new Label() {Foreground = Brushes.DimGray,FontSize = 20, IsHitTestVisible = false};
    public string LetterList => string.Join(", ", Letters);

    public Path Arrow;
    public Path Arc;

    public Transition(Node from, Node to)
    {
        From = from;
        To = to;
    }

    public Transition() {}
}