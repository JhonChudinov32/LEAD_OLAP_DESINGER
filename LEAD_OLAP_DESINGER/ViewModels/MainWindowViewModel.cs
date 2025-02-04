using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using LEAD_OLAP_DESINGER.Class;
using LEAD_OLAP_DESINGER.Models;
using LEAD_OLAP_DESINGER.MsgBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static LEAD_OLAP_DESINGER.Models.DBParameters;

namespace LEAD_OLAP_DESINGER.ViewModels
{
public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private GridLength _column1Width = new GridLength(50, GridUnitType.Star);
        public GridLength Column1Width
        {
            get => _column1Width;
            set
            {
                _column1Width = value;
                OnPropertyChanged(nameof(Column1Width));
            }
        }

        private GridLength _column2Width = new GridLength(50, GridUnitType.Star);
        public GridLength Column2Width
        {
            get => _column2Width;
            set
            {
                _column2Width = value;
                OnPropertyChanged(nameof(Column2Width));
            }
        }

        private GridLength _column3Width = new GridLength(50, GridUnitType.Star);
        public GridLength Column3Width
        {
            get => _column3Width;
            set
            {
                _column3Width = value;
                OnPropertyChanged(nameof(Column3Width));
            }
        }
        public void UpdateColumnWidths(double totalWidth)
        {
            Column1Width = new GridLength(totalWidth * 0.3, GridUnitType.Pixel);
            Column2Width = new GridLength(totalWidth * 0.4, GridUnitType.Pixel);
            Column3Width = new GridLength(totalWidth * 0.3, GridUnitType.Pixel);
        }

        [ObservableProperty]
        List<ThemeVariantInfo> themeVariants;

        [ObservableProperty]
        ThemeVariant selectedThemeVariant;

        public MainWindowViewModel(ThemeVariant startupThemeVariant = null)
        {
           

            ThemeVariants = new List<ThemeVariantInfo>()
            {
                new ThemeVariantInfo("Light", ThemeVariant.Light),
                new ThemeVariantInfo("Dark", ThemeVariant.Dark),
            };
            SelectedThemeVariant = startupThemeVariant == ThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light;

        }
        public static ObservableCollection<ReporterObject> ObjectList { get; set; } = new ObservableCollection<ReporterObject>();


        public static ObservableCollection<Layer> Layers { get; set; } = new ObservableCollection<Layer>();

        public static ObservableCollection<ReporterClass> ReporterClasses { get; set; } = new ObservableCollection<ReporterClass>();

       
        public event PropertyChangedEventHandler PropertyChanged;

        public static List<ConnectionData> connectionList { get; set; } = new List<ConnectionData>();

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static ObservableCollection<TablesList> ProcessingTables { get; set; } = new ObservableCollection<TablesList>();

        public static ObservableCollection<IndexPair> CurrentJoins { get; set; } = new ObservableCollection<IndexPair>();

        public static ObservableCollection<JoinStructure> JoinLines { get; set; } = new ObservableCollection<JoinStructure>();

        //public ObservableCollection<ReporterNode> League { get; set; } = new ObservableCollection<ReporterNode>();

        private ObservableCollection<object> _League;

        public ObservableCollection<object> League
        {
            get => _League;
            set
            {
                _League = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<object> GroupObjectsByClassName(IEnumerable<ReporterObject> objects)
        {
            var grouped = objects
                .GroupBy(o => o.ClassName)
                .Select(g => new ReporterNode
                {
                    ClassName = g.Key,
                    Objects = new ObservableCollection<ReporterObject>(g)
                })
                .ToList();
            //foreach (var g in grouped)
            //{
            //    foreach (var obj in g.Objects)
            //    {
            //        MessageDialog.Show("", obj.ObjectName + " " + obj.ObjectType);
            //    }
            //}

            return new ObservableCollection<object>(grouped);
        }

       

        public static ObservableCollection<string> Platforms { get; set; } = new ObservableCollection<string>();

        public static string _ConnectString = string.Empty;

        public static string ConnectString
        {
            get { return _ConnectString; }
            set { _ConnectString = value; }
        }

        public static int _ReporterLayer_id = 0;
        public static int ReporterLayer_id
        {
            get { return _ReporterLayer_id;}
            set { _ReporterLayer_id = value;}
        }

        private static bool _isAccepted = false;
        public static bool IsAccepted 
        {
            get { return _isAccepted; }
            set { _isAccepted = value; }
        }

        public static string _System_id = "1";
        public static string System_id
        {
            get { return _System_id; }
            set { _System_id = value; }
        }
        public static float _JoinWidth = 2;
        public static float JoinWidth
        {
            get { return _JoinWidth; }
            set { _JoinWidth = value; }
        }

        private static int _defaultPortMS = 1433;
        public static int defaultPortMS
        {
            get { return _defaultPortMS; }
            set { _defaultPortMS = value; }
        }

        private static int _defaultPortPG = 5432;
        public static int defaultPortPG
        {
            get { return _defaultPortPG; }
            set { _defaultPortPG = value; }
        }

        private static string _username = string.Empty;
        public static string username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
            }
        }

        private static string _password = string.Empty;
        public static string password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }

        private static DBMS _dbms;
        public static DBMS dbms
        {
            get
            {
                return _dbms;
            }
            set
            {
                _dbms = value;
            }
        }

        private static string _database = string.Empty;
        public static string database
        {
            get
            {
                return _database;
            }
            set
            {
                _database = value;
            }
        }
        private static int _port;
        public static int port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }

        private static string _server = string.Empty;
        public static string server
        {
            get
            {
                return _server;
            }
            set
            {
                _server = value;
            }
        }

        private static bool _conClose = false;
        public static bool conClose
        {
            get { return _conClose; }
            set { _conClose = value; }
        }

        public static string nameBD
        {
            get => _nameBD;
            set
            {
                if (_nameBD != value)
                {
                    _nameBD = value;
                    title = "Дизайнер семантического слоя БД - " + _nameBD; // Обновляем Title
                    StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(nameBD)));
                }
            }
        }
        private static string _nameBD = string.Empty;

        public static string GetPgConnectionString(string serverAddress, int port, string username, string password, string database)
        {
            string pgConnectionString = $"Host={serverAddress};Port={port};Username={username};Password={password};Database={database};";
            return pgConnectionString;
        }

        public static string GetMssqlConnectionString(string serverAddress, string database, string username, string password)
        {
            string mssqlConnectionString = $"Server={serverAddress};Database={database};User Id={username};Password={password};";
            return mssqlConnectionString;
        }

        private static string _title = "Дизайнер семантического слоя";
        public static string title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(title)));
                }
            }
        }

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        // Метод для вызова события
        private static void OnStaticPropertyChanged(string propertyName)
        {
           
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
