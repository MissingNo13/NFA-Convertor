using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NFA_Convertor.Classes;
public class Machine
{
    public List<Node> Nodes = new List<Node>();

    public List<string> Alphabet = new List<string>();
    public const string Lambda = "λ";
    private int _counter;

    private int Counter => _counter++;

        
    public Node StartNode;
    private Line StartLine = new Line(){Stroke = Brushes.Black, StrokeThickness = 1.0};

    public Node AddNode(List<Node> subNodes = null, bool isStarter = false, bool isFinal = false)
    {
        var newNode = new Node()
        {
            Index = Counter, SubNodes = subNodes , IsFinal = isFinal, IsStarter = isStarter,
            HasSubNodes = subNodes != null
        };

        Nodes.Add(newNode);

        return newNode;
    }

    public void RemoveNode(Node node, CustomCanvas canvas)
    {
        foreach (var variable in Nodes)
        {
            for (var i = 0; i < variable.Transitions.Count; i++)
            {
                var transition = variable.Transitions[i];
                if (transition.To.Equals(node) || transition.From.Equals(node))
                {
                    RemoveTransition(transition, canvas);
                }
            }
        }

        Nodes.Remove(node);
        if (StartNode != null && StartNode.Equals(node))
        {
            StartNode = null;
            canvas.Children.Remove(StartLine);
        }
        canvas.Children.Remove(node.Shape);
        canvas.Children.Remove(node.Label);
    }

    public void ToggleFinalState(Node node)
    {
        var value = node.IsFinal = !node.IsFinal;
        node.Shape.StrokeThickness = value ? 10 : 2;
        node.Shape.Fill = value ? Brushes.SteelBlue : Brushes.LightGray;
    }

    public Node SearchNode(Shape selectedShape)
    {
        foreach (var variable in Nodes)
        {
            if (variable.Shape.Equals(selectedShape)) return variable;
        }
            
        return null;
    }

    public Node SearchNode(int label)
    {
        foreach (var variable in Nodes)
        {
            if (variable.Index.Equals(label)) return variable;
        }

        return null;
    }
        
    /***************************************TRANSITIONS*********************************************/

    public Transition SearchTransition(Node from, Node to)
    {
        return from.Transitions.FirstOrDefault(transition => transition.From.Equals(from) && transition.To.Equals(to));
    }

    public Transition SearchTransition(Node from, string alphabet)
    {
        return from.Transitions.FirstOrDefault(transition => transition.From.Equals(from) && transition.Letters.Contains(alphabet));
    }

    public void RemoveTransition(Shape shape, CustomCanvas canvas)
    {
        foreach (var node in Nodes)
        {
            foreach (var transition in node.Transitions.Where(transition => shape.Equals(transition.Arrow)))
            {
                RemoveTransition(transition, canvas);
                return;
            }
        }
    }
        
    private void RemoveTransition(Transition transition, CustomCanvas canvas)
    {
        transition.From.Transitions.Remove(transition);
        transition.To.Transitions.Remove(transition);
        canvas.Children.Remove(transition.Arrow);
        canvas.Children.Remove(transition.Arc);
        canvas.Children.Remove(transition.Label);
    }

    public Transition SearchTransition(Shape shape)
    {
        return Nodes
            .SelectMany(variable => variable.Transitions.Where(transition => transition.Arrow.Equals(shape)))
            .FirstOrDefault();
    }

    private Transition CreateTransition(Node from, Node to)
    {

        var transition = new Transition(from, to);

        from.Transitions.Add(transition);
        if (from.Index != to.Index) to.Transitions.Add(transition);

        return transition;
    }

    public Transition AddTransition(Node from, Node to, CustomCanvas canvas)
    {
        var transition = SearchTransition(from, to);
        if (transition == null)
        {
            transition = CreateTransition(from, to);
            DrawTransition(transition);
            transition.Arrow.PreviewMouseRightButtonUp += canvas.PathOnPreviewMouseRightButtonUp;
            canvas.Children.Add(transition.Arrow);
            canvas.Children.Add(transition.Arc);
            canvas.Children.Add(transition.Label);
        }

        return transition;
    }

    public void AddTransition(Node from, Node to, string letter) //used in Machine Convertor. Doesn't need Canvas
    {
        var transition = SearchTransition(from, to);
        if (transition == null) transition = CreateTransition(from, to);

        if (!transition.Letters.Contains(letter))
            transition.Letters.Add(letter);
        transition.Label.Content = transition.LetterList;
    }

    public void AddTransition(Node from, Node to, CustomCanvas canvas, List<string> letters) //used in convert method.
    {
        var transition = AddTransition(from, to, canvas);
        transition.Letters = letters;
        transition.Label.Content = transition.LetterList;
    }

    private void DrawTransition(Transition transition)
    {
        var fromShape = transition.From.Shape;
        var toShape = transition.To.Shape;
        var p1 = new Point(Canvas.GetLeft(fromShape) + fromShape.Width / 2,
            Canvas.GetTop(fromShape) + fromShape.Width / 2);
        var p2 = new Point(Canvas.GetLeft(toShape) + toShape.Height / 2,
            Canvas.GetTop(toShape) + toShape.Height / 2);
        var isSamePoint = false;
        if (p1.Equals(p2))
        {
            isSamePoint = true;
            p1 = Point.Add(p1, new Vector(-15, -15));
            p2 = Point.Add(p2, new Vector(15, -15));
        }

        var theta = isSamePoint ? 0 : Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

        var arcPathGeometry = new PathGeometry();
        var arcFigure = new PathFigure() {StartPoint = p1};

        var arcSegment = new ArcSegment()
        {
            Point = p2, Size = fromShape.Equals(toShape) ? new Size(1, 2.5) : new Size(2.5, 1), IsLargeArc = true,
            RotationAngle = theta, SweepDirection = SweepDirection.Clockwise
        };

        arcFigure.Segments.Add(arcSegment);

        arcPathGeometry.Figures.Add(arcFigure);

        var arc = new Path() {Data = arcPathGeometry, Stroke = Brushes.Black, StrokeThickness = 1};
        transition.Arc = arc;
        Panel.SetZIndex(arc, -1);

        arcPathGeometry.GetPointAtFractionLength(0.55, out var p, out _);

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure() {StartPoint = p};

        var lPoint = new Point(p.X + 10, p.Y + 17);
        var rPoint = new Point(p.X - 10, p.Y + 17);

        var seg1 = new LineSegment() {Point = lPoint};
        pathFigure.Segments.Add(seg1);

        var seg2 = new LineSegment() {Point = rPoint};
        pathFigure.Segments.Add(seg2);

        var seg3 = new LineSegment() {Point = p};
        pathFigure.Segments.Add(seg3);

        pathGeometry.Figures.Add(pathFigure);
        var transform = new RotateTransform() {Angle = theta + 90, CenterX = p.X, CenterY = p.Y};
        pathGeometry.Transform = transform;

        var path = new Path() {Data = pathGeometry, Fill = Brushes.Black};
        transition.Arrow = path;
        Canvas.SetLeft(transition.Label, p.X - 25);
        Canvas.SetTop(transition.Label, p.Y - 45);
    }

    public void UpdatePath(Transition transition) //update start and end point of path and label position
    {
        var fromShape = transition.From.Shape;
        var toShape = transition.To.Shape;
        var p1 = new Point(Canvas.GetLeft(fromShape) + fromShape.Width / 2,
            Canvas.GetTop(fromShape) + fromShape.Width / 2);
        var p2 = new Point(Canvas.GetLeft(toShape) + toShape.Height / 2,
            Canvas.GetTop(toShape) + toShape.Height / 2);
        var isSamePoint = false;
        if (p1.Equals(p2))
        {
            isSamePoint = true;
            p1 = Point.Add(p1, new Vector(-15, -15));
            p2 = Point.Add(p2, new Vector(15, -15));
        }

        var theta = isSamePoint ? 0 : Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

        var arcPathGeometry = (PathGeometry)transition.Arc.Data;
        var arcFigure = arcPathGeometry.Figures[0];
        var arcSegment = (ArcSegment)arcFigure.Segments[0];

        arcFigure.StartPoint = p1;
        arcSegment.Point = p2;
        arcSegment.RotationAngle = theta;

        var pathGeometry = (PathGeometry)transition.Arrow.Data;

        if (Math.Abs(p1.X - p2.X) > 0.1 || Math.Abs(p1.Y - p2.Y) > 0.1)
        {
            arcPathGeometry.GetPointAtFractionLength(0.54, out var p, out _);
            var pathFigures = pathGeometry.Figures[0];
            pathFigures.StartPoint = p;

            var lPoint = new Point(p.X + 10, p.Y + 17);
            var rPoint = new Point(p.X - 10, p.Y + 17);

            ((LineSegment)pathFigures.Segments[0]).Point = lPoint;
            ((LineSegment)pathFigures.Segments[1]).Point = rPoint;
            ((LineSegment)pathFigures.Segments[2]).Point = p;

            var transform = (RotateTransform)pathGeometry.Transform;
            transform.Angle = theta + 90;
            transform.CenterX = p.X;
            transform.CenterY = p.Y;
        }




        UpdateTransitionLabelPos(transition);
    }

    public void UpdateTransitionLabelPos(Transition transition)
    {
        Canvas.SetLeft(transition.Label, transition.Arrow.ActualWidth - transition.Label.ActualWidth / 2);
        Canvas.SetTop(transition.Label, transition.Arrow.ActualHeight - 45);
    }

    public void UpdateUi(Shape shape, double deltaX, double deltaY, Point dragStartPos)
    {
        var node = SearchNode(shape); //find node
        var x = deltaX + dragStartPos.X;
        var y = deltaY + dragStartPos.Y;

        //update ellipse position
        Canvas.SetLeft(shape, x);
        Canvas.SetTop(shape, y);

        //update node label position
        var label = node.Label;
        Canvas.SetLeft(label, x + shape.ActualWidth / 2 - label.ActualWidth / 2);
        Canvas.SetTop(label, y + shape.ActualHeight / 2 - label.ActualHeight / 2);
            
        //update StartLine
        if (StartNode == node) UpdateStartLine(StartNode.Shape, StartLine);
            
        //update transitions
        var list = node.Transitions;
        foreach (var variable in list)
        {
            UpdatePath(variable);
        }
    }

    public void SetStarter(Node node, Canvas canvas)
    {
        if (StartNode != null)
        {
            StartNode.IsStarter = false;
            StartNode.Shape.Fill = Brushes.LightGray;
        }
        else
        {
            //draw the startline
            Panel.SetZIndex(StartLine,-1);
            canvas.Children.Add(StartLine);

        }
        UpdateStartLine(node.Shape, StartLine);
        StartNode = node;
        node.IsStarter = true;
        node.Shape.Fill = Brushes.LightGreen;
    }

    private void UpdateStartLine(Shape shape, Line line)
    {
        line.X1 = Canvas.GetLeft(shape) - 50;
        line.X2 = Canvas.GetLeft(shape);

        line.Y1 = line.Y2 = Canvas.GetTop(shape) + shape.Height / 2;
    }
}