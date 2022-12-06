using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NFA_Convertor.Classes;

public class CustomCanvas : Canvas
{
    private Point _dragStartPos, _startDragPoint, _endDragPoint, _rClickPoint;

    private Shape _draggedNode, _selectedNode, _transitionNode1;
    private bool _addingTransition;
        
    public readonly Machine Machine;
   
    private readonly Random _random = new Random();

    public CustomCanvas()
    {
        Background = Brushes.White;
        PreviewMouseLeftButtonUp += (sender, args) =>//deselect node
        {
            _selectedNode = null;
            _draggedNode = null;
        };
        PreviewMouseMove += ProgramCanvas_PreviewMouseMove;
        PreviewMouseRightButtonUp += ProgramCanvas_PreviewMouseRightButtonUp;
        Machine = new Machine();
    }

    private void ProgramCanvas_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)//open context menu or disable adding Transition
    {
        if (!_addingTransition)
        {
            var contextMenu = new ContextMenu();
            _rClickPoint = Mouse.GetPosition(this);
            contextMenu.Placement = PlacementMode.MousePoint;

            var miState = new MenuItem() { Header = "New Node" };

            miState.Click += (o, args) => NewNode();

            contextMenu.Items.Add(miState);
            contextMenu.IsOpen = true;
        }
        else _addingTransition = false;
    }

    private void ProgramCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_addingTransition)//dragging node
        {
            if (_draggedNode == null) return;

            _endDragPoint.X = e.GetPosition(this).X;
            _endDragPoint.Y = e.GetPosition(this).Y;

            var deltaX = _endDragPoint.X - _startDragPoint.X;
            var deltaY = _endDragPoint.Y - _startDragPoint.Y;

            //updating UI
            Machine.UpdateUi(_draggedNode,deltaX,deltaY, _dragStartPos);

        }
            
    }

    private void NodeShapeOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)// apply transition and add path to canvas
    {
        _selectedNode = sender as Shape;
        if (!_addingTransition) return;
        Machine.AddTransition(Machine.SearchNode(_transitionNode1),Machine.SearchNode(_selectedNode), this);
        
        _selectedNode = null;
        _addingTransition = false;

    }

    private void NodeShapeOnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)//create context menu for node
    {
        var contextMenu = new ContextMenu();
        _selectedNode = sender as Shape;
        var node = Machine.SearchNode(_selectedNode);
        contextMenu.Placement = PlacementMode.MousePoint;

        var nodeMiAddTransition = new MenuItem() { Header = "Add Transition" };
        var nodeMiDelete = new MenuItem() { Header = "Delete" };
        var nodeMiToggleFinal = new MenuItem() { Header = "Toggle Final", IsChecked = node.IsFinal};
        var nodeMiSetStarter = new MenuItem() { Header = "Set Starter", IsEnabled = !node.IsStarter}; 
            

        nodeMiAddTransition.Click += (o, args) =>
        {
            _addingTransition = true;
            _transitionNode1 = _selectedNode;
        };
            
        nodeMiDelete.Click += (o, args) =>
        {
            Machine.RemoveNode(node, this);
        };
            
        nodeMiToggleFinal.Click += (o, args) => Machine.ToggleFinalState(node);
        nodeMiSetStarter.Click += (o, args) => Machine.SetStarter(node, this);
            

        contextMenu.Items.Add(nodeMiAddTransition);
        contextMenu.Items.Add(nodeMiDelete);
        contextMenu.Items.Add(nodeMiToggleFinal);
        contextMenu.Items.Add(nodeMiSetStarter);

        if (node.HasSubNodes)
        {
            var nodeMiSubNodes = new MenuItem() { Header = "SubNodes" };
            nodeMiSubNodes.Click += StateMiPropertiesOnClick;
            contextMenu.Items.Add(nodeMiSubNodes);
        } 

        contextMenu.IsOpen = true;
    }

    private void StateMiPropertiesOnClick(object sender, RoutedEventArgs e)//show subNodes of a node
    {
        var node = Machine.SearchNode(_selectedNode);
        
        var listview = new ItemsControl();
        var properties = new Window() { Width = 225.0d, Height = 175.0d, Content = listview, Title = node.Index + "'s SubNodes", ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen};

        properties.SourceInitialized += (s, ee) =>
        {
            IconHelper.RemoveIcon(properties);
            IconHelper.HideMinimizeAndMaximizeButtons(properties);
        };
            
        var str = "{" + string.Join(", ", node.SubNodes.Select((node1, i) => node1.Index )) + "}";
        listview.Items.Add(new TextBlock(){TextWrapping = TextWrapping.Wrap,Text = str});
            
        properties.ShowDialog();
    }

    private void NodeShapeOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)//get pos of node for dragging or 
    {
        var shape = sender as Shape;
        if (!_addingTransition)
        {
            _startDragPoint.X = e.GetPosition(this).X;
            _startDragPoint.Y = e.GetPosition(this).Y;

            if (shape == null) return;
            _dragStartPos.X = GetLeft(shape);
            _dragStartPos.Y = GetTop(shape);

            _draggedNode = shape;
        }
    }

    internal void PathOnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)//context menu for transition line
    {
        _selectedNode = sender as Shape;
        var contextMenu = new ContextMenu();
        var transitionMenuMiSetLetters = new MenuItem() { Header = "Set Letters" };
        var transitionMenuMiDelete = new MenuItem() { Header = "Delete" };

        transitionMenuMiSetLetters.Click += TransitionMenuMiSetLettersOnClick;
            
        transitionMenuMiDelete.Click += (o, args) =>//delete transition
        {
            Machine.RemoveTransition(_selectedNode, this);
            _selectedNode = null;
        };

        contextMenu.Items.Add(transitionMenuMiSetLetters);
        contextMenu.Items.Add(transitionMenuMiDelete);

        contextMenu.IsOpen = true;
    }

    private void TransitionMenuMiSetLettersOnClick(object sender, RoutedEventArgs e)
    {
        var transition = Machine.SearchTransition(_selectedNode); //get the transition
        var label = transition.Label;
        var letters = transition.Letters;
        
        var list = Machine.Alphabet;//list of all letters

        var addItem = new Action<string, ListView>((item, lv) =>
        {
            var checkbox = new CheckBox() {Content = item,IsChecked = letters.Contains(item)};
            checkbox.Checked += (o, args) =>
            {
                letters.Add(item);
                label.Content = transition.LetterList;
                Machine.UpdateTransitionLabelPos(transition);
            };
            checkbox.Unchecked += (o, args) =>
            {
                letters.Remove(item);
                label.Content = transition.LetterList;
                Machine.UpdateTransitionLabelPos(transition);
            };
            lv.Items.Add(checkbox);
        });

        //add alphabet to listView
        var listView = new ListView();
        addItem(Machine.Lambda, listView);
        foreach (var item in list)
        {
            addItem(item,listView);
        }

        var border = new Border() {BorderThickness = new Thickness(2), Background = Brushes.WhiteSmoke,Child = listView};
        var window = new Window() { Width = 225.0d, Height = 350.0d, Content = border, Title = "Letters", ResizeMode = ResizeMode.NoResize};
        window.ShowDialog();
    }

    public void AlphabetList()
    {
        var grid = new Grid();

        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(9, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });


        var window = new Window() { Width = 225.0d, Height = 350.0d, Content = grid, Title = "Alphabet list", ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen};

        window.SourceInitialized += (s, e) =>
        {
            IconHelper.RemoveIcon(window);
            IconHelper.HideMinimizeAndMaximizeButtons(window);
        };


        var list = new ListBox();
        list.SetValue(Grid.ColumnSpanProperty, 2);
        list.ItemsSource = Machine.Alphabet;

        var addBtn = new Button() { Content = "Add"};
        addBtn.Click += (s, e) =>//if add button clicked
        {
            var addGrid = new Grid();

            addGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            addGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.5, GridUnitType.Star) });

            var addWindow = new Window() { Width = 225.0d, Height = 100.0d, Content = addGrid,
                Title = "Add Alphabet Letter", ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen};
            
            var txtBox = new TextBox();
            txtBox.SetValue(Grid.RowProperty, 0);

            var addButton = new Button() { Content = "Confirm"};
            addButton.SetValue(Grid.RowProperty, 1);

            addWindow.SourceInitialized += (ss, ee) =>
            {
                IconHelper.RemoveIcon(addWindow);
                IconHelper.HideMinimizeAndMaximizeButtons(addWindow);
                txtBox.Focus();
            };

            addButton.Click += (sss, eee) =>//add new letter
            {
                Machine.Alphabet.Add(txtBox.Text);

                list.Items.Refresh();

                addWindow.Close();
                
                list.SelectedIndex = list.Items.Count - 1;
            };

            addGrid.Children.Add(txtBox);
            addGrid.Children.Add(addButton);
            
            addWindow.ShowDialog();
        };
        addBtn.SetValue(Grid.ColumnProperty, 0);
        addBtn.SetValue(Grid.RowProperty, 1);

        var deleteBtn = new Button() { Content = "Delete", IsEnabled = false};
        deleteBtn.SetValue(Grid.ColumnProperty, 1);
        deleteBtn.SetValue(Grid.RowProperty, 1);

        var item = "";
        list.SelectionChanged += (s, e) =>//check for item
        {
            if (list.SelectedIndex is -1) deleteBtn.IsEnabled = false;
            else
            {
                item = list.SelectedItem as string;
                deleteBtn.IsEnabled = true;
            }

        };

        deleteBtn.Click += (s, e) => //delete selected letter
        {
            item = list.SelectedItem as string;
            Machine.Alphabet.Remove(item);
            list.Items.Refresh();
        };

        grid.Children.Add(list);
        grid.Children.Add(addBtn);
        grid.Children.Add(deleteBtn);
            
        window.ShowDialog();
    }

    public void LoadMachine(Machine machine)
    {
        Machine.Alphabet = machine.Alphabet;
        
        //creating the Nodes
        var nodes = machine.Nodes;
        foreach (var node in nodes)
        {
            var newNode = Machine.AddNode(node.SubNodes);

            newNode.Index = node.Index;
                
            NewNode(newNode);
                
            if (node.IsFinal) Machine.ToggleFinalState(newNode);
            if (node.IsStarter) Machine.SetStarter(newNode, this);
        }
            
        //drawing transitions
        foreach (var node in nodes)
        {
            foreach (var transition in node.Transitions)
            {
                var from = transition.From;
                var to = transition.To;

                var txtF = from.Index;
                var txtT = to.Index;

                var letters = transition.Letters;

                var newFrom = Machine.SearchNode(txtF);
                var newTo = Machine.SearchNode(txtT);
                
                Machine.AddTransition(newFrom, newTo, this, letters);
            }
        }
    }

    private void NewNode(Node n = null)
    {
        
        Node node;
        Shape nodeShape;
        Label nodeLabel;

        if (n == null)
        {
            node = Machine.AddNode();
            nodeShape = node.Shape;
            nodeLabel = node.Label;
            
            SetLeft(nodeShape, _rClickPoint.X - nodeShape.Width / 2); 
            SetTop(nodeShape, _rClickPoint.Y - nodeShape.Height / 2);
            
            SetLeft(nodeLabel, _rClickPoint.X-8); 
            SetTop(nodeLabel,   _rClickPoint.Y-13);
        }
        else
        {
            node = n;
            nodeShape = node.Shape;
            nodeLabel = node.Label;
            
            var x = _random.Next(50, 600);
            var y = _random.Next(50, 400);
                
            SetLeft(nodeShape,x);
            SetTop(nodeShape,y);
                
            SetLeft(nodeLabel, x+19); 
            SetTop(nodeLabel, y+15);
        }
        
        
        Children.Add(nodeShape);
        

        Children.Add(nodeLabel);

        nodeShape.PreviewMouseLeftButtonDown += NodeShapeOnPreviewMouseLeftButtonDown;
        nodeShape.PreviewMouseLeftButtonUp += NodeShapeOnPreviewMouseLeftButtonUp;
        nodeShape.PreviewMouseRightButtonUp += NodeShapeOnPreviewMouseRightButtonUp;
    }
    
   
}

public static class IconHelper
{
    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x,
        int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr
        lParam);

    const int GWL_EXSTYLE = -20;
    const int WS_EX_DLGMODALFRAME = 0x0001;
    const int SWP_NOSIZE = 0x0001;
    const int SWP_NOMOVE = 0x0002;
    const int SWP_NOZORDER = 0x0004;
    const int SWP_FRAMECHANGED = 0x0020;
    const uint WM_SETICON = 0x0080;
    private const int GWL_STYLE = -16,
        WS_MAXIMIZEBOX = 0x10000,
        WS_MINIMIZEBOX = 0x20000;

    public static void RemoveIcon(Window window)
    {
        // Get this window's handle
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        // Change the extended window style to not show a window icon
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);
        // Update the window's non-client area to reflect the changes
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE |
                                                    SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    public static void HideMinimizeAndMaximizeButtons(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

        SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
    }
}