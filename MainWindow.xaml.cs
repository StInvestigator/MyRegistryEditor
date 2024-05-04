using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Microsoft.Win32;
using static MyRegistryEditor.MainWindow;

namespace MyRegistryEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RegistryKey selectedKey;
        List<RegistryValue> registryValues;
        public class RegistryValue
        {
            public string Name { get; set; }
            public RegistryValueKind? Kind { get; set; }
            public string? Value { get; set; }
            public RegistryValue(string name, RegistryValueKind? kind, string? value)
            {
                Name = name;
                Kind = kind;
                Value = value;
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            createTree();
        }

        void createTree()
        {
            TreeViewItem classesRoot = new TreeViewItem();
            TreeViewItem localMachine = new TreeViewItem();
            TreeViewItem currentUser = new TreeViewItem();
            TreeViewItem users = new TreeViewItem();
            TreeViewItem currentConfig = new TreeViewItem();

            classesRoot.Header = Registry.ClassesRoot.Name;
            localMachine.Header = Registry.LocalMachine.Name;
            currentUser.Header = Registry.CurrentUser.Name;
            users.Header = Registry.Users.Name;
            currentConfig.Header = Registry.CurrentConfig.Name;

            //foreach (var treeViewItem in assignAllKeys(Registry.ClassesRoot))
            //{
            //    classesRoot.Items.Add(treeViewItem);
            //}
            //foreach (var treeViewItem in assignAllKeys(Registry.LocalMachine))
            //{
            //    localMachine.Items.Add(treeViewItem);
            //}

            foreach (var treeViewItem in assignAllKeys(Registry.CurrentUser))
            {
                currentUser.Items.Add(treeViewItem);
            }

            //foreach (var treeViewItem in assignAllKeys(Registry.Users))
            //{
            //    users.Items.Add(treeViewItem);
            //}

            foreach (var treeViewItem in assignAllKeys(Registry.CurrentConfig))
            {
                currentConfig.Items.Add(treeViewItem);
            }

            treeView.SelectedItemChanged -= treeViewItemSelected;

            treeView.Items.Clear();

            treeView.SelectedItemChanged += treeViewItemSelected;

            treeView.Items.Add(classesRoot);
            treeView.Items.Add(currentUser);
            treeView.Items.Add(localMachine);
            treeView.Items.Add(users);
            treeView.Items.Add(currentConfig);

        }

        void treeViewItemSelected(object sender, RoutedEventArgs e)
        {
            if (selectedKey != null) selectedKey.Close();
            try
            {
                TreeViewItem treeViewItem = (TreeViewItem)treeView.SelectedItem;

                string result = Convert.ToString(treeViewItem.Header)??"";

                for (var i = treeViewItem.Parent as TreeViewItem; i != null; i = i.Parent as TreeViewItem)
                {
                    result = i.Header + "\\" + result;
                }


                registryValues = new List<RegistryValue>();
                RegistryKey registryKey;

                if (result.Split('\\')[0] == Registry.CurrentUser.Name) { registryKey = Registry.CurrentUser; }
                else if(result.Split('\\')[0] == Registry.LocalMachine.Name) { registryKey = Registry.LocalMachine; }
                else if(result.Split('\\')[0] == Registry.Users.Name) { registryKey = Registry.Users; }
                else if(result.Split('\\')[0] == Registry.CurrentConfig.Name) { registryKey = Registry.CurrentConfig; }
                else { registryKey = Registry.ClassesRoot; }

                if(result.Split('\\').Length!=1)
                    registryKey = registryKey.OpenSubKey(result.Replace(result.Split('\\')[0] + "\\", "").Trim(),true);
                foreach (var item in registryKey.GetValueNames())
                {
                    registryValues.Add(new RegistryValue(item, registryKey.GetValueKind(item), registryKey.GetValue(item)?.ToString()));
                }
                selectedKey = registryKey;
                dataGrid.ItemsSource = registryValues;
            }
            catch { }
        }

        List<TreeViewItem> assignAllKeys(RegistryKey registryKey)
        {
            List<TreeViewItem> treeViewItems = new List<TreeViewItem>();
            foreach (var key in registryKey.GetSubKeyNames())
            {
                TreeViewItem treeViewItem = new TreeViewItem();
                treeViewItem.Header = key;
                try
                {
                    var subKey = registryKey.OpenSubKey(key);
                    if (subKey.GetSubKeyNames().Length != 0)
                    {
                        foreach (var item in assignAllKeys(subKey))
                        {
                            treeViewItem.Items.Add(item);
                        }
                    }
                    subKey.Close();
                }
                catch { }
                treeViewItems.Add(treeViewItem);
            }

            return treeViewItems;
        }

        private void dataGrid_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedValue != null)
            {
                TBName.Text = (dataGrid.SelectedValue as RegistryValue).Name;
                CBType.Text = (dataGrid.SelectedValue as RegistryValue).Kind.ToString();
                TBValue.Text = (dataGrid.SelectedValue as RegistryValue).Value;
            }
            else
            {
                TBName.Text = "";
                CBType.Text = "";
                TBValue.Text = "";
            }
        }

        private void create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TBName.Text != "" && TBValue.Text != "")
                {
                    var kind = CBType.Text == "Binary" ? RegistryValueKind.Binary :
                        CBType.Text == "ExpandString" ? RegistryValueKind.ExpandString :
                        CBType.Text == "String" ? RegistryValueKind.String :
                        CBType.Text == "DWord" ? RegistryValueKind.DWord :
                        RegistryValueKind.None;
                    Registry.SetValue(selectedKey.Name,TBName.Text, TBValue.Text, kind);
                    var find = registryValues.Find((rv) => rv.Name == TBName.Text);
                    if (find != null) { find.Value = TBValue.Text; find.Kind = kind; }
                    else registryValues.Add(new RegistryValue(TBName.Text, kind, TBValue.Text));
                    dataGrid.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("Name and value can`t be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)  { MessageBox.Show(ex.Message,"Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedName = (dataGrid.SelectedValue as RegistryValue).Name;
                selectedKey.DeleteValue(selectedName);
                registryValues.RemoveAt(registryValues.FindIndex((rv) => rv.Name == selectedName));
                dataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void deleteKey_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey registryKey;
            if (selectedKey.Name.Split('\\')[0] == Registry.CurrentUser.Name) { registryKey = Registry.CurrentUser; }
            else if (selectedKey.Name.Split('\\')[0] == Registry.LocalMachine.Name) { registryKey = Registry.LocalMachine; }
            else if (selectedKey.Name.Split('\\')[0] == Registry.Users.Name) { registryKey = Registry.Users; }
            else if (selectedKey.Name.Split('\\')[0] == Registry.CurrentConfig.Name) { registryKey = Registry.CurrentConfig; }
            else { registryKey = Registry.ClassesRoot; }

            if (selectedKey.Name.Split('\\').Length != 1)
                registryKey.DeleteSubKeyTree(selectedKey.Name.Replace(selectedKey.Name.Split('\\')[0] + "\\", "").Trim());

            TreeViewItem? currentSelection = null;

            Debug.WriteLine(selectedKey.Name.Split('\\')[0]);
            foreach (var item in treeView.Items)
            {
                if ((item as TreeViewItem).Header.ToString() == selectedKey.Name.Split('\\')[0])
                {
                    currentSelection = item as TreeViewItem;
                    break;
                }
            }

            if (currentSelection != null)
            {
                int index = 0;
                for (int i = 1; i < selectedKey.Name.Split('\\').Length; i++)
                {
                    index = 0;
                    foreach (var item in currentSelection.Items)
                    {
                        if ((item as TreeViewItem).Header.ToString() == selectedKey.Name.Split('\\')[i])
                        {
                            if (i != selectedKey.Name.Split('\\').Length - 1)
                            {
                                currentSelection = item as TreeViewItem;
                            }
                            break;
                        }
                        index++;
                    }
                }
                Debug.WriteLine(currentSelection.Header);
                currentSelection.Items.RemoveAt(index);
            }
        }

        private void createKey_Click(object sender, RoutedEventArgs e)
        {
            if (TBNameKey.Text != "")
            {
                try
                {
                    Registry.SetValue(selectedKey.Name + '\\' + TBNameKey.Text, "test", "test", RegistryValueKind.String);

                    TreeViewItem? currentSelection = null;

                    foreach (var item in treeView.Items)
                    {
                        if ((item as TreeViewItem).Header.ToString() == selectedKey.Name.Split('\\')[0])
                        {
                            currentSelection = item as TreeViewItem;
                            break;
                        }
                    }

                    if (currentSelection != null)
                    {
                        for (int i = 1; i < selectedKey.Name.Split('\\').Length; i++)
                        {
                            foreach (var item in currentSelection.Items)
                            {
                                if ((item as TreeViewItem).Header.ToString() == selectedKey.Name.Split('\\')[i])
                                {
                                    currentSelection = item as TreeViewItem;
                                }
                            }
                        }
                        TreeViewItem treeViewItem = new TreeViewItem();
                        treeViewItem.Header = TBNameKey.Text;
                        currentSelection.Items.Add(treeViewItem);
                    }
                }
                catch
                {
                    MessageBox.Show("Access is denied", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}