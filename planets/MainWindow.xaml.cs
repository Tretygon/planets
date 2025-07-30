using planets.Planets;
using System;
using System.CodeDom;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
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


namespace planets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<PlanetProperty> Properties { get; set; } = new();
        public ObservableCollection<Type> PropertyTypes { get; private set; } = [typeof(string), typeof(bool)];
        private bool popupActive = false;
        ObservableCollection<dynamic> Items = new();
        public MainWindow()
        {
            InitializeComponent();
            Backend.CreateDefaultDB();
            Backend.Read(null, out var Items_, out var Properties_);
            Properties = new(Properties_);
            Items = new(Items_);

            foreach (var p in Properties.Skip(2))
                AddGridCol(p.type, p.name);
            datagrid.ItemsSource = Items;
            RemovePropertyComboBox.ItemsSource = Properties;
            FilterComboBox.ItemsSource = Properties;
            AddPropertyTypeComboBox.ItemsSource = PropertyTypes;
        }


    /// <summary>
    /// add a new planet record
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
        private void AddPlanetButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic o = new ExpandoObject();
            var id = 0;
            if (Items.Count > 0)
            {
                id = Items.MaxBy(a => a.id).id + 1;
            }
            
            foreach (var prop in Properties)
            {
                if (prop.name == "id") {
                    o.id = id;
                }
                if (prop.type == typeof(string))
                    ((IDictionary<string, dynamic>)o)[prop.name] = "";
                else if (prop.type.IsValueType)
                    ((IDictionary<string, dynamic>)o)[prop.name] = Activator.CreateInstance(prop.type);
                else
                    ((IDictionary<string, dynamic>)o)[prop.name] = null;
            }
            
            Items.Add(o);
            Backend.Insert_new(null, id.ToString());
        }
        /// <summary>
        /// show popup to add a property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if (popupActive) return;
            AddPropertyPopup.IsOpen = true;
            popupActive = true;
        }
        /// <summary>
        /// add a column to the datagrid
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AddGridCol(Type type, string name)
        {
            DataGridColumn dataCol = null;
            if (type == typeof(bool))
                dataCol = new DataGridCheckBoxColumn() { Binding=new Binding(name) };

            else if (type == typeof(string) || type == typeof(int))
                dataCol = new DataGridTextColumn() { Binding = new Binding(name) };
            else
                throw new NotImplementedException();

            dataCol.Header = name;
            dataCol.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            datagrid.Columns.Add(dataCol);
        }

        /// <summary>
        /// adds a new property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AddProperty_OK(object sender, RoutedEventArgs e)
        {
            var propName = AddPropertyTextBox.Text as string;
            AddPropertyTextBox.Text = "";
            var propType = AddPropertyTypeComboBox.SelectedItem as Type;
            AddPropertyTypeComboBox.SelectedIndex = -1;
            if (propName.Length > 0 && propType != null && !Properties.Select(a=>a.name).Contains(propName))
            {
                var prop = new PlanetProperty(propName, propType as Type);
                Properties.Add(prop);
                foreach (var dyn in Items)
                {
                    if (propType == typeof(string))
                        dyn.prop = "";
                    else if (propType == typeof(bool))
                        dyn.prop = false;
                    else
                        throw new NotImplementedException();
                }
                AddGridCol(prop.type, prop.name);
                Backend.AddProperty(null, propName, propType);

            }
            AddPropertyPopup.IsOpen = false;
            popupActive = false;
        }
        private void AddProperty_Cancel(object sender, RoutedEventArgs e)
        {
            AddPropertyTextBox.Text = "";
            AddPropertyTypeComboBox.SelectedIndex = -1;
            AddPropertyPopup.IsOpen = false;
            popupActive = false;
        }
        private void RemoveProperty_Cancel(object sender, RoutedEventArgs e)
        {
            RemovePropertyPopup.IsOpen = false;
            popupActive = false;
        }
        private void Filter_Cancel(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = false;
            popupActive = false;
            datagrid.ItemsSource = Items;
        }
        /// <summary>
        /// filters the datagrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Filter_OK(object sender, RoutedEventArgs e)
        {
            var prop_i = FilterComboBox.SelectedIndex;
            var val = filterTextBox.Text;
            if (prop_i != -1 && val.Length != 0)
            {
                var prop = Properties[prop_i];
                datagrid.ItemsSource = new ObservableCollection<dynamic>(Items.Where(dyn => ((IDictionary<string, dynamic>)dyn)[prop.name].ToString() == val));

            }
            FilterPopup.IsOpen = false;
            popupActive = false;
        }
        /// <summary>
        /// removes an existing property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveProperty_OK(object sender, RoutedEventArgs e)
        {
            var prop_i = RemovePropertyComboBox.SelectedIndex;
            RemovePropertyComboBox.SelectedIndex = -1;
            if (prop_i != -1 )
            {
                prop_i += 2;
                var prop = Properties[prop_i];
                //var  = properties.IndexOf(prop);
                datagrid.Columns.RemoveAt(prop_i + 2);
                Properties.RemoveAt(prop_i);
                foreach (var dyn in Items)
                {
                    ((IDictionary<string, object>)dyn).Remove(prop.name);
                }
                Backend.DeletePropertyTable(null, prop.name);
            }
            
            RemovePropertyPopup.IsOpen = false;
            popupActive = false;
        }
        /// <summary>
        /// shows a popup to remove a property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (popupActive) return;
            if (Properties.Count > 2)
            {
                RemovePropertyComboBox.ItemsSource = Properties.Skip(2).ToList();
                RemovePropertyPopup.IsOpen = true;
                popupActive = true;
            }
        }
/// <summary>
/// shows a popup to filter the datagrid
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (popupActive) return;
            FilterPopup.IsOpen = true;
            popupActive = true;
        }
        /// <summary>
        /// removes the row selected on the datagrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemovePlanetButton_Click(object sender, RoutedEventArgs e)
        {
            var i = datagrid.SelectedIndex;
            if (i != -1)
            {
                var id = Items[i].id;
                Items.RemoveAt(i);
                Backend.Delete(null, id);
            }
        }


        /// <summary>
        /// triggered when there is an edit in the datagrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void datagrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row != null && e.Column != null)
                {
                    var item = e.Row.Item as dynamic;
                    var col_i = e.Column.DisplayIndex;
                    var id = item.id;

                    if (col_i == 1)
                    {
                        var val = item.name;
                        Backend.Update(null, id, "name", (e.EditingElement as TextBox).Text, typeof(string));
                    }
                    else
                    {
                        var prop = Properties[col_i];
                        if (e.EditingElement is TextBox tb)
                        {
                            ((IDictionary<string, dynamic>)item)[prop.name] = tb.Text.ToString();
                            Backend.Update(null, id, prop.name, tb.Text.ToString(), prop.type);
                        }
                        else if (e.EditingElement is CheckBox cb)
                        {
                            //((IDictionary<string, dynamic>)item)[prop.name] = cb.IsChecked;
                            Backend.Update(null, id, prop.name, cb.IsChecked.ToString(), prop.type);
                        }

                    }

                }
            }
        }
    }
    static class Extensions
    {
        public static KeyValuePair<string, object> WithValue(this string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        public static ExpandoObject Init(
            this ExpandoObject expando, params KeyValuePair<string, object>[] values)
        {
            foreach (KeyValuePair<string, object> kvp in values)
            {
                ((IDictionary<string, Object>)expando)[kvp.Key] = kvp.Value;
            }
            return expando;
        }
    }
}
