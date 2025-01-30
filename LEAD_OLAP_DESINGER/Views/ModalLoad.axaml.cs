using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LEAD_OLAP_DESINGER.Helpers;
using LEAD_OLAP_DESINGER.Models;
using LEAD_OLAP_DESINGER.MsgBox;
using LEAD_OLAP_DESINGER.ViewModels;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using static LEAD_OLAP_DESINGER.Models.DBParameters;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Avalonia.Data;

namespace LEAD_OLAP_DESINGER;

public partial class ModalLoad : Window
{
    private List<ConnectionData> connectionList = new List<ConnectionData>();
    private readonly ComboBox? nameBD;
    private Grid modalLoadgrid;
    private string jsonData;
    public ModalLoad()
    {
        InitializeComponent();

        modalLoadgrid = this.FindControl<Grid>("ModalLoadgrid");
        if (OperatingSystem.IsLinux())
        {
            this.Title = "Выбирете БД для подключения...";
            modalLoadgrid.IsVisible = false;
        }

        nameBD = this.FindControl<ComboBox>("NameBD");

        string configFolderPath = Path.Combine(DirectoryHelper.GetResDirectory(), "config");
        jsonData = Path.Combine(configFolderPath, "params.json");

        UpdateListBox();
    }

    private void UpdateListBox()
    {
        if (File.Exists(jsonData))
        {
            string json = File.ReadAllText(jsonData);
            var existingData = JsonConvert.DeserializeObject<dynamic>(json);

            if (existingData.ConnectionList != null)
            {
                foreach (var item in existingData.ConnectionList)
                {
                    var connection = new ConnectionData
                    {
                        nameBD = item.nameBD.ToString(),
                        namePlatform = item.namePlatform.ToString()
                    };

                    connectionList.Add(connection);
                }

                nameBD.ItemsSource = connectionList;
                nameBD.DisplayMemberBinding = new Binding("nameBD");

            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        MainWindowViewModel.conClose = false;
        this.Close();
    }
    private void nameBDSelection()
    {
        try
        {
            var selectedNameBD = (ConnectionData)nameBD.SelectedItem;

            int DefaultPortMS = MainWindowViewModel.defaultPortMS;
            int DefaultPortPG = MainWindowViewModel.defaultPortPG;

            if (selectedNameBD != null)
            {
                string jsonString = File.ReadAllText(jsonData);

                // Получаем все элементы из ConnectionList
                var connections = JObject.Parse(jsonString)["ConnectionList"];

                MainWindowViewModel.nameBD = selectedNameBD.nameBD;
                // Проходимся по каждому элементу и ищем соответствующий по nameBD
                foreach (var connection in connections)
                {
                    string nameBD = connection["nameBD"].ToString();

                    if (nameBD == MainWindowViewModel.nameBD)
                    {
                        //MainWindowViewModel.title = "Дизайнер семантического слоя БД - " + MainWindowViewModel.nameBD;
                        string platform = connection["namePlatform"].ToString();

                        if (platform == "MS SQL Server")
                        {
                            MainWindowViewModel.dbms = DBMS.MS;
                        }
                        else if (platform == "PostgreSQL")
                        {
                            MainWindowViewModel.dbms = DBMS.PG;
                        }

                        MainWindowViewModel.database = connection["mainBD"].ToString();
                        MainWindowViewModel.server = connection["mainIP"].ToString();

                        if (connection["mainPort"].ToString() != "")
                        {
                            MainWindowViewModel.port = (int)connection["mainPort"];
                        }
                        else
                        {
                            if (MainWindowViewModel.dbms == DBMS.PG)
                            {
                                MainWindowViewModel.port = DefaultPortPG;
                            }
                            else
                            {
                                MainWindowViewModel.port = DefaultPortMS;
                            }

                        }
                        MainWindowViewModel.username = connection["mainUser"].ToString();
                        MainWindowViewModel.password = EncryptionHelper.Decrypt(connection["mainPassword"].ToString());
                        

                     
                    }
                }

            }
        }
        catch (Exception ex)
        {
            MessageDialog.Show("nameBDSelection", ex.Message);
        }
    }

    [Obsolete]
    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        nameBDSelection();
        try
        {
            MainWindowViewModel.conClose = true;
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                string mssqlConnection = MainWindowViewModel.GetMssqlConnectionString(MainWindowViewModel.server, MainWindowViewModel.database, MainWindowViewModel.username, MainWindowViewModel.password);
                MainWindowViewModel.ConnectString = mssqlConnection;
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                string pgConnection = MainWindowViewModel.GetPgConnectionString(MainWindowViewModel.server, MainWindowViewModel.port, MainWindowViewModel.username, MainWindowViewModel.password, MainWindowViewModel.database);
                MainWindowViewModel.ConnectString = pgConnection;
            }
            
           
        }
        catch (Exception ex)
        {
            MainWindowViewModel.conClose = false;
            this.Close();
        }
        finally 
        {
          
            this.Close(); 
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