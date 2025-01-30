using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LEAD_OLAP_DESINGER.ViewModels;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Media;
using LEAD_OLAP_DESINGER.Helpers;
using LEAD_OLAP_DESINGER.Models;
using System.Collections.ObjectModel;

namespace LEAD_OLAP_DESINGER;

public partial class ConnectionParam : Window
{
    private TextBox? nameBD;
    private TextBox? mainIP;
    private TextBox? mainPort;
    private TextBox? mainBD;
    private TextBox? mainUser;
    private TextBox? mainPassword;
    private ComboBox namePlatform;

    private Button save;

    private string jsonData;

    private int Id;

    private Grid conparam;
    public ConnectionParam()
    {
        InitializeComponent();

        conparam = this.FindControl<Grid>("Conparam");

        if (OperatingSystem.IsLinux())
        {
            this.Title = "��������� ����������� � �������� LEAD";
            conparam.IsVisible = false;
        }

        string configFolderPath = Path.Combine(DirectoryHelper.GetResDirectory(), "config");
        jsonData = Path.Combine(configFolderPath, "params.json");

        LoadDataFromJson();

        // �������� ListBox ��� �������� ������
        
        Settings.SettingUpdate();
        MainWindowViewModel.Platforms = new ObservableCollection<string>()
        {
            "MS SQL Server",
            "PostgreSQL"
        };
        NamePlatform.ItemsSource = MainWindowViewModel.Platforms;

        NamePlatform.SelectionChanged += SelectionChanged;

        UpdateListBox();

    }
    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            NamePlatform.SelectionChanged -= SelectionChanged;
            if (e.AddedItems.Count > -1)
            {
                var selectedItem = e.AddedItems[0];
                if (selectedItem != null)
                {
                    var ID = (ConnectionData)selectedItem;

                    Id = ID.Id;

                    string jsonString = File.ReadAllText(jsonData);

                    // �������� ��� �������� �� ConnectionList
                    var connections = JObject.Parse(jsonString)["ConnectionList"];

                    // ���������� �� ������� �������� � ���� ��������������� �� nameBD
                    foreach (var connection in connections)
                    {
                        int IDSL = (int)connection["Id"];

                        int DefaultPortMS = MainWindowViewModel.defaultPortMS;
                        int DefaultPortPG = MainWindowViewModel.defaultPortPG;

                        if (IDSL == Id)
                        {
                          
                          
                            NameBD.Text = connection["nameBD"].ToString();
                            //NamePlatform.SelectedItem = /*connection["namePlatform"].ToString();*/NamePlatform.Items.Cast<object>().FirstOrDefault(item => item.ToString() == connection["namePlatform"].ToString());
                            MainIP.Text = connection["mainIP"].ToString();
                            MainPort.Text = connection["mainPort"].ToString();
                            MainBD.Text = connection["mainBD"].ToString();
                            MainUser.Text = connection["mainUser"].ToString();
                            MainPassword.Text = EncryptionHelper.Decrypt(connection["mainPassword"].ToString());

                            NamePlatform.SelectedItem = connection["namePlatform"].ToString();

                            // ������ � ��� ���� ��� ����������� �������� ��� ���������� ��������
                            break;
                        }
                    }
                }
            }
        }
        catch
        {

        }
        finally
        {
            NamePlatform.SelectionChanged += SelectionChanged;
        }
    }
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void SelectionChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            // ��������, ��� SelectedItem �� null
            if (NamePlatform.SelectedItem is string selectedPlatform)
            {
                if (!string.IsNullOrEmpty(MainPort.Text))
                {
                    if (selectedPlatform.Contains("MS SQL Server"))
                    {
                        MainPort.Text = MainWindowViewModel.defaultPortMS.ToString();
                    }
                    else
                    {
                        MainPort.Text = MainWindowViewModel.defaultPortPG.ToString();
                    }
                }
            }
        }
        catch
        {
        }
    }

    private void InitializeComponents()
    {
        // ������������� ���������
        nameBD = this.FindControl<TextBox>("NameBD");
        namePlatform = this.FindControl<ComboBox>("NamePlatform");

        mainIP = this.FindControl<TextBox>("MainIP");
        mainPort = this.FindControl<TextBox>("MainPort");
        mainBD = this.FindControl<TextBox>("MainBD");
        mainUser = this.FindControl<TextBox>("MainUser");
        mainPassword = this.FindControl<TextBox>("MainPassword");

        save = this.FindControl<Button>("Save");

       // LoadDataFromJson(); // �������� ������ �� JSON �����

    }
    private List<ConnectionData> connectionList = new List<ConnectionData>();

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            NameBD.Text = string.Empty;
            MainIP.Text = string.Empty;
            MainBD.Text = string.Empty;
            MainUser.Text = string.Empty;
            MainPassword.Text = string.Empty;
           

            NamePlatform.SelectedItem = MainWindowViewModel.Platforms.FirstOrDefault(c => c == "MS SQL Server");

            MainPort.Text = MainWindowViewModel.defaultPortMS.ToString();

            string json = File.ReadAllText(jsonData);
            var existingData = JsonConvert.DeserializeObject<JObject>(json);

            var connectionList = existingData["ConnectionList"] as JArray;
            var existingNames = connectionList.Select(c => c["nameBD"].ToString()).ToList();

            string newConnectionName = "����� �����������";
            int suffix = 1;

            while (existingNames.Contains(newConnectionName))
            {
                newConnectionName = $"����� ����������� {suffix}";
                suffix++;
            }

            if (NameBD.Text != "" && items.Items.Count == 0)
            {
                var newConnectionData = new ConnectionData
                {

                    Id = connectionList.Count + 1,

                    nameBD = NameBD.Text,

                    mainIP = MainIP.Text,
                    mainPort = MainPort.Text,
                    mainBD = MainBD.Text,
                    mainUser = MainUser.Text,
                   
                    namePlatform = NamePlatform.SelectedItem.ToString(),

                };
                if (!string.IsNullOrWhiteSpace(newConnectionData.namePlatform) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainIP) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainPort) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainBD) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainUser) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainPassword))
                {
                    // Add the new connection data to the list in Linux
                    connectionList.Add(newConnectionData);
                }


                // ��������� ����� ������ � ������ connectionList

                (existingData["ConnectionList"] as JArray).Add(JObject.FromObject(newConnectionData));
            }
            else
            {
                var newConnectionData = new ConnectionData
                {


                    Id = connectionList.Count + 1,
                    nameBD = newConnectionName,
                    mainIP = !string.IsNullOrWhiteSpace(MainIP.Text) ? MainIP.Text : string.Empty,
                    mainPort = !string.IsNullOrWhiteSpace(MainPort.Text) ? MainPort.Text : string.Empty,
                    mainBD = !string.IsNullOrWhiteSpace(MainBD.Text) ? MainBD.Text : string.Empty,
                    mainUser = !string.IsNullOrWhiteSpace(MainUser.Text) ? MainUser.Text : string.Empty,
                    mainPassword = !string.IsNullOrWhiteSpace(MainPassword.Text) ? EncryptionHelper.Encrypt(MainPassword.Text) : string.Empty,
                    namePlatform = NamePlatform.SelectedItem?.ToString() ?? "MS SQL Server"

                };
                if (!string.IsNullOrWhiteSpace(newConnectionData.namePlatform) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainIP) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainPort) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainBD) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainUser) &&
                    !string.IsNullOrWhiteSpace(newConnectionData.mainPassword))
                {
                    // Add the new connection data to the list in Linux
                    connectionList.Add(newConnectionData);
                }


                // ��������� ����� ������ � ������ connectionList
                (existingData["ConnectionList"] as JArray).Add(JObject.FromObject(newConnectionData));
            }

            // ����������� ������ � JSON
            string updatedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);

            // ���������� ����������� JSON � ����
            File.WriteAllText(jsonData, updatedJson);

            UpdateListBox();

            // ���������� ������ ����������� ������
            int newIndex = connectionList.Count - 1;
            // ������������� ��������� ������ � ListBox
            items.SelectedIndex = newIndex;
            // MessageDialog.Show("","������ ���������!");
        }
        catch
        {

        }

    }

    [Obsolete]
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        try
        {

            string json = File.ReadAllText(jsonData);
            var existingData = JsonConvert.DeserializeObject<JObject>(json);

            // ������ ������ �� �����
            var connectionList = existingData["ConnectionList"] as JArray;
            var connectionToEdit = connectionList.FirstOrDefault(c => c["Id"].Value<int>() == Id);

            if (connectionToEdit != null)
            {
                int indexToEdit = connectionList.IndexOf(connectionToEdit);
                // �������� �������� ������
                connectionToEdit["nameBD"] = NameBD.Text;
                connectionToEdit["namePlatform"] = NamePlatform.SelectedItem.ToString();
                connectionToEdit["mainIP"] = MainIP.Text;
                connectionToEdit["mainPort"] = MainPort.Text;
                connectionToEdit["mainBD"] = MainBD.Text;
                connectionToEdit["mainUser"] = MainUser.Text;
                connectionToEdit["mainPassword"] = EncryptionHelper.Encrypt(MainPassword.Text);

                // ����������� ������ � JSON
                string updatedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);


                // ���������� ����������� JSON � ����
                File.WriteAllText(jsonData, updatedJson);
                UpdateListBox();

                if (indexToEdit > -1)
                {
                    items.SelectedIndex = indexToEdit;
                }
            }
        }
        catch
        {

        }
    }

    [Obsolete]
    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var confirmDialogd = new ConfirmDialogDelete();
            bool? confirmed = await confirmDialogd.ShowDialog<bool?>(this);

            if (confirmed == true)
            {
                string json = File.ReadAllText(jsonData);
                var existingData = JsonConvert.DeserializeObject<JObject>(json);

                // ������ ������ �� Id ��� ��������
                var connectionList = existingData["ConnectionList"] as JArray;
                var connectionToRemove = connectionList.FirstOrDefault(c => c["Id"].Value<int>() == Id);

                if (connectionToRemove != null)
                {
                    // ������� ������ ��������� ������
                    int indexToRemove = connectionList.IndexOf(connectionToRemove);
                    // ������� ������ �� ������
                    connectionList.RemoveAt(indexToRemove);

                    // ����������� ������ � JSON
                    string updatedJson = JsonConvert.SerializeObject(existingData, Formatting.Indented);

                    // ���������� ����������� JSON � ����
                    File.WriteAllText(jsonData, updatedJson);

                    UpdateListBox();

                    // ������������� ������ �� ������� ����, ���� ��������
                    if (indexToRemove > -1)
                    {
                        indexToRemove--; // ��������� �� ������� ����
                        items.SelectedIndex = indexToRemove;
                    }
                    else
                    {
                        items.SelectedIndex = -1; // ���� �� ������� ���������� �� ������� ����, ������� �����
                    }
                }
            }
        }
        catch
        {

        }
    }

    [Obsolete]
    private void UpdateListBox()
    {
        if (items.Items.Count > -1)
        {
            items.ItemsSource = null;
            NameBD.Text = string.Empty;
            MainIP.Text = string.Empty;
            MainPort.Text = string.Empty;
            MainBD.Text = string.Empty;
            MainUser.Text = string.Empty;
            MainPassword.Text = string.Empty;

            NamePlatform.SelectedItem = string.Empty;
        }
        connectionList.Clear();

        string json = File.ReadAllText(jsonData);
        if (File.Exists(jsonData))
        {

            var existingData = JsonConvert.DeserializeObject<dynamic>(json);

            if (existingData.ConnectionList != null)
            {
                foreach (var item in existingData.ConnectionList)
                {
                    var connection = new ConnectionData
                    {
                        nameBD = item.nameBD.ToString(),
                        namePlatform = item.namePlatform.ToString(),
                        Id = item.Id,
                    };

                    connectionList.Add(connection);
                }
                if (connectionList.Count == 0)
                {
                    Save.IsEnabled = false;
                }
                else
                {
                    Save.IsEnabled = true;
                }

                items.ItemsSource = connectionList;
                // ��������� ������ ������ � ListBox
                if (items.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem listBoxItem)
                {
                    listBoxItem.IsSelected = true;
                    listBoxItem.Focus();
                }
            }
        }

        items.ItemsSource = connectionList;
        int newIndex = 0;
        // ������������� ��������� ������ � ListBox
        items.SelectedIndex = newIndex;
        // MessageDialog.Show("","������ ���������!");
    }
    private void LoadDataFromJson()
    {
        try
        {
            if (!File.Exists(jsonData))
            {

                ConnectionParams connectionParams = new ConnectionParams
                {
                    ConnectionList = new List<ConnectionData> { }
                };


                string jsonD = JsonConvert.SerializeObject(connectionParams, Formatting.Indented);
                File.WriteAllText(jsonData, jsonD);
            }

            string json = File.ReadAllText(jsonData);

            var data = JsonConvert.DeserializeObject<dynamic>(json);

        }
        catch
        {

        }

    }
    private void CloseButtonPointerEnter(object sender, PointerEventArgs e)
    {
        Border border = (Border)sender;
        border.Background = new SolidColorBrush(Color.FromRgb(196, 43, 28));

        var textBlock = border.Child as TextBlock;
        textBlock.Foreground = new SolidColorBrush(Colors.White);
    }

    private void CloseButtonPointerLeave(object sender, PointerEventArgs e)
    {
        Border border = (Border)sender;
        border.Background = new SolidColorBrush(Colors.Transparent);

        var textBlock = border.Child as TextBlock;
        textBlock.Foreground = new SolidColorBrush(Colors.Black);
    }

    private void CloseButtonPointerPressed(object sender, PointerPressedEventArgs e)
    {
        Border border = (Border)sender;
        border.Background = new SolidColorBrush(Color.FromRgb(196, 43, 28));
        var textBlock = border.Child as TextBlock;
        textBlock.Foreground = new SolidColorBrush(Colors.White);
    }

    private void CloseButtonPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        this.Close();
    }
}