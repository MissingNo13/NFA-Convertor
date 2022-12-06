using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using NFA_Convertor.Classes;
namespace NFA_Convertor
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
public partial class MainWindow
    {

        private TabItem _selectedItem;
        private List<TabItem> tabList = new List<TabItem>();
        
        private CustomCanvas CurrentCanvas => ((TabControl.SelectedItem as TabItem)?.Content as Border)?.Child as CustomCanvas;

        public MainWindow()
        {
            InitializeComponent();
            NewTab();
            MenuAlphabet.Click += (sender, args) => CurrentCanvas.AlphabetList();
            MenuExit.Click += (sender, args) => Application.Current.Shutdown();
            MenuNewFile.Click += (sender, args) => NewTab();
            MenuConvertNfa.Click += MenuConvertNfaOnClick;
            MenuConvertNfaText.Click += MenuConvertNfaTextOnClick;
            MenuSave.Click += MenuSaveAsOnClick;
            MenuOpen.Click += MenuOpenOnClick;
        }

        private void MenuOpenOnClick(object sender, RoutedEventArgs e)//load a file
        {
            var window = new OpenFileDialog()
            {
                DefaultExt = "mcn",
                Filter = "Machine file (*.mcn)|*.mcn"
            };

            if (window.ShowDialog() is true)
            {
                var file = new StreamReader(window.FileName);
                var serializer = new JsonSerializer();
                var mStruct = (MachineStruct)serializer.Deserialize(file, typeof(MachineStruct));
                file.Close();

                var machine = MachineConvertor.LoadFromStruct(mStruct);
                
                var newCanvas = NewTab((window.SafeFileName).Remove(window.SafeFileName.Length - 4));
                newCanvas.LoadMachine(machine);
            }
        }

        private void MenuSaveAsOnClick(object sender, RoutedEventArgs e)//save a file
        {
            var machine = CurrentCanvas.Machine;

            var mStruct = MachineConvertor.SaveToStruct(machine);
            var window = new SaveFileDialog
            {
                DefaultExt = "mcn",
                Filter = "Machine file (*.mcn)|*.mcn",
                FileName = (TabControl.SelectedItem as TabItem).Header as string
            };

            if (window.ShowDialog() is true)
            {
                var fileName = (window.SafeFileName).Remove(window.SafeFileName.Length - 4);
                var file = new StreamWriter(window.FileName);
                var serializer = new JsonSerializer();
                serializer.Serialize(file, mStruct);
                file.Close();
                ((TabItem) TabControl.SelectedItem).Header = fileName;
            }
        }

        private void MenuConvertNfaOnClick(object sender, RoutedEventArgs e) //convert nfa to dfa
        {
            var nfa = CurrentCanvas.Machine;
            if (nfa.StartNode == null)
            {
                MessageBox.Show("There is no Start Node in this Machine.", "Error");
                return;
            }
            var dfa = MachineConvertor.ConvertNfa(nfa);
            var newCanvas = NewTab();
            newCanvas.LoadMachine(dfa);
        }
        
        private void MenuConvertNfaTextOnClick(object sender, RoutedEventArgs e) //convert nfa to dfa and save as a text file
        {
            var nfa = CurrentCanvas.Machine;
            if (nfa.StartNode == null)
            {
                MessageBox.Show("There is no Start Node in this Machine.", "Error");
                return;
            }
            var dfa = MachineConvertor.ConvertNfaAsText(nfa);
            
            var window = new SaveFileDialog
            {
                DefaultExt = "mcn",
                Filter = "Text file (*.mcn)|*.txt",
                FileName = (TabControl.SelectedItem as TabItem)?.Header as string
            };

            if (window.ShowDialog() is true)
            {
                File.WriteAllText(window.FileName, dfa);
            }
            
        }

        private void Item_MouseRightButtonUp(object sender, MouseButtonEventArgs e) //open context menu for tabItem
        {
            _selectedItem = sender as TabItem;
            var contextMenu = new ContextMenu();
            var miClose = new MenuItem() { Header = "Close" };

            contextMenu.Items.Add(miClose);
            miClose.Click += (o, args) =>
            {
                TabControl.Items.Remove(_selectedItem);
                tabList.Remove(_selectedItem);
                _selectedItem = null;
            };

            contextMenu.IsOpen = true;
        }

        private CustomCanvas NewTab(string name = "Untitled")//create new tab
        {
            var itemContent = new Border() {BorderThickness = new Thickness(3),Background = Brushes.Gray};
            
            var canvas = new CustomCanvas();
            itemContent.Child = canvas; 
            var item = new TabItem() { Content = itemContent, Header = name };

            item.MouseRightButtonUp += Item_MouseRightButtonUp;
            tabList.Add(item);
            TabControl.Items.Add(tabList[tabList.Count - 1]);
            TabControl.SelectedIndex = tabList.Count - 1;
            return canvas;
        }
    }
}
