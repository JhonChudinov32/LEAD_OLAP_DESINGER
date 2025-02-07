using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Data.SqlClient;
using System;
using LEAD_OLAP_DESINGER.ViewModels;
using Avalonia.Input;
using Avalonia.Media;
using LEAD_OLAP_DESINGER.Class;
using LEAD_OLAP_DESINGER.MsgBox;
using Avalonia;
using System.Collections.ObjectModel;
using LEAD_OLAP_DESINGER.Models;
using LEAD_OLAP_DESINGER.Connection;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using System.Collections;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using static LEAD_OLAP_DESINGER.Models.DBParameters;
using Npgsql;
using System.Collections.Generic;
using System.ComponentModel;
using LEAD_OLAP_DESINGER.Helpers;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using Tmds.DBus.Protocol;


namespace LEAD_OLAP_DESINGER.Views
{
    public partial class MainWindow : Window
    {
        private Color JoinColor = Color.FromArgb(255, 192, 192, 192);
        private Point DragOffset;

        private DateTime _lastClickTime;
        private const int DoubleClickTime = 500; // Time in milliseconds

        private Int32 CurrentTableX, CurrentTableY;
        private Int32 SourceTable_id, TargetTable_id;

        private String SourceTableAlias, TargetTableAlias;
        private Canvas? OLAPPanel;
        private Border CurrentPanel, CurrentHeader;
        public Boolean IsMoveMode = false;
        public Boolean IsDragDropMode = false;

        // ���������� ��� ���������� ��������� Drag And Drop
        private ListBox SourceListBox;
        private ListBox TargetListBox;

        private int IndexOfDragField;
        private int IndexOfDropField;

        private Rect dragBoxFromMouseDownRect;

        private ToolTip? NewTableTip;
        private ToolTip? DropTableTip;
        private ToolTip? LoadStructureTip;
        private ToolTip? DropJoinTip;
        private ToolTip? JoinListTip;
        private ToolTip? EditJoinTip;
        private ToolTip? DropObjectTip;
        private ToolTip? EditObjectTip;
        private ToolTip? BackObjectTip;

        private ReporterObject selectedObject;
        public DataGrid? gridObj;
        public Panel? visiblePanel;

        private ComboBox? layerComboBox;

        private MenuItem? connectMenu;
        private MenuItem? exitMenu;
        private MenuItem? connectBD;

        private List<ConnectionData> connectionList = new List<ConnectionData>();
        private readonly ComboBox? nameBD;
        private Grid modalLoadgrid;
        private string jsonData;

        [Obsolete]
        public MainWindow()
        {
            InitializeComponent();

            OnInitialized();

            gridObj = this.FindControl<DataGrid>("gridObjects");

            NameScope.SetNameScope(OLAPPanel, new NameScope());

            visiblePanel = this.FindControl<Panel>("VisiblePanel");

            layerComboBox = this.FindControl<ComboBox>("LayerComboBox");


            connectMenu = this.FindControl<MenuItem>("ConnectMenu");
            exitMenu = this.FindControl<MenuItem>("ExitMenu");
            connectBD = this.FindControl<MenuItem>("ConnectBD");

            if (visiblePanel != null)
            {
                // ��������, ��� OLAPPanel ����������� ������ ���� ���
                if (!visiblePanel.Children.Contains(OLAPPanel))
                {
                    visiblePanel.Children.Add(OLAPPanel);
                }
            }

            DataListBox.AddHandler(InputElement.PointerPressedEvent, DataListBox_MouseDoubleClick, RoutingStrategies.Tunnel);

            // �������� ���������
            SetToolTip("button3", NewTableTip);
            SetToolTip("button4", DropTableTip);
            SetToolTip("button5", LoadStructureTip);
            SetToolTip("button2", DropJoinTip);
            SetToolTip("DataListBox", JoinListTip);
            SetToolTip("button1", EditJoinTip);
            SetToolTip("button9", DropObjectTip);
            SetToolTip("button6", EditObjectTip);
            SetToolTip("button7", BackObjectTip);

            MainWindowViewModel.StaticPropertyChanged += OnTitleChanged;

            nameBD = this.FindControl<ComboBox>("NameBD");

            string configFolderPath = Path.Combine(DirectoryHelper.GetResDirectory(), "config");
            jsonData = Path.Combine(configFolderPath, "params.json");

            UpdateComboBox();
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

                    // �������� ��� �������� �� ConnectionList
                    var connections = JObject.Parse(jsonString)["ConnectionList"];

                    MainWindowViewModel.nameBD = selectedNameBD.nameBD;
                    // ���������� �� ������� �������� � ���� ��������������� �� nameBD
                    foreach (var connection in connections)
                    {
                        string nameBD = connection["nameBD"].ToString();

                        if (nameBD == MainWindowViewModel.nameBD)
                        {
                            //MainWindowViewModel.title = "�������� �������������� ���� �� - " + MainWindowViewModel.nameBD;
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
        private void UpdateComboBox()
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

        private async void ConnectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            }
            finally
            {
                if (MainWindowViewModel.conClose == true)
                {
                    Settings.SettingUpdate();
                    LoadLayers();
                    await InitializeAsync();
                }
            }
         
            
        }

        private bool _isInitializing;

        private void OnTitleChanged(object sender, PropertyChangedEventArgs e)
        {
            // ���������� ��������� ����, ����� �������� Title ����������
            if (e.PropertyName == nameof(MainWindowViewModel.title))
            {
                DataContext = MainWindowViewModel.title;
            }
        }
        private void RaisePropertyChanged(string propertyName)
        {
            //var bindingExpression = Title.GetBindingExpression(Label.ContentProperty);
            //bindingExpression?.UpdateTarget();
        }
        private async Task InitializeAsync()
        {
            if (_isInitializing)
                return;

            _isInitializing = true;
            try
            {
                
                // ��������� ����������� ��������
                await DropDataAsync();
                await LoadTablesAsync();
                await LoadJoinsAsync();
                await LoadObjectsAsync();
            }
            catch (Exception ex)
            {
                // ��������� ������ � UI-������
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MessageDialog.Show("Initialization Error", ex.Message);
                });
            }
            finally
            {
                _isInitializing = false;
            }
        }
        private void LoadLayers()
        {
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                MainWindowViewModel.Layers = new ObservableCollection<Layer>();
                string query = @"SELECT rl.tid, rl.LayerName FROM ReporterLayers as rl WITH(NOLOCK)";
                try
                {
                    using (var dbConnection = new Connect())
                    {
                        SqlConnection connection = dbConnection.Cnn;
                        SqlCommand command = new SqlCommand(query, connection);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                MainWindowViewModel.Layers.Add(new Layer
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                });
                            }
                        }
                    }

                    // ����������� ������ � ComboBox
                    BindComboBox(layerComboBox);
                   
                }
                catch (Exception ex)
                {
                    MessageDialog.Show("������", ex.Message);
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                MainWindowViewModel.Layers = new ObservableCollection<Layer>();
                string query = @"SELECT rl.tid, rl.LayerName FROM ReporterLayers AS rl";

                try
                {
                    using (var dbConnection = new ConnectPG())
                    {
                        Npgsql.NpgsqlConnection connection = dbConnection.Cnn;

                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        using (var command = new Npgsql.NpgsqlCommand(query, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    MainWindowViewModel.Layers.Add(new Layer
                                    {
                                        Id = reader.GetInt32(0),
                                        Name = reader.GetString(1)
                                    });
                                }
                            }
                        }
                    }

                    // ����������� ������ � ComboBox
                    BindComboBox(layerComboBox);
                }
                catch (Exception ex)
                {
                    MessageDialog.Show("������", ex.Message);
                }
            }
        }
        private void BindComboBox(ComboBox comboBox)
        {
            // ���������� ���������� ����� ��������� ������, ����� �������� ��������� ������
            comboBox.SelectionChanged -= LayerComboBox_SelectionChanged;

            comboBox.ItemsSource = MainWindowViewModel.Layers;
            comboBox.DisplayMemberBinding = new Binding("Name");
            comboBox.SelectedValueBinding = new Binding("Id");

            if (MainWindowViewModel.Layers.Count > 0)
            {
                // ������������� ������ ������� �� ��������� ������ ��� ������ ��������
                if (comboBox.SelectedIndex == -1)
                {
                    comboBox.SelectedIndex = 0; // ������������� ������ �������
                }
            }
            else
            {
                comboBox.SelectedIndex = -1; // ���� ������ ���, ���������� �����
            }

            // ���������� ���������� ����� �������� ������
            comboBox.SelectionChanged += LayerComboBox_SelectionChanged;
        }

        private async void LayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // �������� ComboBox, ��������� �������
            if (sender is ComboBox comboBox && comboBox.SelectedValue is int selectedId)
            {
                MainWindowViewModel.ReporterLayer_id = selectedId;

                // �������������� ������ ���� ���
                await InitializeAsync();
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
           
            if (gridMain.ColumnDefinitions.Count > 0)
            {
                var width = gridMain.ColumnDefinitions[0].ActualWidth;
                TabControlName.Width = width;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Settings.SettingUpdate();

            // ������������� ������
            OLAPPanel = new Canvas // �������� � Panel �� Canvas, ��� ��� � ���� ������������ Canvas
            {
                Width = 30000,
                Height = 30000
            };


            // ������������� ���������

            //��� ������
            NewTableTip = new ToolTip { Content = "�������� �������" };
            DropTableTip = new ToolTip { Content = "������� �������" };
            LoadStructureTip = new ToolTip { Content = "������������� ������ �� ����" };
            DropJoinTip = new ToolTip { Content = "������� ��������� �����" };
            JoinListTip = new ToolTip { Content = "������ ������ ��� ��������� �������" };
            EditJoinTip = new ToolTip { Content = "�������� ��������� ��������� �����" };

            //��� ��������
            DropObjectTip = new ToolTip { Content = "������� ��������� ������" };
            EditObjectTip = new ToolTip { Content = "�������� ��������� �������" };
            BackObjectTip = new ToolTip { Content = "������� ������" };
        }

        private void SetToolTip(string controlName, ToolTip toolTip)
        {
            if (this.FindControl<Control>(controlName) is Control control)
            {
                ToolTip.SetTip(control, toolTip.Content);
            }
        }

        private Border GetParentPanel(Control child)
        {
            // ���������� ��������� �������� �� ��� ���, ���� �� ������ MainPanel
            var parent = child.Parent;
            while (parent != null)
            {
                if (parent is Border mainPanel && mainPanel.Tag is PanelTag)
                {
                    // ��������� ������� �������� Tag, ����� ��������������, ��� ��� MainPanel
                    return mainPanel;
                }
                parent = parent.Parent;
            }

            // ���������� null, ���� MainPanel �� ������
            return null;
        }

        [Obsolete]
        private LineCoordinates? GetLineCoors(ListBox sourceListBox, ListBox targetListBox, int indexOfDragField, int indexOfDropField)
        {
            var sourcePanel = GetParentPanel(sourceListBox);
            var targetPanel = GetParentPanel(targetListBox);

            if (sourcePanel == null || targetPanel == null) return null;

            Int32 fromLineX = 0, fromLineY = 0, toLineX = 0, toLineY = 0;

            // �������� �������������� ���������
            var sourceItemRect = sourceListBox.ItemContainerGenerator.ContainerFromIndex(indexOfDragField)?.Bounds ?? new Rect();
            var targetItemRect = targetListBox.ItemContainerGenerator.ContainerFromIndex(indexOfDropField)?.Bounds ?? new Rect();

            // �������� ���������� ������
            var sourcePanelLeft = Canvas.GetLeft(sourcePanel);
            var sourcePanelTop = Canvas.GetTop(sourcePanel);
            var targetPanelLeft = Canvas.GetLeft(targetPanel);
            var targetPanelTop = Canvas.GetTop(targetPanel);

            var sourcePanelRight = sourcePanelLeft + sourcePanel.Width; // ������ ���� ������ ���������
            var targetPanelRight = targetPanelLeft + targetPanel.Width; // ������ ���� ������ ����

            // ��������� ��������� X � Y
            if (targetPanelLeft > sourcePanelRight) // ������� ������ ������ �� ��������
            {
                fromLineX = (int)(sourceItemRect.Left + sourcePanelLeft);
                fromLineY = (int)(sourceItemRect.Top + sourcePanelTop);

                toLineX = (int)(targetItemRect.Left + targetPanelLeft);
                toLineY = (int)(targetItemRect.Top + targetPanelTop);
            }
            else if (targetPanelRight < sourcePanelLeft) // ������� ������ ����� �� ��������
            {
                fromLineX = (int)(sourceItemRect.Left + sourcePanelLeft);
                fromLineY = (int)(sourceItemRect.Top + sourcePanelTop);

                toLineX = (int)(targetItemRect.Left + targetPanelLeft + targetPanel.Width); // ��������� ������ ���� ������� ������
                toLineY = (int)(targetItemRect.Top + targetPanelTop);
            }
            else if (targetPanelLeft >= sourcePanelLeft && targetPanelLeft <= sourcePanelRight) // ������� ������ � �������� �������� ������
            {
                fromLineX = (int)(sourceItemRect.Left + sourcePanelLeft);
                fromLineY = (int)(sourceItemRect.Top + sourcePanelTop);

                toLineX = (int)(targetItemRect.Left + targetPanelLeft);
                toLineY = (int)(targetItemRect.Top + targetPanelTop);
            }

            // ��������� Y-���������, ����� ����� �� �������� �� ������� �������
            if (fromLineY < sourcePanelTop + 30) fromLineY = (int)(sourcePanelTop) + 30;
            if (toLineY < targetPanelTop + 30) toLineY = (int)(targetPanelTop) + 30;

            var sourcePanelBottom = sourcePanelTop + sourcePanel.Height;
            if (fromLineY > sourcePanelBottom - 10) fromLineY = (int)(sourcePanelBottom) - 10;

            var targetPanelBottom = targetPanelTop + targetPanel.Height;
            if (toLineY > targetPanelBottom - 10) toLineY = (int)(targetPanelBottom) - 10;

            // ���������� ���������� ��� ��������� �����
            return new LineCoordinates
            {
                FromX = fromLineX,
                FromY = fromLineY,
                ToX = toLineX,
                ToY = toLineY
            };
        }

        /// <summary>
        /// �������� ����������
        /// </summary>
        private async Task LoadJoinsAsync()
        {
            JoinStructure ThisJoin;
            TablesList ThisTable;
            LineCoordinates ThisCoors;
            ListBox SourceListBox, TargetListBox;
            LineControl MyLine;
            int SourceIndex, TargetIndex, SourceTable_id, TargetTable_id;

            try
            {
                if(MainWindowViewModel.dbms == DBMS.MS)
                {
                    using (var dbConnection = new Connect())
                    {
                        // ������ � ����������
                        SqlConnection connection = dbConnection.Cnn;

                        string stringCommand = @"SELECT 
                                       a.tid, 
                                       a.SourceTable_id, 
                                       a.TargetTable_id, 
                                       a.ReporterJoin_id, 
                                       a.ConditionStatement, 
                                       a.SourceColumn, 
                                       a.TargetColumn, 
                                       b.TableAlias AS SourceAlias, 
                                       c.TableAlias AS TargetAlias
                                   FROM ReporterTableJoins AS a WITH (NOLOCK)
                                   JOIN ReporterTables AS b WITH (NOLOCK) ON a.SourceTable_id = b.tid
                                   JOIN ReporterTables AS c WITH (NOLOCK) ON a.TargetTable_id = c.tid
                                   WHERE a.System_id = @SystemId AND a.ReporterLayer_id = @ReporterLayerId";

                        using (var command = new SqlCommand(stringCommand, connection))
                        {
                            // ���������� ����������
                            command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);
                            command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    SourceTable_id = reader.GetInt32(1);
                                    TargetTable_id = reader.GetInt32(2);

                                    // ����� ����������, ��������� � �������� � ������� ���������
                                    SourceListBox = null;
                                    TargetListBox = null;

                                    SourceIndex = -1;
                                    TargetIndex = -1;

                                    for (int i = 0; i < MainWindowViewModel.ProcessingTables.Count; i++)
                                    {
                                        ThisTable = MainWindowViewModel.ProcessingTables[i];
                                        if (ThisTable.Table_id == SourceTable_id)
                                        {
                                            SourceListBox = ThisTable.CustomList;
                                            SourceIndex = SourceListBox.Items.IndexOf(reader.GetValue(5));
                                        }
                                        if (ThisTable.Table_id == TargetTable_id)
                                        {
                                            TargetListBox = ThisTable.CustomList;
                                            TargetIndex = TargetListBox.Items.IndexOf(reader.GetValue(6));
                                        }
                                        if (SourceListBox != null && TargetListBox != null)
                                            break;
                                    }

                                    if (SourceListBox != null && TargetListBox != null)
                                    {
                                        ThisJoin = new JoinStructure();

                                        ThisCoors = GetLineCoors(SourceListBox, TargetListBox, SourceIndex, TargetIndex);
                                        MyLine = new LineControl(
                                            OLAPPanel,
                                            ThisCoors.FromX,
                                            ThisCoors.FromY,
                                            ThisCoors.ToX,
                                            ThisCoors.ToY,
                                            JoinColor,
                                            MainWindowViewModel.JoinWidth,
                                            true); // true ��� ������������ �����
                                        MyLine.ZIndex = 0; // ����� ����� ����
                                        ThisJoin.Line = MyLine;

                                        ThisJoin.Join_id = reader.GetValue(3).ToString();
                                        ThisJoin.SourceColumnName = reader.GetString(5);
                                        ThisJoin.TargetColumnName = reader.GetString(6);
                                        ThisJoin.Record_id = reader.GetInt32(0);

                                        ThisJoin.SourceIndex = SourceIndex;
                                        ThisJoin.SourceListBox = SourceListBox;
                                        ThisJoin.SourcePanel = GetParentPanel(SourceListBox);
                                        ThisJoin.SourceTableName = reader.GetString(7);
                                        ThisJoin.TargetIndex = TargetIndex;
                                        ThisJoin.TargetListBox = TargetListBox;
                                        ThisJoin.TargetPanel = GetParentPanel(TargetListBox);
                                        ThisJoin.TargetTableName = reader.GetString(8);
                                        ThisJoin.ConditionStatement = reader.GetString(4);

                                        MainWindowViewModel.JoinLines.Add(ThisJoin);
                                    }
                                }
                                await reader.CloseAsync();
                            }
                        }
                    }
                }
                else if (MainWindowViewModel.dbms == DBMS.PG)
                {
                    using (var dbConnection = new ConnectPG())
                    {
                        // ������ � ����������
                        NpgsqlConnection connection = dbConnection.Cnn;

                        string stringCommand = @"SELECT 
                     a.tid, 
                     a.SourceTable_id, 
                     a.TargetTable_id, 
                     a.ReporterJoin_id, 
                     a.ConditionStatement, 
                     a.SourceColumn, 
                     a.TargetColumn, 
                     b.TableAlias AS SourceAlias, 
                     c.TableAlias AS TargetAlias
                 FROM ReporterTableJoins AS a
                 JOIN ReporterTables AS b ON a.SourceTable_id = b.tid
                 JOIN ReporterTables AS c ON a.TargetTable_id = c.tid
                 WHERE a.system_id = @system_id AND a.reporterLayer_id = @reporterLayer_id";

                        using (var command = new NpgsqlCommand(stringCommand, connection))
                        {
                            // ���������� ����������
                           
                            command.Parameters.AddWithValue("@system_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                            command.Parameters.AddWithValue("@reporterlayer_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    SourceTable_id = reader.GetInt32(1);
                                    TargetTable_id = reader.GetInt32(2);

                                    // ����� ����������, ��������� � �������� � ������� ���������
                                    SourceListBox = null;
                                    TargetListBox = null;

                                    SourceIndex = -1;
                                    TargetIndex = -1;

                                    for (int i = 0; i < MainWindowViewModel.ProcessingTables.Count; i++)
                                    {
                                        ThisTable = MainWindowViewModel.ProcessingTables[i];
                                        if (ThisTable.Table_id == SourceTable_id)
                                        {
                                            SourceListBox = ThisTable.CustomList;
                                            SourceIndex = SourceListBox.Items.IndexOf(reader.GetValue(5));
                                        }
                                        if (ThisTable.Table_id == TargetTable_id)
                                        {
                                            TargetListBox = ThisTable.CustomList;
                                            TargetIndex = TargetListBox.Items.IndexOf(reader.GetValue(6));
                                        }
                                        if (SourceListBox != null && TargetListBox != null)
                                            break;
                                    }

                                    if (SourceListBox != null && TargetListBox != null)
                                    {
                                        ThisJoin = new JoinStructure();

                                        ThisCoors = GetLineCoors(SourceListBox, TargetListBox, SourceIndex, TargetIndex);
                                        MyLine = new LineControl(
                                            OLAPPanel,
                                            ThisCoors.FromX,
                                            ThisCoors.FromY,
                                            ThisCoors.ToX,
                                            ThisCoors.ToY,
                                            JoinColor,
                                            MainWindowViewModel.JoinWidth,
                                            true); // true ��� ������������ �����
                                        MyLine.ZIndex = 0; // ����� ����� ����
                                        ThisJoin.Line = MyLine;

                                        ThisJoin.Join_id = reader.GetValue(3).ToString();
                                        ThisJoin.SourceColumnName = reader.GetString(5);
                                        ThisJoin.TargetColumnName = reader.GetString(6);
                                        ThisJoin.Record_id = reader.GetInt32(0);

                                        ThisJoin.SourceIndex = SourceIndex;
                                        ThisJoin.SourceListBox = SourceListBox;
                                        ThisJoin.SourcePanel = GetParentPanel(SourceListBox);
                                        ThisJoin.SourceTableName = reader.GetString(7);
                                        ThisJoin.TargetIndex = TargetIndex;
                                        ThisJoin.TargetListBox = TargetListBox;
                                        ThisJoin.TargetPanel = GetParentPanel(TargetListBox);
                                        ThisJoin.TargetTableName = reader.GetString(8);
                                        ThisJoin.ConditionStatement = reader.GetString(4);

                                        MainWindowViewModel.JoinLines.Add(ThisJoin);
                                    }
                                }
                                await reader.CloseAsync();
                            }
                        }
                    }
                }


            }
            catch (SqlException sqlEx)
            {
                MessageDialog.Show("Database Error", $"������ ��� ���������� SQL-�������: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                MessageDialog.Show("General Error", $"������: {ex.Message}");
            }
        }

        /// <summary>
        /// �������� ��������
        /// </summary>
        private async Task LoadObjectsAsync()
        {
            try
            {
                if (MainWindowViewModel.dbms == DBMS.MS)
                {
                    using (var dbConnection = new Connect())
                    {
                        SqlConnection connection = dbConnection.Cnn;

                        // �������� �������
                        string classQuery = @"SELECT tid, ClassName 
                                  FROM ReporterClasses WITH (NOLOCK) 
                                  WHERE System_id = @SystemId AND ReporterLayer_id = @LayerId";

                        using (var classCommand = new SqlCommand(classQuery, connection))
                        {
                            classCommand.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);
                            classCommand.Parameters.AddWithValue("@LayerId", MainWindowViewModel.ReporterLayer_id);

                            using (var classReader = await classCommand.ExecuteReaderAsync())
                            {
                                while (await classReader.ReadAsync())
                                {
                                    var reporterClass = new ReporterClass
                                    {
                                        Class_id = classReader.GetInt32(0),
                                        ClassName = classReader.GetString(1)
                                    };
                                    MainWindowViewModel.ReporterClasses.Add(reporterClass);
                                }
                            }
                        }

                        // �������� ��������
                        string objectQuery = @"SELECT a.tid, a.ObjectName, 
                                   a.ReporterDimension_id, a.ReporterMeasure_id, 
                                   a.ReporterDetail_id, a.ReporterClass_id, b.ClassName,
                                   ISNULL(a.IsNumeric, 0), a.ObjectDescription
                                   FROM ReporterObjects AS a WITH (NOLOCK) 
                                   JOIN ReporterClasses AS b WITH (NOLOCK) 
                                   ON a.ReporterClass_id = b.tid 
                                   WHERE a.System_id = @SystemId 
                                   AND a.ReporterLayer_id = @LayerId";

                        using (var objectCommand = new SqlCommand(objectQuery, connection))
                        {
                            objectCommand.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);
                            objectCommand.Parameters.AddWithValue("@LayerId", MainWindowViewModel.ReporterLayer_id);

                            using (var objectReader = await objectCommand.ExecuteReaderAsync())
                            {
                              
                                while (await objectReader.ReadAsync())
                                {
                                    var reporterObject = new ReporterObject
                                    {
                                        Object_id = objectReader.GetInt32(0),
                                        ObjectName = objectReader.GetString(1),
                                        ReporterDimension_id = objectReader.IsDBNull(2) ? -1 : objectReader.GetInt32(2),
                                        ReporterMeasure_id = objectReader.IsDBNull(3) ? -1 : objectReader.GetInt32(3),
                                        ReporterDetail_id = objectReader.IsDBNull(4) ? -1 : objectReader.GetInt32(4),
                                        ReporterClass_id = objectReader.GetInt32(5),
                                        ClassName = objectReader.GetString(6),
                                        IsNumeric = objectReader.GetInt32(7) == 1,
                                        ObjectDescription = objectReader.GetString(8),
                                    };

                                    reporterObject.ObjectType = reporterObject.ReporterDimension_id > -1 ? "���������" :
                                                                reporterObject.ReporterMeasure_id > -1 ? "����" :
                                                                "������";

                                    // ���������� ������� � ���������
                                  
                                   MainWindowViewModel.ObjectList.Add(reporterObject);
                                }
                                MainWindowViewModel m = new MainWindowViewModel();
                               
                                m.League = m.GroupObjectsByClassName(MainWindowViewModel.ObjectList);

                                var header = new HeaderNode();

                                // ��������� ��������� � ������

                                m.League.Insert(0, header);

                                treeObj.ItemsSource = m.League;
                            }

                        }

                        if (MainWindowViewModel.ObjectList.Count > 0)
                        {
                          
                            gridObj.ItemsSource = MainWindowViewModel.ObjectList;
                           
                        }
                    }
                }
                else if (MainWindowViewModel.dbms == DBMS.PG)
                {
                    using (var dbConnection = new ConnectPG())
                    {
                        // ������ � ����������
                        NpgsqlConnection connection = dbConnection.Cnn;

                        // �������� �������
                        string classQuery = @"SELECT tid, ClassName 
                          FROM ReporterClasses
                          WHERE system_id = @system_id AND reporterLayer_id = @LayerId";

                        using (var classCommand = new NpgsqlCommand(classQuery, connection))
                        {
                            classCommand.Parameters.AddWithValue("@system_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                            classCommand.Parameters.AddWithValue("@LayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));
                           

                            using (var classReader = await classCommand.ExecuteReaderAsync())
                            {
                                while (await classReader.ReadAsync())
                                {
                                    var reporterClass = new ReporterClass
                                    {
                                        Class_id = classReader.GetInt32(0),
                                        ClassName = classReader.GetString(1)
                                    };
                                    MainWindowViewModel.ReporterClasses.Add(reporterClass);
                                }
                            }
                        }

                        // �������� ��������
                        string objectQuery = @"SELECT a.tid, a.ObjectName, 
                          a.ReporterDimension_id, a.ReporterMeasure_id, 
                          a.ReporterDetail_id, a.ReporterClass_id, b.ClassName,
                          COALESCE(a.IsNumeric, 0), a.ObjectDescription 
                          FROM ReporterObjects AS a 
                          JOIN ReporterClasses AS b ON a.ReporterClass_id = b.tid
                          WHERE a.system_id = @system_id 
                          AND a.reporterLayer_id = @LayerId";

                        using (var objectCommand = new NpgsqlCommand(objectQuery, connection))
                        {

                            objectCommand.Parameters.AddWithValue("@system_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                            objectCommand.Parameters.AddWithValue("@LayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));
                          
                            using (var objectReader = await objectCommand.ExecuteReaderAsync())
                            {
                                while (await objectReader.ReadAsync())
                                {
                                    var reporterObject = new ReporterObject
                                    {
                                        Object_id = objectReader.GetInt32(0),
                                        ObjectName = objectReader.GetString(1),
                                        ReporterDimension_id = objectReader.IsDBNull(2) ? -1 : objectReader.GetInt32(2),
                                        ReporterMeasure_id = objectReader.IsDBNull(3) ? -1 : objectReader.GetInt32(3),
                                        ReporterDetail_id = objectReader.IsDBNull(4) ? -1 : objectReader.GetInt32(4),
                                        ReporterClass_id = objectReader.GetInt32(5),
                                        ClassName = objectReader.GetString(6),
                                        IsNumeric = objectReader.GetInt32(7) == 1,
                                        ObjectDescription = objectReader.GetString(8),
                                    };

                                    reporterObject.ObjectType = reporterObject.ReporterDimension_id > -1 ? "���������" :
                                                                reporterObject.ReporterMeasure_id > -1 ? "����" :
                                                                "������";

                                    MainWindowViewModel.ObjectList.Add(reporterObject);
                                }
                                MainWindowViewModel m = new MainWindowViewModel();
                               
                              
                                m.League = m.GroupObjectsByClassName(MainWindowViewModel.ObjectList);

                                var header = new HeaderNode();

                                m.League.Insert(0, header);

                                treeObj.ItemsSource = m.League;
                            }
                        }

                        if (MainWindowViewModel.ObjectList.Count > 0)
                        {
                            gridObj.ItemsSource = MainWindowViewModel.ObjectList;
                        }
                    }
                }
                    
            }
            catch (SqlException sqlEx)
            {
                MessageDialog.Show("Database Error", $"������ ��� ���������� SQL-�������: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                MessageDialog.Show("General Error", $"������: {ex.Message}");
            }
        }


        private void HeaderGrid_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
               vm.UpdateColumnWidths(e.NewSize.Width);
            }
        }

        /// <summary>
        /// �������� ������
        /// </summary>
        private async Task LoadTablesAsync()
        {
            try
            {
                if (MainWindowViewModel.dbms == DBMS.MS)
                {
                    string query = @"SELECT tid, TableName, TableAlias, X, Y 
                         FROM ReporterTables WITH (NOLOCK)
                         WHERE System_id = @system_id AND ReporterLayer_id = @reporter_layer_id";

                    using (var dbConnect = new Connect())
                    using (var command = new SqlCommand(query, dbConnect.Cnn))
                    {
                        command.Parameters.AddWithValue("@system_id", MainWindowViewModel.System_id);
                        command.Parameters.AddWithValue("@reporter_layer_id", MainWindowViewModel.ReporterLayer_id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var table = new TablesList
                                {
                                    Table_id = reader.GetInt32(0),
                                    TableName = reader.GetString(1),
                                    TableAlias = reader.GetString(2),
                                    X = reader.IsDBNull(3) ? 10 : reader.GetInt32(3),
                                    Y = reader.IsDBNull(4) ? 10 : reader.GetInt32(4)
                                };

                                MainWindowViewModel.ProcessingTables.Add(table);
                            }
                        }
                    }
                }
                else if (MainWindowViewModel.dbms == DBMS.PG)
                {
                    string query = @"SELECT tid, TableName, TableAlias, X, Y 
                         FROM ReporterTables 
                         WHERE System_id = @system_id AND ReporterLayer_id = @reporter_layer_id";

                    using (var dbConnect = new ConnectPG())  // ���������� ��� Connect ����� � Npgsql
                    using (var command = new NpgsqlCommand(query, dbConnect.Cnn))  // ���������� NpgsqlCommand
                    {
                        command.Parameters.AddWithValue("@system_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                        command.Parameters.AddWithValue("@reporter_layer_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                        using (var reader = await command.ExecuteReaderAsync())  // ����������� ���������� �������
                        {
                            while (await reader.ReadAsync())
                            {
                                var table = new TablesList
                                {
                                    Table_id = reader.GetInt32(0),
                                    TableName = reader.GetString(1),
                                    TableAlias = reader.GetString(2),
                                    X = reader.IsDBNull(3) ? 10 : reader.GetInt32(3),
                                    Y = reader.IsDBNull(4) ? 10 : reader.GetInt32(4)
                                };

                                MainWindowViewModel.ProcessingTables.Add(table);
                            }
                        }
                    }
                }

                // �������� ������� ��� ������ �������
                foreach (var table in MainWindowViewModel.ProcessingTables)
                {
                    var customPanel = CreateCustomPanel(
                        table.TableAlias,
                        OLAPPanel,
                        table.TableName,
                        table.X,
                        table.Y,
                        table.Table_id);

                    table.CustomPanel = customPanel.ThisPanel;
                    table.CustomList = customPanel.ThisListBox;
                    customPanel.ThisPanel.ZIndex = 1; // ������ ����� ����
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��� �������� ������: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// �������� ������ ������ ��������
        /// </summary>
        public CustomPanelReturn CreateCustomPanel_Old(string panelName, object parentObject, string tableName, int x, int y, int tableId)
        {
            // �������� ������ ��� �����
            // �������� ������� ������
            var secondaryColor = Color.Parse("#7a9fff");
            var backgroundColor = Color.Parse("#f0f4f8");
            var borderColor = Color.Parse("#D1D1D1");
            var textColor = Color.Parse("#120033");
            var itemTextColor = Color.Parse("#5C5C5C");


            // ����� ������ "�������"
            var thisPanel = new Border
            {
                Background = Brushes.Transparent, // Transparent to reflect the new color scheme
                BorderBrush = new SolidColorBrush(secondaryColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Width = 150,
                Height = 204
            };

            if (x == -1) x = 100;
            if (y == -1) y = 100;

            Canvas.SetLeft(thisPanel, x);
            Canvas.SetTop(thisPanel, y);

            var thisTag = new PanelTag
            {
                Table_id = tableId,
                TableName = tableName,
                TableAlias = panelName
            };
            thisPanel.Tag = thisTag;

            // ������ ��������� �������
            var titlePanel = new Border
            {
                Background = new SolidColorBrush(secondaryColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                Height = 30,
                Tag = "PANEL_TITLE"
            };
            titlePanel.PointerPressed += ThisMouseDown;
            titlePanel.PointerReleased += ThisMouseUp;
            titlePanel.PointerMoved += ThisMouseMove;

            // ������ ���������� �������
            var contentPanel = new Border
            {
                Background = new SolidColorBrush(backgroundColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Height = 170,
                Tag = "content"
            };

            // ��������� ������
            var newLabel = new TextBlock
            {
                Text = panelName + "\n" + "(" + tableName + ")",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeight.ExtraBlack,
                FontSize = 11,
                Foreground = new SolidColorBrush(textColor) // Apply text color
            };

            titlePanel.Child = newLabel;

            // �������� �����
            var newBox = new ListBox
            {
                BorderBrush = null,
                Background = new SolidColorBrush(backgroundColor),
            };
            newBox.Padding = new Thickness(0); // ������� ������� � ������ ListBox

            newBox.ItemTemplate = new FuncDataTemplate<object>((item, provider) =>
            {
                var listBoxItem = new ListBoxItem
                {
                    Content = item,
                    Padding = new Thickness(0, 0, 0, 0),
                    Tag = "DRAGDROP_ELEMENT",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(itemTextColor), // Apply item text color
                };

                // ���������� ���������� ������� PointerPressed ��� ListBoxItem
                listBoxItem.PointerPressed += ThisMouseDown;
                listBoxItem.PointerReleased += ThisMouseUp;
                listBoxItem.PointerMoved += ThisMouseMove;
                listBoxItem.AddHandler(DragDrop.DragOverEvent, ThisDragOver, RoutingStrategies.Bubble);
                listBoxItem.AddHandler(DragDrop.DropEvent, ThisDragDrop, RoutingStrategies.Bubble);
                listBoxItem.AddHandler(DragDrop.DragLeaveEvent, ThisDragLeave, RoutingStrategies.Bubble);

                return listBoxItem;
            });

            // ����������� ����� ��� ��������� ������ (ListBoxItem)
            var itemStyle = new Style(x => x.OfType<ListBoxItem>())
            {
                Setters =
                {
                    new Setter(ListBoxItem.PaddingProperty, new Thickness(0, 0, 0, 0)) // ���������� ������� ��������
                }
            };

            // ��������� ����� � ListBox
            newBox.Styles.Add(itemStyle);
            DragDrop.SetAllowDrop(newBox, true);

            // ���������� ListBox �� ���� ������
            FillListBoxWithColumns(newBox, tableName);

            contentPanel.Child = newBox;

            var mainStack = new StackPanel();
            mainStack.Children.Add(titlePanel);
            mainStack.Children.Add(contentPanel);
            thisPanel.Child = mainStack; // ��������� StackPanel � �������� ������

            // ����������� ������� ���� ��� ���� ������
            thisPanel.PointerMoved += ThisMouseMove;

            // ��������� ������ � ��������
            if (parentObject is ILogical parent)
            {
                (parent as Panel)?.Children.Add(thisPanel);
            }

            return new CustomPanelReturn
            {
                ThisPanel = thisPanel,
                ThisListBox = newBox
            };
        }

        public CustomPanelReturn CreateCustomPanel(string panelName, object parentObject, string tableName, int x, int y, int tableId)
        {
            // �������� ������� ������
            var secondaryColor = Color.Parse("#7a9fff");
            var backgroundColor = Color.Parse("#f0f4f8");
            var borderColor = Color.Parse("#D1D1D1");
            var textColor = Color.Parse("#120033");
            var itemTextColor = Color.Parse("#5C5C5C");

            // ����� ������ "�������"
            var thisPanel = new Border
            {
                Background = Brushes.Transparent, // Transparent to reflect the new color scheme
                BorderBrush = new SolidColorBrush(secondaryColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Width = 150,
                Height = 204
            };

            if (x == -1) x = 100;
            if (y == -1) y = 100;

            Canvas.SetLeft(thisPanel, x);
            Canvas.SetTop(thisPanel, y);

            var thisTag = new PanelTag
            {
                Table_id = tableId,
                TableName = tableName,
                TableAlias = panelName
            };
            thisPanel.Tag = thisTag;

            // ������� Grid ��� ���������� ������
            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // ���������� ��� ������ ��� ������ (���� ��� ���������, ������ ��� ��������)
            var titleRow = new RowDefinition { Height = new GridLength(30) };  // ���������
            var contentRow = new RowDefinition { Height = new GridLength(170, GridUnitType.Pixel) };  // �������
            grid.RowDefinitions.Add(titleRow);
            grid.RowDefinitions.Add(contentRow);

            // ������ ���������
            var titlePanel = new Border
            {
                Background = new SolidColorBrush(secondaryColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                Height = 30,
                Tag = "PANEL_TITLE"
            };
            titlePanel.PointerPressed += ThisMouseDown;
            titlePanel.PointerReleased += ThisMouseUp;
            titlePanel.PointerMoved += ThisMouseMove;

            // ��������� ������
            var newLabel = new TextBlock
            {
                Text = panelName + "\n" + "(" + tableName + ")",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeight.ExtraBlack,
                FontSize = 11,
                Foreground = new SolidColorBrush(textColor) // Apply text color
            };

            titlePanel.Child = newLabel;

            // ������ ���������� �������
            var contentPanel = new Border
            {
                Background = new SolidColorBrush(backgroundColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Height = 170,
                Tag = "content"
            };

            // ������ ��� ����������� ������
            var newBox = new ListBox
            {
                BorderBrush = null,
                Background = new SolidColorBrush(backgroundColor),
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Stretch,  // ������������ ���������� �� ���������
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            newBox.ItemTemplate = new FuncDataTemplate<object>((item, provider) =>
            {
                var listBoxItem = new ListBoxItem
                {
                    Content = item,
                    Padding = new Thickness(0),
                    Tag = "DRAGDROP_ELEMENT",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(itemTextColor),
                };

                listBoxItem.PointerPressed += ThisMouseDown;
                listBoxItem.PointerReleased += ThisMouseUp;
                listBoxItem.PointerMoved += ThisMouseMove;
                listBoxItem.AddHandler(DragDrop.DragOverEvent, ThisDragOver, RoutingStrategies.Bubble);
                listBoxItem.AddHandler(DragDrop.DropEvent, ThisDragDrop, RoutingStrategies.Bubble);
                listBoxItem.AddHandler(DragDrop.DragLeaveEvent, ThisDragLeave, RoutingStrategies.Bubble);

                return listBoxItem;
            });

            // ��������� ����� � ListBoxItem
            var itemStyle = new Style(x => x.OfType<ListBoxItem>())
            {
                Setters =
                {
                    new Setter(ListBoxItem.PaddingProperty, new Thickness(0))
                }
            };

            newBox.Styles.Add(itemStyle);
            DragDrop.SetAllowDrop(newBox, true);

            FillListBoxWithColumns(newBox, tableName);

            contentPanel.Child = newBox;

            // ��������� ������ � Grid
            grid.Children.Add(titlePanel);
            Grid.SetRow(titlePanel, 0);  // ���������
            grid.Children.Add(contentPanel);
            Grid.SetRow(contentPanel, 1);  // �������

            // ��������� ������� ��� ��������� ������� � ������ ������ ����
            var resizeHandle = new Border
            {
                Background = new SolidColorBrush(borderColor),
                Width = 5,
                Height = 5,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = new Cursor(StandardCursorType.SizeAll)
            };

            grid.Children.Add(resizeHandle);
            Grid.SetRow(resizeHandle, 1); // ��������� ��� � ������ ����� ��������

            // �������������� ��� ��������� �������� ������
            bool isResizing = false;
            double lastMouseX = 0, lastMouseY = 0;

            resizeHandle.PointerPressed += (sender, e) =>
            {
                isResizing = true;
                lastMouseX = e.GetPosition(thisPanel).X;
                lastMouseY = e.GetPosition(thisPanel).Y;
            };

            resizeHandle.PointerMoved += (sender, e) =>
            {
                if (isResizing)
                {
                    var deltaX = e.GetPosition(thisPanel).X - lastMouseX;
                    var deltaY = e.GetPosition(thisPanel).Y - lastMouseY;

                    // �������� ������� ������
                    var newWidth = Math.Max(150, thisPanel.Width + deltaX);  // ������
                    var newHeight = Math.Max(50, thisPanel.Height + deltaY); // ������

                    thisPanel.Width = newWidth;
                    thisPanel.Height = newHeight;

                    // ��������� ������� �������� � ��������� � Grid
                    contentRow.Height = new GridLength(newHeight - 30);  // ��������� ������ ��������
                    titleRow.Height = new GridLength(30);  // ��������� �������� �������������

                    // ��������� ������ ListBox
                    newBox.Height = newHeight - 30;  // ��� ������ ������ ��� ������������ ListBox

                    lastMouseX = e.GetPosition(thisPanel).X;
                    lastMouseY = e.GetPosition(thisPanel).Y;
                }
            };

            resizeHandle.PointerReleased += (sender, e) =>
            {
                isResizing = false;
            };

            // ������������� Grid ��� ���������� �������� ������
            thisPanel.Child = grid;
            thisPanel.PointerMoved += ThisMouseMove;

            // ��������� ������ � ������������ �������
            if (parentObject is ILogical parent)
            {
                (parent as Panel)?.Children.Add(thisPanel);
            }

            return new CustomPanelReturn
            {
                ThisPanel = thisPanel,
                ThisListBox = newBox
            };
        }

        private bool _isResizing = false;
        private double _lastMouseY;

        private void GridSplitter_DragDelta(object? sender, VectorEventArgs e)
        {
            if (sender is GridSplitter splitter)
            {
                var parentGrid = splitter.Parent as Grid;
                if (parentGrid == null) return;

                int columnIndex = Grid.GetColumn(splitter);

                if (columnIndex == 1) // ������ ��������
                {
                    if (MainWindowViewModel.Column1Width.GridUnitType == GridUnitType.Star)
                    {
                        MainWindowViewModel.Column1Width = new GridLength(MainWindowViewModel.Column1Width.Value * parentGrid.Bounds.Width, GridUnitType.Pixel);
                    }

                    MainWindowViewModel.Column1Width = new GridLength(Math.Max(0, MainWindowViewModel.Column1Width.Value + e.Vector.X), GridUnitType.Pixel);
                }
                else if (columnIndex == 3) // ������ ��������
                {
                    if (MainWindowViewModel.Column2Width.GridUnitType == GridUnitType.Star)
                    {
                        MainWindowViewModel.Column2Width = new GridLength(MainWindowViewModel.Column2Width.Value * parentGrid.Bounds.Width, GridUnitType.Pixel);
                    }

                    MainWindowViewModel.Column2Width = new GridLength(Math.Max(0, MainWindowViewModel.Column2Width.Value + e.Vector.X), GridUnitType.Pixel);
                }
            }
        }


        /// <summary>
        /// ������� �������� ������� �����
        /// </summary>
        private async void DataListBox_MouseDoubleClick(object sender, PointerPressedEventArgs e)
        {
            // Get the current click time
            var currentClickTime = DateTime.Now;

            // Check if the interval between two clicks is within the double-click threshold
            if ((currentClickTime - _lastClickTime).TotalMilliseconds <= DoubleClickTime)
            {
                // Handle double-click logic
                await UpdateJoinAsync();
            }

            // Update the last click time
            _lastClickTime = currentClickTime;
        }

        /// <summary>
        /// ������� ������� ������� ���� �� ������ OLAP 
        /// </summary>
        private void ThisMouseDown(object sender, PointerPressedEventArgs e)
        {
            
            string elementName = (sender as Control)?.Tag?.ToString();
            DragOffset = e.GetPosition(sender as Visual);

            //+
            if (elementName == "PANEL_TITLE")
            {
                IsMoveMode = true;
                var headerPanel = sender as Border;
                var thisPanel = GetParentPanel(headerPanel);


                if (thisPanel != null)
                {
                    CurrentTableX = (int)Canvas.GetLeft(thisPanel);
                    CurrentTableY = (int)Canvas.GetTop(thisPanel);
                    headerPanel.Background = new SolidColorBrush(Color.Parse("#3A76D1"));

                    PanelTag ThisTag = (PanelTag)thisPanel.Tag;

                    String Table_id = ThisTag.Table_id.ToString();
                    String CurrentTable_id;

                    if (CurrentPanel == null)
                    {
                        CurrentTable_id = "";
                    }
                    else
                    {
                        PanelTag CurrentTag = (PanelTag)CurrentPanel.Tag;
                        CurrentTable_id = CurrentTag.Table_id.ToString();
                    }

                    if (CurrentPanel == null)
                    {
                        CurrentPanel = thisPanel;
                        CurrentHeader = headerPanel;
                        UpdateCurrentPanel(thisPanel);
                    }
                    else if (Table_id != CurrentTable_id)
                    {
                        //CurrentHeader.Background = new SolidColorBrush(Color.Parse("#F4A460"));
                        CurrentHeader.Background = new SolidColorBrush(Color.Parse("#7a9fff"));
                        CurrentHeader = headerPanel;
                        CurrentPanel = thisPanel;
                        UpdateCurrentPanel(thisPanel);
                    }
                }

            }
            //+
            else if (elementName == "DRAGDROP_ELEMENT" && sender is ListBoxItem listBoxItem)
            {
                IsDragDropMode = true;
                SourceListBox = listBoxItem.FindLogicalAncestorOfType<ListBox>();

                // �������� ������ �������� ��� ���������� ����
                var position = e.GetPosition(SourceListBox);
                IndexOfDragField = GetIndexFromPoint(SourceListBox, position);

                if (IndexOfDragField >= 0)
                {
                    // ������ ������� ��� ������ ��������������
                    var dragSize = new Size(10, 10); // ������, ����������� SystemInformation.DragSize � WinForms
                    dragBoxFromMouseDownRect = new Rect(
                        position.X - dragSize.Width / 2,
                        position.Y - dragSize.Height / 2,
                        dragSize.Width,
                        dragSize.Height
                    );
                }
                else
                {
                    dragBoxFromMouseDownRect = new Rect(0, 0, 0, 0);
                }

                // ��������� ���������� � ������� ������, ���� �������������� ���������� ������ ������
                if (SourceListBox.Parent is Border contentPanel &&
                    contentPanel.Parent is Grid grid &&
                    grid.Parent is Border thisPanel &&
                    thisPanel.Tag is PanelTag thisTag)
                // �������� ������ ������
                //if (SourceListBox.Parent is Border contentPanel &&
                //        contentPanel.Parent is StackPanel mainStack &&
                //        mainStack.Parent is Border thisPanel &&
                //        thisPanel.Tag is PanelTag thisTag)
                {
                    SourceTable_id = thisTag.Table_id;
                    SourceTableAlias = thisTag.TableAlias;
                }
            }
        }

        /// <summary>
        /// ������� ��� ���������� ������� ���� �� ������ OLAP 
        /// </summary>
        private void ThisMouseUp(object sender, PointerReleasedEventArgs e)
        {
            string elementName = (sender as Control)?.Tag?.ToString();

            if (elementName == "PANEL_TITLE")
            {
                IsMoveMode = false;

                if (CurrentPanel != null)
                {
                    var tags = CurrentPanel.Tag as PanelTag;
                    var x = Canvas.GetLeft(CurrentPanel);  // ��� Avalonia, ���������� Canvas.GetLeft ��� ��������� ���������
                    var y = Canvas.GetTop(CurrentPanel);   // ��� Avalonia, ���������� Canvas.GetTop ��� ��������� ���������

                    if (x.ToString() != CurrentTableX.ToString() || y.ToString() != CurrentTableY.ToString())
                    {
                        if (MainWindowViewModel.dbms == DBMS.MS)
                        {
                            using (var dbConnection = new Connect())
                            {
                                // ������ � ����������
                                SqlConnection connection = dbConnection.Cnn;
                                // ���������� ��������� � ���� ������
                                using (var command = new SqlCommand("UPDATE ReporterTables SET X = @x, Y = @y WHERE System_id = @SystemId AND tid = @TableId AND ReporterLayer_id = @LayerId", connection))
                                {
                                    command.Parameters.AddWithValue("@x", x);
                                    command.Parameters.AddWithValue("@y", y);
                                    command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);
                                    command.Parameters.AddWithValue("@TableId", tags?.Table_id);
                                    command.Parameters.AddWithValue("@LayerId", MainWindowViewModel.ReporterLayer_id);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                        else if (MainWindowViewModel.dbms == DBMS.PG)
                        {
                            using (var dbConnection = new ConnectPG()) // ����������� ����� ��� ����� ConnectPG
                            {
                                // ������ � ����������
                                var connection = dbConnection.Cnn;

                               
                                // ���������� ��������� � ���� ������
                                const string query = @"UPDATE ReporterTables SET X = @x, Y = @y WHERE System_id = @SystemId AND tid = @TableId AND ReporterLayer_id = @LayerId";

                                using (var command = new NpgsqlCommand(query, connection)) // ���������� NpgsqlCommand ��� PostgreSQL
                                {
                                    // ���������� ����������
                                    command.Parameters.AddWithValue("@x", NpgsqlTypes.NpgsqlDbType.Double, Convert.ToDouble(x));
                                    command.Parameters.AddWithValue("@y", NpgsqlTypes.NpgsqlDbType.Double, Convert.ToDouble(y));
                                    command.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                    command.Parameters.AddWithValue("@TableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(tags?.Table_id));
                                    command.Parameters.AddWithValue("@LayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                                    // ����������� ���������� �������
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                       
                    }
                }
            }
            else if (elementName == "DRAGDROP_ELEMENT")
            {
                IsDragDropMode = false;
                dragBoxFromMouseDownRect = new Rect(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// ������� ��������� ������� ���� ���� �� ������ OLAP 
        /// </summary>

        private async void ThisMouseMove(object sender, PointerEventArgs e)
        {
            string elementName = (sender as Control)?.Tag?.ToString();

            if (IsMoveMode && sender is Control control && control.Parent is Canvas parentCanvas)
            {
                var pointerPosition = e.GetPosition(parentCanvas);
                Canvas.SetLeft(control, pointerPosition.X - DragOffset.X);
                Canvas.SetTop(control, pointerPosition.Y - DragOffset.Y);

                // ����������� Canvas ��� ���������� "��������"
                parentCanvas.InvalidateVisual();
            }
            //+
            if (elementName == "PANEL_TITLE" && IsMoveMode)
            {
                var thisPanel = sender as Control;

                if (thisPanel != null)
                {
                    var parent = GetParentPanel(thisPanel);
                    var pointerPosition = e.GetPosition(parent);

                    if (pointerPosition.X > 0 && pointerPosition.Y > 0)
                    {
                        thisPanel.Margin = new Thickness(pointerPosition.X - DragOffset.X, pointerPosition.Y - DragOffset.Y, 0, 0);
                    }

                    // ���������� ������
                    foreach (var join in MainWindowViewModel.JoinLines)
                    {
                        if (join.SourcePanel.ToString() == thisPanel.ToString() || join.TargetPanel.ToString() == thisPanel.ToString())
                        {
                            DrawLine(join.SourceListBox, join.TargetListBox, join.SourceIndex, join.TargetIndex, join.Line);
                        }
                    }
                }

            }
            //+
            else if (elementName == "DRAGDROP_ELEMENT" && IsDragDropMode)
            {
                var pointerProperties = e.GetCurrentPoint(null).Properties;

                // ���������, ������ �� ����� ������ ����
                if (pointerProperties.IsLeftButtonPressed)
                {
                    // ���������, ��� dragBoxFromMouseDownRect �� ������ � ������� ������� ��� ���
                    if (dragBoxFromMouseDownRect != default)
                    {
                        // ������� ������ ������ ��� ��������������
                        var dataObject = new DataObject();
                        if (SourceListBox?.SelectedItem != null)
                        {
                            // �������� ������ ���������� ��������
                            dataObject.Set("application/x-my-data", SourceListBox.SelectedItem);

                            // ������ ��������������
                            var dropEffect = await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move | DragDropEffects.Link);

                            // ��������� ���������� ��������������
                            if (dropEffect == DragDropEffects.Move && SourceListBox != null && IndexOfDragField >= 0)
                            {
                                // �������� �������� �� ��������� ������
                                if (SourceListBox.Items is IList items && IndexOfDragField < items.Count)
                                {
                                    items.RemoveAt(IndexOfDragField);
                                }

                                // ������������� ��������� �� ���������� ������� ��� ������ �������, ���� ������ �� ����
                                if (IndexOfDragField > 0 && IndexOfDragField <= SourceListBox.ItemCount)
                                    SourceListBox.SelectedIndex = IndexOfDragField - 1;
                                else if (SourceListBox.ItemCount > 0)
                                    SourceListBox.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
 
        }

        private void OnToggleThemeClicked(object? sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
               // app.ToggleTheme();
            }
        }
     

        /// <summary>
        /// ��������� ����� ��� �������������� �������
        /// </summary>
        [Obsolete]
        private void DrawLine(ListBox sourceListBox, ListBox targetListBox, int indexOfDragField, int indexOfDropField, LineControl myLine)
        {
            // �������� ���������� �����
            LineCoordinates thisCoors = GetLineCoors(sourceListBox, targetListBox, indexOfDragField, indexOfDropField);

            // ������������� ���������� �����
            myLine.SetCoordinates(thisCoors.FromX, thisCoors.FromY, thisCoors.ToX, thisCoors.ToY);

        }

        /// <summary>
        /// ���������� ListBox
        /// </summary>
        [Obsolete]
        private void FillListBoxWithColumns(ListBox listBox, string tableName)
        {
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                const string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName";

                try
                {
                    using (var dbConnect = new Connect())
                    using (var command = new SqlCommand(query, dbConnect.Cnn))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName.ToLower());

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listBox.Items.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageDialog.Show("������ ��� ���������� ListBox", ex.Message);
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                const string query = @"SELECT column_name 
                       FROM information_schema.columns 
                       WHERE table_name = @tableName";
                string toLowertableName = tableName.ToLower();
                try
                {
                    using (var dbConnect = new ConnectPG()) // ���������� ��� Connect ����� ��� PostgreSQL
                    using (var command = new NpgsqlCommand(query, dbConnect.Cnn))
                    {
                        // ���������� ����������
                        command.Parameters.AddWithValue("@tableName", NpgsqlTypes.NpgsqlDbType.Text, toLowertableName);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listBox.Items.Add(reader.GetString(0)); // ��������� ����� ������� � ListBox
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageDialog.Show("������ ��� ���������� ListBox", ex.Message);
                }
            }
        }

        /// <summary>
        /// ������� ������ �������������� 
        /// </summary>
        private void ThisDragOver(object? sender, DragEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.Tag is string elementName && elementName == "DRAGDROP_ELEMENT" && IsDragDropMode)
            {
                var targetListBox = listBoxItem.FindLogicalAncestorOfType<ListBox>();
                // �������� ������������ ListBox
                if (targetListBox != SourceListBox)
                {
                    // ���������� ������ ��������, ��� ������� ���������� ��������������
                    var position = e.GetPosition(targetListBox);
                    IndexOfDropField = GetIndexFromPoint(targetListBox, position);

                    if (IndexOfDropField >= 0 && IndexOfDropField < targetListBox.ItemCount)
                    {
                        // �������� ������� �������
                        targetListBox.SelectedIndex = IndexOfDropField;
                        e.DragEffects = DragDropEffects.Link;
                        TargetListBox = targetListBox;

                        // ��������� ������ �� ������������ ���������
                        if (targetListBox.Parent is Border contentPanel &&
                         contentPanel.Parent is Grid grid &&
                         grid.Parent is Border thisPanel &&
                         thisPanel.Tag is PanelTag thisTag)
                       
                        //if (targetListBox.Parent is Border contentPanel &&
                        //    contentPanel.Parent is StackPanel mainStack &&
                        //    mainStack.Parent is Border thisPanel &&
                        //    thisPanel.Tag is PanelTag thisTag)
                        {
                            TargetTable_id = thisTag.Table_id;
                            TargetTableAlias = thisTag.TableAlias;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ������� ����� �������������� 
        /// </summary>
        private async void ThisDragDrop(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.Tag is string elementName && elementName == "DRAGDROP_ELEMENT")
            {
                // ����� ������� ��� �������� �����
                await CreateJoinAsync();
            }
        }

        /// <summary>
        /// ������� �������������� ����� �� ��������� �����
        /// </summary>
        private void ThisDragLeave(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.Tag is string elementName && elementName == "DRAGDROP_ELEMENT" && IsDragDropMode)
            {
                // ����� �������� ��������
                TargetListBox = null;
            }
        }

        /// <summary>
        /// ����� ��� ����������� ������� �������� �� �����
        /// </summary>
        private int GetIndexFromPoint(ListBox listBox, Point position)
        {
            for (int i = 0; i < listBox.ItemCount; i++)
            {
                var container = listBox.ContainerFromIndex(i) as Control;
                if (container != null && container.Bounds.Contains(position))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// ��������� �����
        /// </summary>
        private async Task UpdateJoinAsync()
        {
            if (DataListBox.SelectedIndex >= 0)
            {
                // ���������� � ���������
                IndexPair? selectedPair = null;
                JoinStructure? selectedJoin = null;
                int recordId = -1, itemIndex = -1;

                // ��������� �������������� ������
                foreach (var pair in MainWindowViewModel.CurrentJoins)
                {
                    if (pair.ItemIndex == DataListBox.SelectedIndex)
                    {
                        selectedPair = pair;
                        break;
                    }
                }

                if (selectedPair != null)
                {
                    foreach (var join in MainWindowViewModel.JoinLines)
                    {
                        if (join.Record_id.ToString() == selectedPair.ItemValue)
                        {
                            selectedJoin = join;
                            itemIndex = MainWindowViewModel.JoinLines.IndexOf(join);
                            break;
                        }
                    }
                }

                if (selectedJoin != null)
                {
                    // �������� � ����������� ����
                    var joinDialog = new JoinPropertiesDialog(selectedJoin.Join_id, selectedJoin.SourceTableName, selectedJoin.SourceColumnName, selectedJoin.TargetTableName, selectedJoin.TargetColumnName, selectedJoin.ConditionStatement);
                   
                    var dialogResult = await joinDialog.ShowDialog<bool>(this);

                    if (dialogResult && joinDialog.IsReturnValue)
                    {
                        // ���������� ������
                        selectedJoin.ConditionStatement = joinDialog.WhereStatement;
                        selectedJoin.Join_id = joinDialog.Join_id;
                        MainWindowViewModel.JoinLines[itemIndex] = selectedJoin;

                        if (MainWindowViewModel.dbms == DBMS.MS)
                        {
                            using (var dbConnection = new Connect())
                            {
                                SqlConnection connection = dbConnection.Cnn;

                                string updateQuery = @"UPDATE ReporterTableJoins SET ConditionStatement = @ConditionStatement, ReporterJoin_id = @ReporterJoin_id WHERE tid = @tid";
                                using (var updateCommand = new SqlCommand(updateQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@ConditionStatement", joinDialog.WhereStatement);
                                    updateCommand.Parameters.AddWithValue("@ReporterJoin_id", joinDialog.Join_id);
                                    updateCommand.Parameters.AddWithValue("@tid", selectedJoin.Record_id);

                                    updateCommand.ExecuteNonQuery();
                                }

                            }
                        }
                        else if (MainWindowViewModel.dbms == DBMS.PG)
                        {
                            using (var dbConnection = new ConnectPG()) // ���������� ����� ����������� ��� PostgreSQL
                            {
                                var connection = dbConnection.Cnn;

                                string updateQuery = @"UPDATE ReporterTableJoins SET ConditionStatement = @ConditionStatement, ReporterJoin_id = @ReporterJoin_id WHERE tid = @tid";

                                using (var updateCommand = new NpgsqlCommand(updateQuery, connection))
                                {
                                    // ���������� ���������� � �� ������
                                    updateCommand.Parameters.AddWithValue("@ConditionStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(joinDialog.WhereStatement));
                                    updateCommand.Parameters.AddWithValue("@ReporterJoin_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(joinDialog.Join_id));
                                    updateCommand.Parameters.AddWithValue("@tid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedJoin.Record_id));

                                    // ���������� �������
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                        }
 
                    }
                }
            }
        }

        /// <summary>
        /// ������� �����
        /// </summary>
        private async Task CreateJoinAsync()
        {
            string fromField = string.Empty;
            string toField = string.Empty;

            try
            {
                fromField = SourceListBox.SelectedItem?.ToString();
                toField = TargetListBox.SelectedItem?.ToString();
            }
            catch
            {
                fromField = null; 
                toField = null;

            }

            if (fromField != null && toField != null)
            {
                var myForm = new JoinPropertiesDialog("", SourceTableAlias, fromField, TargetTableAlias, toField,
               $"{SourceTableAlias}.{fromField} = {TargetTableAlias}.{toField}");

                var dialogResult = await myForm.ShowDialog<bool>(this);
                if (dialogResult != null && myForm.IsReturnValue)
                {
                    var thisCoors = GetLineCoors(SourceListBox, TargetListBox, IndexOfDragField, IndexOfDropField);
                    var myLine = new LineControl(OLAPPanel, thisCoors.FromX, thisCoors.FromY, thisCoors.ToX, thisCoors.ToY,
                        JoinColor, MainWindowViewModel.JoinWidth, true);


                    string sourceTableId = await GetTableIdAsync(SourceTableAlias);
                    string targetTableId = await GetTableIdAsync(TargetTableAlias);

                    string thisGuid = await GetGUIDAsync();
                    await InsertJoinAsync(sourceTableId, targetTableId, myForm.Join_id, myForm.WhereStatement, fromField, toField, thisGuid);

                    int thisJoinId = await GetJoinIdAsync(thisGuid);

                    var thisStructure = new JoinStructure
                    {
                        Record_id = thisJoinId,
                        SourceTableName = SourceTableAlias,
                        TargetTableName = TargetTableAlias,
                        SourceColumnName = fromField,
                        TargetColumnName = toField,
                        Join_id = myForm.Join_id,
                        SourceListBox = SourceListBox,
                        TargetListBox = TargetListBox,
                        SourceIndex = IndexOfDragField,
                        TargetIndex = IndexOfDropField,
                        ConditionStatement = myForm.WhereStatement,
                        Line = myLine,
                        SourcePanel = GetParentPanel(SourceListBox),
                        TargetPanel = GetParentPanel(TargetListBox)
                    };
                    MainWindowViewModel.JoinLines.Add(thisStructure);

                    if ((CurrentPanel == thisStructure.SourcePanel) || (CurrentPanel == thisStructure.TargetPanel))
                    {
                        UpdateCurrentPanel(CurrentPanel);
                    }
                }
            }

        }

        /// <summary>
        /// �������� GUID
        /// </summary>
        private async Task<string> GetGUIDAsync()
        {
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                // Ensure the database connection class `Connect` is properly implemented to manage connections.
                using (var dbConnection = new Connect())
                {
                    SqlConnection connection = dbConnection.Cnn;

                    // Open the connection if it is not already open.
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    const string query = "SELECT ThisGUID = CAST(NEWID() AS VARCHAR(50))";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetString(0); // Return the generated GUID.
                        }
                    }
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                using (var dbConnection = new ConnectPG()) // ��� ����� ����������� ��� PostgreSQL
                {
                    var connection = dbConnection.Cnn;

                    // ��������� ����������, ���� ��� ��� �� �������
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    const string query = "SELECT gen_random_uuid()::text AS ThisGUID";

                    using (var command = new NpgsqlCommand(query, connection)) // ���������� NpgsqlCommand ��� PostgreSQL
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetString(0); // ���������� ��������������� UUID ��� ������
                        }
                    }
                }
            }

                // If no GUID is generated or an issue occurs, throw an exception.
            throw new InvalidOperationException("Failed to generate GUID from the database.");
        }

        private async Task<string> GetTableIdAsync(string tableAlias)
        {
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                string query = @"SELECT TOP 1 tid FROM ReporterTables WITH (NOLOCK) WHERE TableAlias = @TableAlias AND ReporterLayer_id = @ReporterLayerId";

                using (var dbConnection = new Connect())
                {
                    SqlConnection connection = dbConnection.Cnn;
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TableAlias", tableAlias);
                        command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return reader["tid"].ToString();
                            }
                        }
                    }
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                const string query = @"SELECT tid 
                       FROM ReporterTables 
                       WHERE TableAlias = @TableAlias AND ReporterLayer_id = @ReporterLayerId 
                       LIMIT 1";

                using (var dbConnection = new ConnectPG()) // ���������� ��� ����� ����������� ��� PostgreSQL
                {
                    var connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new NpgsqlCommand(query, connection)) // ���������� NpgsqlCommand ��� PostgreSQL
                    {
                        // ��������� ���������
                        command.Parameters.AddWithValue("@TableAlias", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(tableAlias));
                        command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return reader["tid"].ToString(); // ���������� �������� tid ��� ������
                            }
                        }
                    }
                }
            }
           

            throw new InvalidOperationException($"No table ID found for alias '{tableAlias}' and ReporterLayer_id '{MainWindowViewModel.ReporterLayer_id}'.");
        }

        private async Task InsertJoinAsync(string sourceTableId, string targetTableId, string joinId, string whereStatement, string fromField, string toField, string guid)
        {

            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                const string query = @"INSERT INTO ReporterTableJoins (System_id, SourceTable_id, TargetTable_id, ReporterJoin_id, ConditionStatement, SourceColumn, TargetColumn, [GUID], ReporterLayer_id) VALUES (@SystemId, @SourceTableId, @TargetTableId, @JoinId, @WhereStatement, @FromField, @ToField, @Guid, @ReporterLayerId)";
                using (var dbConnection = new Connect())
                {
                    SqlConnection connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Add parameters to prevent SQL injection.
                        command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);
                        command.Parameters.AddWithValue("@SourceTableId", sourceTableId);
                        command.Parameters.AddWithValue("@TargetTableId", targetTableId);
                        command.Parameters.AddWithValue("@JoinId", joinId);
                        command.Parameters.AddWithValue("@WhereStatement", whereStatement.Replace("'", "''"));
                        command.Parameters.AddWithValue("@FromField", fromField);
                        command.Parameters.AddWithValue("@ToField", toField);
                        command.Parameters.AddWithValue("@Guid", guid);
                        command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                        // Execute the insert command asynchronously.
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                const string query = @"INSERT INTO ReporterTableJoins (System_id, SourceTable_id, TargetTable_id, ReporterJoin_id, ConditionStatement, SourceColumn, TargetColumn, [GUID], ReporterLayer_id) VALUES (@SystemId, @SourceTableId, @TargetTableId, @JoinId, @WhereStatement, @FromField, @ToField, @Guid, @ReporterLayerId)";
                using (var dbConnection = new ConnectPG()) // ���������� ����� ����������� ��� PostgreSQL
                {
                    var connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new NpgsqlCommand(query, connection)) // ���������� NpgsqlCommand
                    {
                        // ���������� ���������� � ��������� ����
                        command.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                        command.Parameters.AddWithValue("@SourceTableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(sourceTableId));
                        command.Parameters.AddWithValue("@TargetTableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(targetTableId));
                        command.Parameters.AddWithValue("@JoinId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(joinId));
                        command.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(whereStatement.Replace("'", "''")));
                        command.Parameters.AddWithValue("@FromField", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(fromField));
                        command.Parameters.AddWithValue("@ToField", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(toField));
                        command.Parameters.AddWithValue("@Guid", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(guid));
                        command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                        // ���������� ������� ����������
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
         
        }

        private async Task<int> GetJoinIdAsync(string guid)
        {   
            if(MainWindowViewModel.dbms == DBMS.MS)
            {
                const string query = @"SELECT TOP 1 tid FROM ReporterTableJoins WITH (NOLOCK) WHERE [GUID] = @Guid AND ReporterLayer_id = @ReporterLayerId";
                using (var dbConnection = new Connect())
                {
                    SqlConnection connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        // Add parameters to prevent SQL injection.
                        command.Parameters.AddWithValue("@Guid", guid);
                        command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return reader.GetInt32(0); // Return the `tid` from the first column.
                            }
                        }
                    }
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                const string query = @"SELECT tid FROM ReporterTableJoins WHERE guid = @Guid AND ReporterLayer_id = @ReporterLayerId LIMIT 1";

                using (var dbConnection = new ConnectPG()) // ���������� ����� ����������� � PostgreSQL
                {
                    var connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new NpgsqlCommand(query, connection)) // ���������� NpgsqlCommand
                    {
                        // ���������� ���������� � ��������� ����
                        command.Parameters.AddWithValue("@Guid", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(guid));
                        command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return reader.GetInt32(0); // ���������� `tid` �� ������ �������.
                            }
                        }
                    }
                }
            }

            throw new InvalidOperationException($"No join found for GUID '{guid}' in layer ID '{MainWindowViewModel.ReporterLayer_id}'.");
        }

        /// <summary>
        /// ��������� ������� ������ ��� ������ Olap - �������
        /// </summary>
        private void UpdateCurrentPanel(Border currentPanel)
        {
            if (MainWindowViewModel.CurrentJoins == null)
            {
                MainWindowViewModel.CurrentJoins = new ObservableCollection<IndexPair>();
            }

            MainWindowViewModel.CurrentJoins.Clear();
            DataListBox.Items.Clear();

            string itemName = string.Empty;
            int recordId = -1;

            DataListBox.BeginInit();
            
            for (int i = 0; i < MainWindowViewModel.JoinLines.Count; i++)
            {
                itemName = string.Empty;

                var join = (JoinStructure)MainWindowViewModel.JoinLines[i];

                if (currentPanel == join.SourcePanel)
                {
                    itemName = $"{join.TargetTableName}.{join.TargetColumnName}";
                    recordId = join.Record_id;
                }
                else if (currentPanel == join.TargetPanel)
                {
                    itemName = $"{join.SourceTableName}.{join.SourceColumnName}";
                    recordId = join.Record_id;
                }

                if (!string.IsNullOrEmpty(itemName))
                {
                    var indexPair = new IndexPair
                    {
                        ItemIndex = DataListBox.Items.Add(itemName),
                        ItemValue = recordId.ToString()
                    };

                    MainWindowViewModel.CurrentJoins.Add(indexPair);
                }
            }
            DataListBox.EndInit();
          
        }

        public async Task AddTableAsync()
        {
            var myForm = new ChooseTableDialog();
            var myBox = myForm.FindControl<ListBox>("TableList");
            var myText = myForm.FindControl<TextBox>("AliasTextBox");

            bool hasItems = false;

            myBox.Items.Clear();

            if(MainWindowViewModel.dbms == DBMS.MS)
            {
                using (var dbConnection = new Connect())
                {
                    SqlConnection connection = dbConnection.Cnn;

                    // Query to fetch table names
                    var commandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME NOT LIKE 'temp[_]%' ORDER BY TABLE_NAME ASC";
                    using (var command = new SqlCommand(commandText, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            hasItems = true;
                            myBox.Items.Add(reader.GetString(0));
                        }
                    }

                    if (hasItems)
                    {
                        myBox.SelectedIndex = 0;
                    }
                    else
                    {
                        return;
                    }

                    // Show dialog and await user interaction
                    await myForm.ShowDialog(this);

                    if (MainWindowViewModel.IsAccepted == false)
                    {
                        return;
                    }

                    string tableName = myForm.CurrentValue;
                    string tableAlias = myText.Text.ToString().Trim();


                    int counter = await CheckTableAliasAsync(connection, tableAlias);

                    if (counter > 0)
                    {
                        MessageDialog.Show("���������� �������", "������� � ����� ������� ��� ����������!");
                        return;
                    }

                    if (MainWindowViewModel.IsAccepted && !string.IsNullOrEmpty(tableAlias))
                    {
                        var thisReturn = CreateCustomPanel(tableAlias, OLAPPanel, tableName, -1, -1, -1);
                        var customPanel = thisReturn.ThisPanel;
                        var customList = thisReturn.ThisListBox;
                        thisReturn.ThisPanel.ZIndex = 1; // ������ ����� ����

                        string x = customPanel.Margin.Left.ToString();
                        string y = customPanel.Margin.Top.ToString();
                        int tableId = await InsertTableAsync(connection, tableAlias, tableName, x, y);

                        // Link table ID with the custom panel
                        var thisTag = new PanelTag
                        {
                            Table_id = tableId,
                            TableName = tableName,
                            TableAlias = tableAlias
                        };
                        customPanel.Tag = thisTag;

                        // Create and add table to processing list
                        AddTableToProcessingList(customPanel, customList, tableAlias, tableName, tableId);
                    }
                    else
                    {
                        MessageDialog.Show("���������� �������", "�� ����� ����� �������!");
                    }

                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                using (var dbConnection = new ConnectPG()) // ���������� ��� ����� ConnectPG
                {
                    var connection = dbConnection.Cnn;

                    // ��������, ��� ���������� �������
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    // ������ ��� ��������� ���� ������
                    const string commandText = @"SELECT table_name 
                                 FROM information_schema.tables 
                                 WHERE table_name NOT LIKE 'temp[_]%' 
                                 ORDER BY table_name ASC";

                    using (var command = new NpgsqlCommand(commandText, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            hasItems = true;
                            myBox.Items.Add(reader.GetString(0)); // ��������� ��� ������� � ������
                        }
                    }

                    if (hasItems)
                    {
                        myBox.SelectedIndex = 0; // �������� ������ �������
                    }
                    else
                    {
                        return; // ���� ������ ����, ������ �� ������
                    }

                    // ���������� ������ � ���� �������������� ������������
                    await myForm.ShowDialog(this);

                    if (!MainWindowViewModel.IsAccepted)
                    {
                        return; // ���� ������������ �������, �������
                    }

                    string tableName = myForm.CurrentValue;
                    string tableAlias = myText.Text.ToString().Trim();

                    // ���������, ���������� �� ������� � ����� �������
                    int counter = await CheckTableAliasPGAsync(connection, tableAlias);

                    if (counter > 0)
                    {
                        MessageDialog.Show("���������� �������", "������� � ����� ������� ��� ����������!");
                        return;
                    }

                    if (MainWindowViewModel.IsAccepted && !string.IsNullOrEmpty(tableAlias))
                    {
                        var thisReturn = CreateCustomPanel(tableAlias, OLAPPanel, tableName, -1, -1, -1);
                        var customPanel = thisReturn.ThisPanel;
                        var customList = thisReturn.ThisListBox;
                        customPanel.ZIndex = 1; // ������ ����� ����

                        string x = customPanel.Margin.Left.ToString();
                        string y = customPanel.Margin.Top.ToString();

                        // ��������� ����� ������� � ���� ������ � �������� �� ID
                        int tableId = await InsertTablePGAsync(connection, tableAlias, tableName, x, y);

                        // ��������� ID ������� � ���������������� �������
                        var thisTag = new PanelTag
                        {
                            Table_id = tableId,
                            TableName = tableName,
                            TableAlias = tableAlias
                        };
                        customPanel.Tag = thisTag;

                        // ������� � ��������� ������� � ������ ���������
                        AddTableToProcessingList(customPanel, customList, tableAlias, tableName, tableId);
                    }
                    else
                    {
                        MessageDialog.Show("���������� �������", "�� ����� ����� �������!");
                    }
                }
            }
        }
        private async Task<int> CheckTableAliasPGAsync(NpgsqlConnection connection, string tableAlias)
        {
            const string checkAliasCommand = @"SELECT COUNT(*) 
                                       FROM ReporterTables 
                                       WHERE TableAlias = @TableAlias 
                                       AND ReporterLayer_id = @ReporterLayerId";
            using (var command = new NpgsqlCommand(checkAliasCommand, connection))
            {
                command.Parameters.AddWithValue("@TableAlias", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(tableAlias));
                command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }
        private async Task<int> CheckTableAliasAsync(SqlConnection connection, string tableAlias)
        {
            var checkAliasCommand = "SELECT COUNT(*) FROM ReporterTables with (nolock) WHERE TableAlias = @TableAlias AND ReporterLayer_id = @ReporterLayerId";
            using (var command = new SqlCommand(checkAliasCommand, connection))
            {
                command.Parameters.AddWithValue("@TableAlias", tableAlias);
                command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                return (int)await command.ExecuteScalarAsync();
            }
        }
        private async Task<int> InsertTablePGAsync(NpgsqlConnection connection, string tableAlias, string tableName, string x, string y)
        {
            const string insertTableCommand = @"INSERT INTO ReporterTables 
                                        (System_id, TableName, TableAlias, X, Y, ReporterLayer_id) 
                                        VALUES (@SystemId, @TableName, @TableAlias, @X, @Y, @ReporterLayerId)";
            using (var command = new NpgsqlCommand(insertTableCommand, connection))
            {
                command.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                command.Parameters.AddWithValue("@TableName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(tableName));
                command.Parameters.AddWithValue("@TableAlias", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(tableAlias));
                command.Parameters.AddWithValue("@X", NpgsqlTypes.NpgsqlDbType.Double, Convert.ToDouble(x));
                command.Parameters.AddWithValue("@Y", NpgsqlTypes.NpgsqlDbType.Double, Convert.ToDouble(y));
                command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                await command.ExecuteNonQueryAsync();
            }

            const string selectTableIdCommand = @"SELECT tid 
                                          FROM ReporterTables 
                                          WHERE TableAlias = @TableAlias AND ReporterLayer_id = @ReporterLayerId";
            using (var command = new NpgsqlCommand(selectTableIdCommand, connection))
            {
                command.Parameters.AddWithValue("@TableAlias", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(tableAlias));
                command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return reader.GetInt32(0);
                    }
                }
            }

            return -1; // ���� ������ �� �������
        }
        private async Task<int> InsertTableAsync(SqlConnection connection, string tableAlias, string tableName, string x, string y)
        {
            var insertTableCommand = "INSERT INTO ReporterTables (System_id, TableName, TableAlias, X, Y, ReporterLayer_id) " +
                                     "VALUES (@SystemId, @TableName, @TableAlias, @X, @Y, @ReporterLayerId)";
            using (var command = new SqlCommand(insertTableCommand, connection))
            {
                command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@TableAlias", tableAlias);
                command.Parameters.AddWithValue("@X", x);
                command.Parameters.AddWithValue("@Y", y);
                command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                await command.ExecuteNonQueryAsync();
            }

            var selectTableIdCommand = "SELECT tid FROM ReporterTables with (nolock) WHERE TableAlias = @TableAlias AND ReporterLayer_id = @ReporterLayerId";
            using (var command = new SqlCommand(selectTableIdCommand, connection))
            {
                command.Parameters.AddWithValue("@TableAlias", tableAlias);
                command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return reader.GetInt32(0);
                    }
                }
            }

            return -1;
        }

        private void AddTableToProcessingList(Border customPanel, ListBox customList, string tableAlias, string tableName, int tableId)
        {
            var tablesList = new TablesList
            {
                CustomPanel = customPanel,
                CustomList = customList,
                Table_id = tableId,
                TableAlias = tableAlias,
                TableName = tableName,
                X = (int)customPanel.Margin.Left,
                Y = (int)customPanel.Margin.Top
            };

            if (MainWindowViewModel.ProcessingTables == null)
            {
                MainWindowViewModel.ProcessingTables = new ObservableCollection<TablesList>();
            }

            MainWindowViewModel.ProcessingTables.Add(tablesList);
        }

        public async Task DropDataAsync()
        {
            // ��������� ��� ������� � UI-������
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (MainWindowViewModel.ProcessingTables != null)
                {
                    foreach (var table in MainWindowViewModel.ProcessingTables)
                    {
                        if (table.CustomPanel?.Parent is Panel parentPanel)
                        {
                            parentPanel.Children.Remove(table.CustomPanel);
                        }
                    }
                    MainWindowViewModel.ProcessingTables.Clear();
                }

                if (MainWindowViewModel.JoinLines != null)
                {
                    foreach (var join in MainWindowViewModel.JoinLines)
                    {
                        if (join.Line?.Parent is Panel parentPanel)
                        {
                            parentPanel.Children.Remove(join.Line);
                        }
                    }
                    MainWindowViewModel.JoinLines.Clear();
                }

                // �������� DropData_Objects ��� ������� ��������
                DropData_Objects();
            });
        }

        /// <summary>
        /// ���������� ������� �� OLAP
        /// </summary>
        private async void OpenButton_Click(object? sender, RoutedEventArgs e)
        {
            await AddTableAsync();
        }

        /// <summary>
        /// �������� ������� � OLAP
        /// </summary>
        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = await ShowMessageDialogAsync("������� ��������� �������?", "�������� �������");
            if (result == true) // `true` for OK, `false` for Cancel
            {
                PanelTag CurrentTag = (PanelTag)CurrentPanel.Tag;

                var itemToRemove = MainWindowViewModel.ProcessingTables.FirstOrDefault(t => t.Table_id == CurrentTag.Table_id);

                if (itemToRemove != null)
                {
                    MainWindowViewModel.ProcessingTables.Remove(itemToRemove);

                    if (CurrentPanel.Parent is Canvas parentPanel)
                    {
                        parentPanel.Children.Remove(CurrentPanel);
                    }


                    VisiblePanel.UpdateLayout();
                    if (MainWindowViewModel.dbms == DBMS.MS)
                    {
                        using (var dbConnection = new Connect())
                        {
                            SqlConnection connection = dbConnection.Cnn;

                            var insertTableCommand = "Delete from ReporterTables Where tid = @tid";
                            using (var command = new SqlCommand(insertTableCommand, connection))
                            {
                                command.Parameters.AddWithValue("@tid", CurrentTag.Table_id);


                                await command.ExecuteNonQueryAsync();
                            }

                        }
                    }
                    else if (MainWindowViewModel.dbms == DBMS.PG)
                    {
                        using (var dbConnection = new ConnectPG())
                        {
                            NpgsqlConnection connection = dbConnection.Cnn;

                            const string deleteTableCommand = "DELETE FROM ReporterTables WHERE tid = @tid";
                            using (var command = new NpgsqlCommand(deleteTableCommand, connection))
                            {
                                command.Parameters.AddWithValue("@tid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(CurrentTag.Table_id));

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                       
                }
            }

        }

        /// <summary>
        /// ������� �������� �����
        /// </summary>
        private async void DisableButton_Click(object? sender, RoutedEventArgs e)
        {
            if (MainWindowViewModel.CurrentJoins == null)
            {
                MainWindowViewModel.CurrentJoins = new ObservableCollection<IndexPair>();
            }

            if (DataListBox.SelectedIndex >= 0)
            {
                var result = await ShowMessageDialogAsync("������� ��������� �����?", "�������� �����");
                if (result == true) // `true` for OK, `false` for Cancel
                {
                    // ��������� �������������� ������
                    IndexPair selectedPair = MainWindowViewModel.CurrentJoins.FirstOrDefault(pair => pair.ItemIndex == DataListBox.SelectedIndex);
                    if (selectedPair == null)
                    {
                        return; // Nothing to delete
                    }

                    JoinStructure selectedJoin = MainWindowViewModel.JoinLines.Cast<JoinStructure>().FirstOrDefault(join => join.Record_id.ToString() == selectedPair.ItemValue);

                    if (selectedJoin != null)
                    {
                        // 1. �������� �������������� �����
                        selectedJoin.Line.Remove();

                        // 2. �������� ������ � ����� � ����������� ������
                        MainWindowViewModel.JoinLines.Remove(selectedJoin);

                        // 3. �������� ������ � ����� �� ��������� ����������
                        MainWindowViewModel.CurrentJoins.Remove(selectedPair);

                        // 4. �������� ����� � ���� ������
                        await DeleteJoinFromDatabaseAsync(selectedPair.ItemValue);

                        // 5. �������� ������ �� ������ �� ������

                        MainWindowViewModel.CurrentJoins.Remove(selectedPair);

                        UpdateCurrentPanel(CurrentPanel);

                    }
                }
            }

        }
        private async Task<bool> ShowMessageDialogAsync(string message, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,

            };

            // �������� ����������� �������
            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 50)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesButton = new Button
            {
                Content = "��",
                Width = 80,
                Margin = new Thickness(5, 0, 5, 0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var noButton = new Button
            {
                Content = "���",
                Width = 80,
                Margin = new Thickness(5, 0, 5, 0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            // ����������� ������
            yesButton.Click += (_, _) =>
            {
                dialog.Close(true); // ������� ���� � ����������� true
            };

            noButton.Click += (_, _) =>
            {
                dialog.Close(false); // ������� ���� � ����������� false
            };

            // ���������� ������ � ������
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            // ���������� ������ � ������ � ����������
            content.Children.Add(textBlock);
            content.Children.Add(buttonPanel);

            dialog.Content = content;

            // �������� ���������� �������
            var result = await dialog.ShowDialog<bool>(this);
            return result;
        }
        private async Task DeleteJoinFromDatabaseAsync(string recordId)
        {
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                const string query = "DELETE FROM ReporterTableJoins WHERE tid = @RecordId AND ReporterLayer_id = @ReporterLayerId";

                using (var dbConnection = new Connect())
                {
                    SqlConnection connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RecordId", recordId);
                        command.Parameters.AddWithValue("@ReporterLayerId", MainWindowViewModel.ReporterLayer_id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                const string query = "DELETE FROM ReporterTableJoins WHERE tid = @RecordId AND ReporterLayer_id = @ReporterLayerId";

                using (var dbConnection = new ConnectPG()) // ���������� ��� ����� ����������� ��� PostgreSQL
                {
                    NpgsqlConnection connection = dbConnection.Cnn;

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@RecordId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(recordId));
                        command.Parameters.AddWithValue("@ReporterLayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// ������� ��������� �����
        /// </summary>
        private void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            // ��������� ��������������
            if (DataListBox.SelectedIndex >= 0)
            {
                UpdateJoinAsync();
            }
        }

        /// <summary>
        /// ������� ���������� ��
        /// </summary>
        private async void InfoButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = await ShowMessageDialogAsync("������������� ������ ��?", "���������� ������ OLAP");
            if (result == true) // `true` for OK, `false` for Cancel
            {
                LoadLayers();
                await InitializeAsync();
            }
        }

        /// <summary>
        /// ������� �������� �������
        /// </summary>
        private async void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            if (CurrentPanel == null)
            {
                MessageDialog.Show("����������", "�������� �������� ������� � �������� ���� ��� ���������");
            }
            else
            {
                TablesList? ThisTable = null;
                ListBox? ThisList = null;
                string TempValue;
                string ThisGUID = await GetGUIDAsync();

                foreach (var table in MainWindowViewModel.ProcessingTables)
                {
                    if (table.CustomPanel == CurrentPanel)
                    {
                        ThisTable = table;
                        ThisList = table.CustomList;

                        if (ThisList?.SelectedIndex < 0)
                        {
                            MessageDialog.Show("����������", "�������� ���� ��� �������� �������");
                        }
                        else
                        {
                            if (MainWindowViewModel.dbms == DBMS.MS)
                            {
                                // ������ �������� �������
                                using (var dbConnection = new Connect())
                                {
                                    var ThisCommand = new SqlCommand
                                    {
                                        Connection = dbConnection.Cnn
                                    };
                                    string Dimension_id = "NULL", Measure_id = "NULL", Detail_id = "NULL", ReporterTable_id = "";

                                    ObjectDialog myForm = new ObjectDialog(ThisTable.Table_id.ToString());

                                    myForm.FindControl<TextBox>("SelectStatement").Text = ThisList.Items[ThisList.SelectedIndex]?.ToString() ?? string.Empty;

                                    await myForm.ShowDialog(this);

                                    if (myForm.IsReturnValue)
                                    {
                                        ReporterTable_id = myForm.ReturnTable_id;

                                        if (myForm.ObjectType == 0)
                                        {
                                            ThisCommand.CommandText = $@"INSERT INTO ReporterDimensions (System_id, ReporterClass_id, ReporterTable_id, DimensionName, AssociatedColumn, SelectStatement, WhereStatement, [GUID])
                                        VALUES ({MainWindowViewModel.System_id}, {myForm.ReporterClass_id}, {ReporterTable_id}, '{myForm.ReturnObjectName.Replace("'", "''")}', '{myForm.ReturnSelectStatement.Replace("'", "''")}', '{myForm.ReturnWhereStatement.Replace("'", "''")}', '{ThisGUID}')";
                                            await ThisCommand.ExecuteNonQueryAsync();

                                            ThisCommand.CommandText = $"SELECT TOP 1 tid FROM ReporterDimensions WHERE [GUID] = '{ThisGUID}'";
                                            using var reader = await ThisCommand.ExecuteReaderAsync();
                                            if (reader.Read())
                                            {
                                                Dimension_id = reader.GetValue(0).ToString();
                                            }
                                            reader.Close();
                                            Measure_id = "NULL";
                                            Detail_id = "NULL";
                                        }
                                        else if (myForm.ObjectType == 1)
                                        {

                                            if (myForm.ReturnIsFloat) TempValue = "1"; else TempValue = "0";
                                            ThisCommand.CommandText = "INSERT INTO ReporterMeasures (System_id, ReporterClass_id, ReporterTable_id, MeasureName, AssociatedColumn, SelectStatement, WhereStatement, ReporterAggregate_id, IsFloat, [GUID]) VALUES (" + MainWindowViewModel.System_id + ", " + myForm.ReporterClass_id + ", " + ReporterTable_id + ", '" + myForm.ReturnObjectName.Replace("'", "''") + "', '" + ThisList.Items[ThisList.SelectedIndex].ToString() + "', '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', '" + myForm.ReturnWhereStatement.Replace("'", "''") + "', " + myForm.ReporterAggregate_id + ", '" + TempValue + "', '" + ThisGUID + "')";
                                            await ThisCommand.ExecuteNonQueryAsync();
                                            ThisCommand.CommandText = "SELECT TOP 1 tid FROM ReporterMeasures with (nolock) WHERE [GUID] = '" + ThisGUID + "'";
                                            using var reader = await ThisCommand.ExecuteReaderAsync();
                                            if (reader.Read())
                                            {
                                                Measure_id = reader.GetValue(0).ToString();
                                            }
                                            reader.Close();
                                            Dimension_id = "NULL";
                                            Detail_id = "NULL";
                                        }
                                        else if (myForm.ObjectType == 2)
                                        {
                                            ThisCommand.CommandText = "INSERT INTO ReporterDetails (System_id, ReporterClass_id, ReporterDimension_id, DetailName, AssociatedColumn, SelectStatement, WhereStatement, [GUID]) VALUES (" + myForm.ReporterClass_id + ", " + MainWindowViewModel.System_id + ", " + myForm.ReporterDimension_id + ", '" + myForm.ReturnObjectName.Replace("'", "''") + "', '" + ThisList.Items[ThisList.SelectedIndex].ToString() + "', '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', '" + myForm.ReturnWhereStatement.Replace("'", "''") + "', '" + ThisGUID + "')";
                                            await ThisCommand.ExecuteNonQueryAsync();
                                            ThisCommand.CommandText = "SELECT TOP 1 tid FROM ReporterDetails with (nolock) WHERE [GUID] = '" + ThisGUID + "'";
                                            using var reader = await ThisCommand.ExecuteReaderAsync();
                                            if (reader.Read())
                                            {
                                                Detail_id = reader.GetValue(0).ToString();
                                            }
                                            reader.Close();
                                            Dimension_id = "NULL";
                                            Measure_id = "NULL";
                                        }

                                        await FinalizeObjectCreation(ThisCommand, myForm, Dimension_id, Measure_id, Detail_id, ThisGUID);

                                        DropData_Objects();
                                        await LoadObjectsAsync();
                                    }
                                }
                            }
                            else if (MainWindowViewModel.dbms == DBMS.PG)
                            {
                                // ������� ����������� � ���� ������ PostgreSQL
                                using (var dbConnection = new ConnectPG())  // ��������������, ��� ConnectPG - ��� �����, ������� ������� ����������� � ���� ������
                                {
                                    var ThisCommand = new NpgsqlCommand
                                    {
                                        Connection = dbConnection.Cnn
                                    };

                                    string Dimension_id = "NULL", Measure_id = "NULL", Detail_id = "NULL", ReporterTable_id = "";
                                    ObjectDialog myForm = new ObjectDialog(ThisTable.Table_id.ToString());

                                    myForm.FindControl<TextBox>("SelectStatement").Text = ThisList.Items[ThisList.SelectedIndex]?.ToString() ?? string.Empty;
                                    await myForm.ShowDialog(this);

                                    if (myForm.IsReturnValue)
                                    {
                                        ReporterTable_id = myForm.ReturnTable_id;

                                        // ������� ������ � PostgreSQL
                                        if (myForm.ObjectType == 0)
                                        {
                                            // ����������������� ������ ��� ������� � ReporterDimensions
                                            ThisCommand.CommandText = @"INSERT INTO ReporterDimensions (System_id, ReporterClass_id, ReporterTable_id, DimensionName, AssociatedColumn, SelectStatement, WhereStatement, ""GUID"")
                    VALUES (@System_id, @ReporterClass_id, @ReporterTable_id, @DimensionName, @AssociatedColumn, @SelectStatement, @WhereStatement, @GUID) RETURNING tid";

                                            // ���������� ����������
                                            ThisCommand.Parameters.AddWithValue("@System_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                            ThisCommand.Parameters.AddWithValue("@ReporterClass_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                            ThisCommand.Parameters.AddWithValue("@ReporterTable_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(ReporterTable_id));
                                            ThisCommand.Parameters.AddWithValue("@DimensionName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName));
                                            ThisCommand.Parameters.AddWithValue("@AssociatedColumn", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(ThisList.Items[ThisList.SelectedIndex]?.ToString()));
                                            ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement));
                                            ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement));
                                            ThisCommand.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(ThisGUID));

                                            // ��������� ������ � �������� Dimension_id
                                            Dimension_id = (await ThisCommand.ExecuteScalarAsync()).ToString();
                                            Measure_id = "NULL";
                                            Detail_id = "NULL";
                                        }
                                        else if (myForm.ObjectType == 1)
                                        {
                                            // ���������� ��� ReporterMeasures
                                            ThisCommand.CommandText = @"INSERT INTO ReporterMeasures (System_id, ReporterClass_id, ReporterTable_id, MeasureName, AssociatedColumn, SelectStatement, WhereStatement, ReporterAggregate_id, IsFloat, ""GUID"")
                    VALUES (@System_id, @ReporterClass_id, @ReporterTable_id, @MeasureName, @AssociatedColumn, @SelectStatement, @WhereStatement, @ReporterAggregate_id, @IsFloat, @GUID) RETURNING tid";

                                            // ��������� ��� �������
                                            ThisCommand.Parameters.AddWithValue("@System_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                            ThisCommand.Parameters.AddWithValue("@ReporterClass_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                            ThisCommand.Parameters.AddWithValue("@ReporterTable_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(ReporterTable_id));
                                            ThisCommand.Parameters.AddWithValue("@MeasureName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName));
                                            ThisCommand.Parameters.AddWithValue("@AssociatedColumn", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(ThisList.Items[ThisList.SelectedIndex]?.ToString()));
                                            ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement));
                                            ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement));
                                            ThisCommand.Parameters.AddWithValue("@ReporterAggregate_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterAggregate_id));
                                            ThisCommand.Parameters.AddWithValue("@IsFloat", NpgsqlTypes.NpgsqlDbType.Boolean, Convert.ToBoolean(myForm.ReturnIsFloat ? 1 : 0));
                                            ThisCommand.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(ThisGUID));

                                            // ��������� ������ � �������� Measure_id
                                            Measure_id = (await ThisCommand.ExecuteScalarAsync()).ToString();
                                            Dimension_id = "NULL";
                                            Detail_id = "NULL";
                                        }
                                        else if (myForm.ObjectType == 2)
                                        {
                                            // ��� ReporterDetails
                                            ThisCommand.CommandText = @"INSERT INTO ReporterDetails (System_id, ReporterClass_id, ReporterDimension_id, DetailName, AssociatedColumn, SelectStatement, WhereStatement, ""GUID"")
                    VALUES (@System_id, @ReporterClass_id, @ReporterDimension_id, @DetailName, @AssociatedColumn, @SelectStatement, @WhereStatement, @GUID) RETURNING tid";

                                            // ��������� ��� �������
                                            ThisCommand.Parameters.AddWithValue("@System_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                            ThisCommand.Parameters.AddWithValue("@ReporterClass_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                            ThisCommand.Parameters.AddWithValue("@ReporterDimension_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterDimension_id));
                                            ThisCommand.Parameters.AddWithValue("@DetailName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName));
                                            ThisCommand.Parameters.AddWithValue("@AssociatedColumn", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(ThisList.Items[ThisList.SelectedIndex]?.ToString()));
                                            ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement));
                                            ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement));
                                            ThisCommand.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(ThisGUID));

                                            // ��������� ������ � �������� Detail_id
                                            Detail_id = (await ThisCommand.ExecuteScalarAsync()).ToString();
                                            Dimension_id = "NULL";
                                            Measure_id = "NULL";
                                        }

                                        // ����������� ���������
                                        await FinalizeObjectCreationPG(ThisCommand, myForm, Dimension_id, Measure_id, Detail_id, ThisGUID);

                                        DropData_Objects();
                                        await LoadObjectsAsync();
                                    }
                                }
                            }  
                        }

                        break;
                    }
                }
            }
        }
      
        private async Task FinalizeObjectCreation(SqlCommand command, ObjectDialog form, string dimensionId, string measureId, string detailId, string guid)
        {
            command.CommandText = $@"INSERT INTO ReporterObjects (System_id, ObjectName, ReporterDimension_id, ReporterMeasure_id, ReporterDetail_id, ReporterClass_id, IsNumeric, [GUID], ReporterLayer_id)
        VALUES ({MainWindowViewModel.System_id}, '{form.ReturnObjectName.Replace("'", "''")}', {dimensionId}, {measureId}, {detailId}, {form.ReporterClass_id}, {(form.ReturnIsNumeric ? 1 : 0)}, '{guid}', {MainWindowViewModel.ReporterLayer_id})";
            await command.ExecuteNonQueryAsync();
        }

        private async Task FinalizeObjectCreationPG(NpgsqlCommand command, ObjectDialog form, string dimensionId, string measureId, string detailId, string guid)
        {
            // ����������������� ������ ��� PostgreSQL
            command.CommandText = @"
        INSERT INTO ReporterObjects (System_id, ObjectName, ReporterDimension_id, ReporterMeasure_id, ReporterDetail_id, ReporterClass_id, IsNumeric, ""GUID"", ReporterLayer_id)
        VALUES (@System_id, @ObjectName, @ReporterDimension_id, @ReporterMeasure_id, @ReporterDetail_id, @ReporterClass_id, @IsNumeric, @GUID, @ReporterLayer_id)";

            // ���������� ����������
            command.Parameters.AddWithValue("@System_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
            command.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(form.ReturnObjectName.Replace("'", "''")));  // ������ ������� ��� ������ �� SQL ��������
            command.Parameters.AddWithValue("@ReporterDimension_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(dimensionId == "NULL" ? (object)DBNull.Value : dimensionId));
            command.Parameters.AddWithValue("@ReporterMeasure_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(measureId == "NULL" ? (object)DBNull.Value : measureId));
            command.Parameters.AddWithValue("@ReporterDetail_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(detailId == "NULL" ? (object)DBNull.Value : detailId));
            command.Parameters.AddWithValue("@ReporterClass_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(form.ReporterClass_id));
            command.Parameters.AddWithValue("@IsNumeric", NpgsqlTypes.NpgsqlDbType.Boolean, Convert.ToBoolean(form.ReturnIsNumeric ? 1 : 0));
            command.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(guid));
            command.Parameters.AddWithValue("@ReporterLayer_id", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

            // ���������� �������
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// ������ ������ �������
        /// </summary>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (gridObj.SelectedItem is ReporterObject selectedItem)
            {
                // ���������� ��������� �������
                selectedObject =  selectedItem;
            }

            if (treeObj.SelectedItem is HeaderNode || treeObj.SelectedItem is ReporterNode)
            {
                treeObj.SelectedItem = null; // ���������� ���������
            }

            // ���������, ���� ���� ��������� �� HeaderNode, �������� ���
            if (treeObj.SelectedItem is ReporterObject selectedItemtr)
            {
                // ���������� ��������� �������
                selectedObject = selectedItemtr;
            }

        }

        /// <summary>
        /// ������� ��������� ����
        /// </summary>
        private void TreeObj_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is not TreeView treeView) return;

            var hit = e.Source as Visual;

            while (hit != null)
            {
                if (hit is Border border && border.DataContext is HeaderNode)
                {
                    border.Background = Brushes.Transparent;
                    border.BorderBrush = Brushes.Transparent;
                    return;
                }
                hit = hit.GetVisualParent(); // ���������� Avalonia API ��� ��������� ��������
            }
        }


        /// <summary>
        /// ������� ��������� �������
        /// </summary>
        private async void EditObjectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (treeObj.SelectedItems.Count != 0)
            {
                if(MainWindowViewModel.dbms == DBMS.MS)
                {
                    using (var dbConnection = new Connect())
                    {
                        SqlConnection connection = dbConnection.Cnn;
                        SqlCommand ThisCommand = new SqlCommand();
                        ThisCommand.Connection = connection;

                        SqlDataReader ThisReader;
                        String TempValue = "", SelectStatement = "", WhereStatement = "", ReporterTable_id = "";
                        int ObjectType = -1;
                        Boolean IsFloat = false, IsTransactionFinished = true;
                        String ThisGUID = await GetGUIDAsync(), Dimension_id = "NULL", Measure_id = "NULL", Detail_id = "NULL", AssociatedColumn = "";
                        SqlTransaction ThisTransaction;


                        ThisCommand.CommandText = "SELECT TOP 1 ReporterTable_id = coalesce(b.ReporterTable_id, c.ReporterTable_id, e.ReporterTable_id) FROM ReporterObjects as a with (nolock) LEFT JOIN ReporterDimensions as b with (nolock) ON a.ReporterDimension_id = b.tid LEFT JOIN ReporterMeasures as c with (nolock) ON a.ReporterMeasure_id = c.tid LEFT JOIN ReporterDetails as d with (nolock) ON a.ReporterDetail_id = d.tid LEFT JOIN ReporterDimensions as e with (nolock) ON d.ReporterDimension_id = e.tid WHERE a.tid = " + selectedObject.Object_id.ToString() + " AND a.ReporterLayer_id = " + MainWindowViewModel.ReporterLayer_id.ToString();


                        ThisReader = ThisCommand.ExecuteReader();
                        while (ThisReader.Read()) ReporterTable_id = ThisReader.GetValue(0).ToString();
                        ThisReader.Close();

                        ObjectDialog myForm = new ObjectDialog(ReporterTable_id);
                        BindValues ThisValue;

                        // ��������� �������
                        myForm.SetComboPosition(ReporterTable_id, myForm.ReporterTables, 4);

                        // ��������� ������
                        myForm.SetComboPosition(selectedObject.ReporterClass_id.ToString(), myForm.ReporterClasses, 1);

                        // ��������� ������ ������� � ������� ������� ��� ���������
                        if (selectedObject.ReporterDimension_id != -1)
                        {
                            ThisCommand.CommandText = "SELECT SelectStatement, WhereStatement, AssociatedColumn FROM ReporterDimensions with (nolock) WHERE tid = " + selectedObject.ReporterDimension_id.ToString();
                            ThisReader = ThisCommand.ExecuteReader();
                            while (ThisReader.Read())
                            {
                                if (ThisReader.IsDBNull(0)) SelectStatement = ""; else SelectStatement = ThisReader.GetValue(0).ToString();
                                if (ThisReader.IsDBNull(1)) WhereStatement = ""; else WhereStatement = ThisReader.GetValue(1).ToString();
                                IsFloat = false;
                                AssociatedColumn = ThisReader.GetValue(2).ToString();
                            }
                            ThisReader.Close();

                            ObjectType = 0;
                        }

                        // ��������� ������� �������������
                        if (selectedObject.ReporterMeasure_id != -1)
                        {
                            ThisCommand.CommandText = "SELECT ReporterAggregate_id, SelectStatement, WhereStatement, IsFloat, AssociatedColumn FROM ReporterMeasures with (nolock) WHERE tid = " + selectedObject.ReporterMeasure_id.ToString();
                            ThisReader = ThisCommand.ExecuteReader();
                            while (ThisReader.Read())
                            {
                                TempValue = ThisReader.GetValue(0).ToString();
                                if (ThisReader.IsDBNull(1)) SelectStatement = ""; else SelectStatement = ThisReader.GetValue(1).ToString();
                                if (ThisReader.IsDBNull(2)) WhereStatement = ""; else WhereStatement = ThisReader.GetValue(2).ToString();
                                if (ThisReader.IsDBNull(3)) IsFloat = false;
                                else
                                {
                                    if (ThisReader.GetValue(3).ToString() == "1") IsFloat = true;
                                    else IsFloat = false;
                                }
                                AssociatedColumn = ThisReader.GetValue(4).ToString();
                            }
                            ThisReader.Close();

                            myForm.SetComboPosition(TempValue, myForm.AggregateFunctions, 2);

                            ObjectType = 1;
                        }

                        // ��������� ���������
                        if (selectedObject.ReporterDetail_id != -1)
                        {
                            ThisCommand.CommandText = "SELECT ReporterDimension_id, SelectStatement, WhereStatement, AssociatedColumn FROM ReporterDetails with (nolock) WHERE tid = " + selectedObject.ReporterDetail_id;
                            ThisReader = ThisCommand.ExecuteReader();
                            while (ThisReader.Read())
                            {
                                TempValue = ThisReader.GetValue(0).ToString();
                                if (ThisReader.IsDBNull(1)) SelectStatement = ""; else SelectStatement = ThisReader.GetValue(1).ToString();
                                if (ThisReader.IsDBNull(2)) WhereStatement = ""; else WhereStatement = ThisReader.GetValue(2).ToString();
                                IsFloat = false;
                                AssociatedColumn = ThisReader.GetValue(3).ToString();
                            }
                            ThisReader.Close();

                            myForm.SetComboPosition(TempValue, myForm.ReporterDimensions, 3);
                            ObjectType = 2;
                        }

                        // ��������� �������� �� ������� ��� �������
                        myForm.FindControl<TabControl>("TabControl").SelectedIndex = ObjectType;

                        // ��������� ����� �������
                        myForm.FindControl<TextBox>("ObjectName").Text = selectedObject.ObjectName;

                        // ��������� ������ �������
                        myForm.FindControl<TextBox>("SelectStatement").Text = SelectStatement;

                        // ��������� ������� �������
                        myForm.FindControl<TextBox>("WhereStatement").Text = WhereStatement;

                        // ��������� ����� �������� ���������
                        myForm.SetFloatValue(IsFloat);

                        // ��������� ����� �������� �������������
                        myForm.FindControl<CheckBox>("IsNumeric").IsChecked = selectedObject.IsNumeric;

                        myForm.IsUpdate = true;
                        await myForm.ShowDialog(this);

                        if (myForm.IsReturnValue)
                        {
                            // ���� ������ ��� ��������� ������ ����
                            if (ObjectType == myForm.ObjectType)
                            {
                                // ���������� ������ ���������
                                if (ObjectType == 0)
                                {
                                    ThisCommand.CommandText = "UPDATE ReporterDimensions SET ReporterClass_id = " + myForm.ReporterClass_id + ", ReporterTable_id = " + myForm.ReturnTable_id + ", DimensionName = '" + myForm.ReturnObjectName.Replace("'", "''") + "', SelectStatement = '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', WhereStatement = '" + myForm.ReturnWhereStatement.Replace("'", "''") + "' WHERE tid = " + selectedObject.ReporterDimension_id.ToString();
                                }
                                else if (ObjectType == 1)
                                {
                                    ThisCommand.CommandText = "UPDATE ReporterMeasures SET ReporterClass_id = " + myForm.ReporterClass_id + ", ReporterTable_id = " + myForm.ReturnTable_id + ", MeasureName = '" + myForm.ReturnObjectName.Replace("'", "''") + "', SelectStatement = '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', WhereStatement = '" + myForm.ReturnWhereStatement.Replace("'", "''") + "', ReporterAggregate_id = " + myForm.ReporterAggregate_id + " WHERE tid = " + selectedObject.ReporterMeasure_id.ToString();
                                }
                                else if (ObjectType == 2)
                                {
                                    ThisCommand.CommandText = "UPDATE ReporterDetails SET ReporterClass_id = " + myForm.ReporterClass_id + ", ReporterDimension_id = " + myForm.ReporterDimension_id + ", DetailName = '" + myForm.ReturnObjectName.Replace("'", "''") + "', SelectStatement = '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', WhereStatement = '" + myForm.ReturnWhereStatement.Replace("'", "''") + "' WHERE tid = " + selectedObject.ReporterDetail_id.ToString();
                                }
                                ThisCommand.ExecuteNonQuery();
                                ThisCommand.CommandText = "UPDATE ReporterObjects SET ObjectName = '" + myForm.ReturnObjectName.Replace("'", "''") + "', ReporterClass_id = " + myForm.ReporterClass_id + " WHERE tid = " + selectedObject.Object_id.ToString();
                                ThisCommand.ExecuteNonQuery();

                                DropData_Objects();
                                await LoadObjectsAsync();
                            }
                            // ���� ������ ��� ���������� �� ������ ����
                            else if (ObjectType != myForm.ObjectType)
                            {
                                ThisTransaction = connection.BeginTransaction();
                                ThisCommand.Transaction = ThisTransaction;
                                IsTransactionFinished = true;

                                try
                                {
                                    // ��� 0. ��������� ������
                                    ThisCommand.CommandText = "UPDATE ReporterObjects SET ReporterDimension_id = NULL, ReporterMeasure_id = NULL, ReporterDetail_id = NULL WHERE tid = " + selectedObject.Object_id.ToString();
                                    ThisCommand.ExecuteNonQuery();

                                    // ��� 1. �������� ������� ����
                                    if (ObjectType == 0)
                                    {
                                        ThisCommand.CommandText = "DELETE FROM ReporterDimensions WHERE tid = " + selectedObject.ReporterDimension_id.ToString();
                                    }
                                    else if (ObjectType == 1)
                                    {
                                        ThisCommand.CommandText = "DELETE FROM ReporterMeasures WHERE tid = " + selectedObject.ReporterMeasure_id.ToString();
                                    }
                                    else if (ObjectType == 2)
                                    {
                                        ThisCommand.CommandText = "DELETE FROM ReporterDetails WHERE tid = " + selectedObject.ReporterDetail_id.ToString();
                                    }
                                    ThisCommand.ExecuteNonQuery();

                                    // ��� 2. �������� ������ ����
                                    if (myForm.ObjectType == 0)
                                    {
                                        ThisCommand.CommandText = "INSERT INTO ReporterDimensions (System_id, ReporterClass_id, ReporterTable_id, DimensionName, AssociatedColumn, SelectStatement, WhereStatement, [GUID]) VALUES (" + myForm.ReporterClass_id + ", " + MainWindowViewModel.System_id + ", " + ReporterTable_id + ", '" + myForm.ReturnObjectName.Replace("'", "''") + "', '" + AssociatedColumn + "', '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', '" + myForm.ReturnWhereStatement.Replace("'", "''") + "', '" + ThisGUID + "')";
                                        ThisCommand.ExecuteNonQuery();
                                        ThisCommand.CommandText = "SELECT TOP 1 tid FROM ReporterDimensions with (nolock) WHERE [GUID] = '" + ThisGUID + "'";
                                        ThisReader = ThisCommand.ExecuteReader();
                                        while (ThisReader.Read()) Dimension_id = ThisReader.GetValue(0).ToString();
                                        ThisReader.Close();
                                        Measure_id = "NULL";
                                        Detail_id = "NULL";
                                    }
                                    else if (myForm.ObjectType == 1)
                                    {
                                        if (myForm.ReturnIsFloat) TempValue = "1"; else TempValue = "0";
                                        ThisCommand.CommandText = "INSERT INTO ReporterMeasures (System_id, ReporterClass_id, ReporterTable_id, MeasureName, AssociatedColumn, SelectStatement, WhereStatement, ReporterAggregate_id, IsFloat, [GUID]) VALUES (" + myForm.ReporterClass_id + ", " + MainWindowViewModel.System_id + ", " + ReporterTable_id + ", '" + myForm.ReturnObjectName.Replace("'", "''") + "', '" + AssociatedColumn + "', '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', '" + myForm.ReturnWhereStatement.Replace("'", "''") + "', " + myForm.ReporterAggregate_id + ", '" + TempValue + "', '" + ThisGUID + "')";
                                        ThisCommand.ExecuteNonQuery();
                                        ThisCommand.CommandText = "SELECT TOP 1 tid FROM ReporterMeasures with (nolock) WHERE [GUID] = '" + ThisGUID + "'";
                                        ThisReader = ThisCommand.ExecuteReader();
                                        while (ThisReader.Read()) Measure_id = ThisReader.GetValue(0).ToString();
                                        ThisReader.Close();
                                        Dimension_id = "NULL";
                                        Detail_id = "NULL";
                                    }
                                    else if (myForm.ObjectType == 2)
                                    {
                                        ThisCommand.CommandText = "INSERT INTO ReporterDetails (System_id, ReporterClass_id, ReporterDimension_id, DetailName, AssociatedColumn, SelectStatement, WhereStatement, [GUID]) VALUES (" + myForm.ReporterClass_id + ", " + MainWindowViewModel.System_id + ", " + myForm.ReporterDimension_id + ", '" + myForm.ReturnObjectName.Replace("'", "''") + "', '" + AssociatedColumn + "', '" + myForm.ReturnSelectStatement.Replace("'", "''") + "', '" + myForm.ReturnWhereStatement.Replace("'", "''") + "', '" + ThisGUID + "')";
                                        ThisCommand.ExecuteNonQuery();
                                        ThisCommand.CommandText = "SELECT TOP 1 tid FROM ReporterDetails with (nolock) WHERE [GUID] = '" + ThisGUID + "'";
                                        ThisReader = ThisCommand.ExecuteReader();
                                        while (ThisReader.Read()) Detail_id = ThisReader.GetValue(0).ToString();
                                        ThisReader.Close();
                                        Dimension_id = "NULL";
                                        Measure_id = "NULL";
                                    }

                                    if (myForm.ReturnIsNumeric) TempValue = "1"; else TempValue = "0";
                                    ThisCommand.CommandText = "UPDATE ReporterObjects SET ObjectName = '" + myForm.ReturnObjectName.Replace("'", "''") + "', ReporterDimension_id = " + Dimension_id + ", ReporterMeasure_id = " + Measure_id + ", ReporterDetail_id = " + Detail_id + ", ReporterClass_id = " + myForm.ReporterClass_id + ", IsNumeric = " + TempValue + " WHERE tid = " + selectedObject.Object_id.ToString();
                                    ThisCommand.ExecuteNonQuery();
                                    ThisTransaction.Commit();
                                }
                                catch
                                {
                                    IsTransactionFinished = false;
                                    ThisTransaction.Rollback();
                                    MessageDialog.Show("������", "������������� ���������! ������� ������ � �������� �����");
                                }

                                if (IsTransactionFinished)
                                {
                                    DropData_Objects();
                                    await LoadObjectsAsync();
                                }
                            }
                        }

                    }
                }
                else if (MainWindowViewModel.dbms == DBMS.PG)
                {
                    using (var dbConnection = new ConnectPG()) // ����������� ����� ��� ����� ConnectPG ��� PostgreSQL
                    {
                        NpgsqlConnection connection = dbConnection.Cnn;
                        NpgsqlCommand ThisCommand = new NpgsqlCommand();
                        ThisCommand.Connection = connection;

                        NpgsqlDataReader ThisReader;
                        string TempValue = "", SelectStatement = "", WhereStatement = "", ReporterTable_id = "";
                        int ObjectType = -1;
                        bool IsFloat = false, IsTransactionFinished = true;
                        string ThisGUID = await GetGUIDAsync(), Dimension_id = "NULL", Measure_id = "NULL", Detail_id = "NULL", AssociatedColumn = "";
                        NpgsqlTransaction ThisTransaction;

                        // ��� 1: �������� ReporterTable_id
                        ThisCommand.CommandText = @"
        SELECT COALESCE(b.ReporterTable_id, c.ReporterTable_id, e.ReporterTable_id) 
        FROM ReporterObjects AS a
        LEFT JOIN ReporterDimensions AS b ON a.ReporterDimension_id = b.tid
        LEFT JOIN ReporterMeasures AS c ON a.ReporterMeasure_id = c.tid
        LEFT JOIN ReporterDetails AS d ON a.ReporterDetail_id = d.tid
        LEFT JOIN ReporterDimensions AS e ON d.ReporterDimension_id = e.tid
        WHERE a.tid = @ObjectId AND a.ReporterLayer_id = @LayerId";

                        ThisCommand.Parameters.AddWithValue("@ObjectId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.Object_id));
                        ThisCommand.Parameters.AddWithValue("@LayerId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.ReporterLayer_id));

                        ThisReader = await ThisCommand.ExecuteReaderAsync();
                        while (await ThisReader.ReadAsync()) ReporterTable_id = ThisReader.GetValue(0).ToString();
                        await ThisReader.CloseAsync();

                        ObjectDialog myForm = new ObjectDialog(ReporterTable_id);
                        BindValues ThisValue;

                        // ��������� �������
                        myForm.SetComboPosition(ReporterTable_id, myForm.ReporterTables, 4);

                        // ��������� ������
                        myForm.SetComboPosition(selectedObject.ReporterClass_id.ToString(), myForm.ReporterClasses, 1);

                        // ��������� ������ ������� � ������� ������� ��� ���������
                        if (selectedObject.ReporterDimension_id != -1)
                        {
                            ThisCommand.CommandText = @"
            SELECT SelectStatement, WhereStatement, AssociatedColumn 
            FROM ReporterDimensions 
            WHERE tid = @DimensionId";

                            ThisCommand.Parameters.AddWithValue("@DimensionId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterDimension_id));

                            ThisReader = await ThisCommand.ExecuteReaderAsync();
                            while (await ThisReader.ReadAsync())
                            {
                                SelectStatement = ThisReader.IsDBNull(0) ? "" : ThisReader.GetValue(0).ToString();
                                WhereStatement = ThisReader.IsDBNull(1) ? "" : ThisReader.GetValue(1).ToString();
                                AssociatedColumn = ThisReader.GetValue(2).ToString();
                                IsFloat = false;
                            }
                            await ThisReader.CloseAsync();

                            ObjectType = 0;
                        }

                        // ��������� ������� �������������
                        if (selectedObject.ReporterMeasure_id != -1)
                        {
                            ThisCommand.CommandText = @"
            SELECT ReporterAggregate_id, SelectStatement, WhereStatement, IsFloat, AssociatedColumn 
            FROM ReporterMeasures 
            WHERE tid = @MeasureId";

                            ThisCommand.Parameters.AddWithValue("@MeasureId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterMeasure_id));

                            ThisReader = await ThisCommand.ExecuteReaderAsync();
                            while (await ThisReader.ReadAsync())
                            {
                                TempValue = ThisReader.GetValue(0).ToString();
                                SelectStatement = ThisReader.IsDBNull(1) ? "" : ThisReader.GetValue(1).ToString();
                                WhereStatement = ThisReader.IsDBNull(2) ? "" : ThisReader.GetValue(2).ToString();
                                IsFloat = ThisReader.IsDBNull(3) ? false : ThisReader.GetValue(3).ToString() == "1";
                                AssociatedColumn = ThisReader.GetValue(4).ToString();
                            }
                            await ThisReader.CloseAsync();

                            myForm.SetComboPosition(TempValue, myForm.AggregateFunctions, 2);

                            ObjectType = 1;
                        }

                        // ��������� ���������
                        if (selectedObject.ReporterDetail_id != -1)
                        {
                            ThisCommand.CommandText = @"
            SELECT ReporterDimension_id, SelectStatement, WhereStatement, AssociatedColumn 
            FROM ReporterDetails 
            WHERE tid = @DetailId";

                            ThisCommand.Parameters.AddWithValue("@DetailId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterDetail_id));

                            ThisReader = await ThisCommand.ExecuteReaderAsync();
                            while (await ThisReader.ReadAsync())
                            {
                                TempValue = ThisReader.GetValue(0).ToString();
                                SelectStatement = ThisReader.IsDBNull(1) ? "" : ThisReader.GetValue(1).ToString();
                                WhereStatement = ThisReader.IsDBNull(2) ? "" : ThisReader.GetValue(2).ToString();
                                AssociatedColumn = ThisReader.GetValue(3).ToString();
                            }
                            await ThisReader.CloseAsync();

                            myForm.SetComboPosition(TempValue, myForm.ReporterDimensions, 3);
                            ObjectType = 2;
                        }

                        // ��������� �������� �� ������� ��� �������
                        myForm.FindControl<TabControl>("TabControl").SelectedIndex = ObjectType;

                        // ��������� ����� �������
                        myForm.FindControl<TextBox>("ObjectName").Text = selectedObject.ObjectName;

                        // ��������� ������ �������
                        myForm.FindControl<TextBox>("SelectStatement").Text = SelectStatement;

                        // ��������� ������� �������
                        myForm.FindControl<TextBox>("WhereStatement").Text = WhereStatement;

                        // ��������� ����� �������� ���������
                        myForm.SetFloatValue(IsFloat);

                        // ��������� ����� �������� �������������
                        myForm.FindControl<CheckBox>("IsNumeric").IsChecked = selectedObject.IsNumeric;

                        myForm.IsUpdate = true;
                        await myForm.ShowDialog(this);

                        if (myForm.IsReturnValue)
                        {
                            // ���� ������ ��� ��������� ������ ����
                            if (ObjectType == myForm.ObjectType)
                            {
                                // ���������� ������ ���������
                                if (ObjectType == 0)
                                {
                                    ThisCommand.CommandText = @"
                    UPDATE ReporterDimensions 
                    SET ReporterClass_id = @ReporterClassId, 
                        ReporterTable_id = @TableId, 
                        DimensionName = @ObjectName, 
                        SelectStatement = @SelectStatement, 
                        WhereStatement = @WhereStatement 
                    WHERE tid = @DimensionId";

                                    ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                    ThisCommand.Parameters.AddWithValue("@TableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReturnTable_id));
                                    ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@DimensionId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterDimension_id));
                                }
                                else if (ObjectType == 1)
                                {
                                    ThisCommand.CommandText = @"
                    UPDATE ReporterMeasures 
                    SET ReporterClass_id = @ReporterClassId, 
                        ReporterTable_id = @TableId, 
                        MeasureName = @ObjectName, 
                        SelectStatement = @SelectStatement, 
                        WhereStatement = @WhereStatement, 
                        ReporterAggregate_id = @AggregateId 
                    WHERE tid = @MeasureId";

                                    ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                    ThisCommand.Parameters.AddWithValue("@TableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReturnTable_id));
                                    ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@AggregateId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterAggregate_id));
                                    ThisCommand.Parameters.AddWithValue("@MeasureId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterMeasure_id));
                                }
                                else if (ObjectType == 2)
                                {
                                    ThisCommand.CommandText = @"
                    UPDATE ReporterDetails 
                    SET ReporterClass_id = @ReporterClassId, 
                        ReporterDimension_id = @ReporterDimensionId, 
                        DetailName = @ObjectName, 
                        SelectStatement = @SelectStatement, 
                        WhereStatement = @WhereStatement 
                    WHERE tid = @DetailId";

                                    ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                    ThisCommand.Parameters.AddWithValue("@ReporterDimensionId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterDimension_id));
                                    ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@DetailId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterDetail_id));
                                }
                                await ThisCommand.ExecuteNonQueryAsync();

                                ThisCommand.CommandText = @"
                UPDATE ReporterObjects 
                SET ObjectName = @ObjectName, 
                    ReporterClass_id = @ReporterClassId 
                WHERE tid = @ObjectId";

                                ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                ThisCommand.Parameters.AddWithValue("@ObjectId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.Object_id));

                                await ThisCommand.ExecuteNonQueryAsync();
                                DropData_Objects();
                                await LoadObjectsAsync();
                            }
                            // ��������� ������� � ��������� �����...
                            else if (ObjectType != myForm.ObjectType)
                            {
                                ThisTransaction = connection.BeginTransaction();
                                ThisCommand.Transaction = ThisTransaction;
                                IsTransactionFinished = true;

                                try
                                {
                                    // ��� 0. ��������� ������ �� ������ ����
                                    ThisCommand.CommandText = @"
            UPDATE ReporterObjects 
            SET ReporterDimension_id = NULL, ReporterMeasure_id = NULL, ReporterDetail_id = NULL 
            WHERE tid = @ObjectId";
                                    ThisCommand.Parameters.AddWithValue("@ObjectId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.Object_id));
                                    await ThisCommand.ExecuteNonQueryAsync();

                                    // ��� 1. �������� ������� ����
                                    if (ObjectType == 0) // ���� ��� ��� ReporterDimension
                                    {
                                        ThisCommand.CommandText = @"
                DELETE FROM ReporterDimensions 
                WHERE tid = @DimensionId";
                                        ThisCommand.Parameters.AddWithValue("@DimensionId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterDimension_id));
                                    }
                                    else if (ObjectType == 1) // ���� ��� ��� ReporterMeasure
                                    {
                                        ThisCommand.CommandText = @"
                DELETE FROM ReporterMeasures 
                WHERE tid = @MeasureId";
                                        ThisCommand.Parameters.AddWithValue("@MeasureId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterMeasure_id));
                                    }
                                    else if (ObjectType == 2) // ���� ��� ��� ReporterDetail
                                    {
                                        ThisCommand.CommandText = @"
                DELETE FROM ReporterDetails 
                WHERE tid = @DetailId";
                                        ThisCommand.Parameters.AddWithValue("@DetailId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.ReporterDetail_id));
                                    }
                                    await ThisCommand.ExecuteNonQueryAsync();

                                    // ��� 2. �������� ������ ����
                                    if (myForm.ObjectType == 0) // ���� ����� ��� - ReporterDimension
                                    {
                                        ThisCommand.CommandText = @"
                INSERT INTO ReporterDimensions (System_id, ReporterClass_id, ReporterTable_id, DimensionName, AssociatedColumn, SelectStatement, WhereStatement, [GUID]) 
                VALUES (@SystemId, @ReporterClassId, @TableId, @ObjectName, @AssociatedColumn, @SelectStatement, @WhereStatement, @GUID)";

                                        ThisCommand.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                        ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                        ThisCommand.Parameters.AddWithValue("@TableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(ReporterTable_id));
                                        ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@AssociatedColumn", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(AssociatedColumn));
                                        ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(ThisGUID));

                                        await ThisCommand.ExecuteNonQueryAsync();
                                        ThisCommand.CommandText = "SELECT tid FROM ReporterDimensions WHERE [GUID] = @GUID";
                                        NpgsqlDataReader reader = await ThisCommand.ExecuteReaderAsync();
                                        if (await reader.ReadAsync())
                                        {
                                            Dimension_id = reader.GetValue(0).ToString();
                                        }
                                        await reader.CloseAsync();
                                    }
                                    else if (myForm.ObjectType == 1) // ���� ����� ��� - ReporterMeasure
                                    {
                                        string isFloat = myForm.ReturnIsFloat ? "1" : "0";
                                        ThisCommand.CommandText = @"
                INSERT INTO ReporterMeasures (System_id, ReporterClass_id, ReporterTable_id, MeasureName, AssociatedColumn, SelectStatement, WhereStatement, ReporterAggregate_id, IsFloat, [GUID]) 
                VALUES (@SystemId, @ReporterClassId, @TableId, @ObjectName, @AssociatedColumn, @SelectStatement, @WhereStatement, @AggregateId, @IsFloat, @GUID)";
                                        ThisCommand.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                        ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                        ThisCommand.Parameters.AddWithValue("@TableId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(ReporterTable_id));
                                        ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@AssociatedColumn", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(AssociatedColumn));
                                        ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@AggregateId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterAggregate_id));
                                        ThisCommand.Parameters.AddWithValue("@IsFloat", NpgsqlTypes.NpgsqlDbType.Boolean, Convert.ToBoolean(isFloat));
                                        ThisCommand.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(ThisGUID));

                                        await ThisCommand.ExecuteNonQueryAsync();
                                        ThisCommand.CommandText = "SELECT tid FROM ReporterMeasures WHERE [GUID] = @GUID";
                                        NpgsqlDataReader reader = await ThisCommand.ExecuteReaderAsync();
                                        if (await reader.ReadAsync())
                                        {
                                            Measure_id = reader.GetValue(0).ToString();
                                        }
                                        await reader.CloseAsync();
                                    }
                                    else if (myForm.ObjectType == 2) // ���� ����� ��� - ReporterDetail
                                    {
                                        ThisCommand.CommandText = @"
                INSERT INTO ReporterDetails (System_id, ReporterClass_id, ReporterDimension_id, DetailName, AssociatedColumn, SelectStatement, WhereStatement, [GUID]) 
                VALUES (@SystemId, @ReporterClassId, @DimensionId, @ObjectName, @AssociatedColumn, @SelectStatement, @WhereStatement, @GUID)";

                                        ThisCommand.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));
                                        ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                        ThisCommand.Parameters.AddWithValue("@DimensionId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterDimension_id));
                                        ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@AssociatedColumn", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(AssociatedColumn));
                                        ThisCommand.Parameters.AddWithValue("@SelectStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnSelectStatement.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@WhereStatement", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnWhereStatement.Replace("'", "''")));
                                        ThisCommand.Parameters.AddWithValue("@GUID", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.Parse(ThisGUID));

                                        await ThisCommand.ExecuteNonQueryAsync();
                                        ThisCommand.CommandText = "SELECT tid FROM ReporterDetails WHERE [GUID] = @GUID";
                                        NpgsqlDataReader reader = await ThisCommand.ExecuteReaderAsync();
                                        if (await reader.ReadAsync())
                                        {
                                            Detail_id = reader.GetValue(0).ToString();
                                        }
                                        await reader.CloseAsync();
                                    }

                                    // ���������� ��������� �������
                                    ThisCommand.CommandText = @"
            UPDATE ReporterObjects 
            SET ObjectName = @ObjectName, 
                ReporterDimension_id = @DimensionId, 
                ReporterMeasure_id = @MeasureId, 
                ReporterDetail_id = @DetailId, 
                ReporterClass_id = @ReporterClassId, 
                IsNumeric = @IsNumeric 
            WHERE tid = @ObjectId";

                                    ThisCommand.Parameters.AddWithValue("@ObjectName", NpgsqlTypes.NpgsqlDbType.Text, Convert.ToString(myForm.ReturnObjectName.Replace("'", "''")));
                                    ThisCommand.Parameters.AddWithValue("@DimensionId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(Dimension_id));
                                    ThisCommand.Parameters.AddWithValue("@MeasureId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(Measure_id));
                                    ThisCommand.Parameters.AddWithValue("@DetailId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(Detail_id));
                                    ThisCommand.Parameters.AddWithValue("@ReporterClassId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(myForm.ReporterClass_id));
                                    ThisCommand.Parameters.AddWithValue("@IsNumeric", NpgsqlTypes.NpgsqlDbType.Boolean, Convert.ToBoolean(myForm.ReturnIsNumeric ? "1" : "0"));
                                    ThisCommand.Parameters.AddWithValue("@ObjectId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.Object_id));

                                    await ThisCommand.ExecuteNonQueryAsync();

                                    ThisTransaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    IsTransactionFinished = false;
                                    ThisTransaction.Rollback();
                                    MessageDialog.Show("������", "������������� ���������! ������� ������ � �������� �����");
                                }

                                if (IsTransactionFinished)
                                {
                                    DropData_Objects();
                                    await LoadObjectsAsync();
                                }
                            }
                        }
                    }
                }
                
            }
            else
            {
                MessageDialog.Show("������","�� �� ������� ������ ��� ��������������");
            }
        }

        /// <summary>
        /// ������� �������� �������
        /// </summary>
        private async void DeleteObjectButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await ShowMessageDialogAsync("������� ��������� ������?", "�������� �������");
            if (result == true) // `true` for OK, `false` for Cancel
            {
                if (treeObj.SelectedItem != null)
                {
                    if(MainWindowViewModel.dbms == DBMS.MS)
                    {
                        using (var dbConnection = new Connect())
                        {
                            SqlConnection connection = dbConnection.Cnn;

                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    using (var command = new SqlCommand())
                                    {
                                        command.Connection = connection;
                                        command.Transaction = transaction;

                                        // Update ReporterObjects
                                        command.CommandText = "UPDATE ReporterObjects SET ReporterDimension_id = NULL, ReporterMeasure_id = NULL, ReporterDetail_id = NULL WHERE tid = @ObjectId";
                                        command.Parameters.AddWithValue("@ObjectId", selectedObject.Object_id);
                                        await command.ExecuteNonQueryAsync();

                                        // Delete related dimensions
                                        command.CommandText = "DELETE FROM ReporterDimensions WHERE tid IN (SELECT ReporterDimension_id FROM ReporterObjects WITH (nolock) WHERE tid = @ObjectId)";
                                        await command.ExecuteNonQueryAsync();

                                        // Delete related measures
                                        command.CommandText = "DELETE FROM ReporterMeasures WHERE tid IN (SELECT ReporterMeasure_id FROM ReporterObjects WITH (nolock) WHERE tid = @ObjectId)";
                                        await command.ExecuteNonQueryAsync();

                                        // Delete related details
                                        command.CommandText = "DELETE FROM ReporterDetails WHERE tid IN (SELECT ReporterDetail_id FROM ReporterObjects WITH (nolock) WHERE tid = @ObjectId)";
                                        await command.ExecuteNonQueryAsync();

                                        // Delete the main object
                                        command.CommandText = "DELETE FROM ReporterObjects WHERE tid = @ObjectId";
                                        await command.ExecuteNonQueryAsync();
                                    }

                                    await transaction.CommitAsync();

                                    // Refresh the DataGrid
                                    DropData_Objects();
                                    await LoadObjectsAsync();
                                }
                                catch (Exception ex)
                                {
                                    await transaction.RollbackAsync();
                                    MessageDialog.Show("������", "���������� ������� ������, ��� ��� �� ��� ������������\n" + ex.Message);
                                }
                            }
                        }
                    }
                    else if (MainWindowViewModel.dbms == DBMS.PG)
                    {
                        using (var dbConnection = new ConnectPG()) // ���������� ��� ����� ����������� ��� PostgreSQL
                        {
                            NpgsqlConnection connection = dbConnection.Cnn;

                            using (var transaction = await connection.BeginTransactionAsync())
                            {
                                try
                                {
                                    using (var command = new NpgsqlCommand())
                                    {
                                        command.Connection = connection;
                                        command.Transaction = transaction;

                                        // Update ReporterObjects
                                        command.CommandText = "UPDATE ReporterObjects SET ReporterDimension_id = NULL, ReporterMeasure_id = NULL, ReporterDetail_id = NULL WHERE tid = @ObjectId";
                                        command.Parameters.AddWithValue("@ObjectId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(selectedObject.Object_id));
                                        await command.ExecuteNonQueryAsync();

                                        // Delete related dimensions
                                        command.CommandText = "DELETE FROM ReporterDimensions WHERE tid IN (SELECT ReporterDimension_id FROM ReporterObjects WHERE tid = @ObjectId)";
                                        await command.ExecuteNonQueryAsync();

                                        // Delete related measures
                                        command.CommandText = "DELETE FROM ReporterMeasures WHERE tid IN (SELECT ReporterMeasure_id FROM ReporterObjects WHERE tid = @ObjectId)";
                                        await command.ExecuteNonQueryAsync();

                                        // Delete related details
                                        command.CommandText = "DELETE FROM ReporterDetails WHERE tid IN (SELECT ReporterDetail_id FROM ReporterObjects WHERE tid = @ObjectId)";
                                        await command.ExecuteNonQueryAsync();

                                        // Delete the main object
                                        command.CommandText = "DELETE FROM ReporterObjects WHERE tid = @ObjectId";
                                        await command.ExecuteNonQueryAsync();
                                    }

                                    await transaction.CommitAsync();

                                    // Refresh the DataGrid
                                    DropData_Objects();
                                    await LoadObjectsAsync();
                                }
                                catch (Exception ex)
                                {
                                    await transaction.RollbackAsync();
                                    MessageDialog.Show("������", "���������� ������� ������, ��� ��� �� ��� ������������\n" + ex.Message);
                                }
                            }
                        }
                    }
                    
                }
            }
        }

        /// <summary>
        /// �����������������
        /// </summary>
        public void DropData_Objects()
        {
            gridObjects.ItemsSource = null;
            treeObj.ItemsSource = null;
            DataListBox.Items.Clear();
         
            if (MainWindowViewModel.ReporterClasses != null) MainWindowViewModel.ReporterClasses.Clear();
            if (MainWindowViewModel.ObjectList != null) MainWindowViewModel.ObjectList.Clear();
        }

        /// <summary>
        /// �����
        /// </summary>
        public void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) lifetime.Shutdown();
        }

        /// <summary>
        /// ��������� �����������
        /// </summary>
        public async void Param_Click(object sender, RoutedEventArgs e)
        {
            // ���������� �������� ����
            this.IsEnabled = false;

            // �������� ������� ����
            var connectionParam = new ConnectionParam();

            // �������� �������� ������� ����
            await connectionParam.ShowDialog(this);

            // ������������� �������� ����
            this.IsEnabled = true;
        }

        /// <summary>
        /// ����������� � ��
        /// </summary>
        public async void Connect_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;

            // �������� ������� ����
            var modalLoad = new ModalLoad();

            // �������� �������� ������� ����
            await modalLoad.ShowDialog(this);

            if(MainWindowViewModel.conClose == true)
            {
                Settings.SettingUpdate();
                LoadLayers();
                await InitializeAsync();
            }
          
            // ������������� �������� ����
            this.IsEnabled = true;
        }

        /// <summary>
        /// ���������� ������ ��� ������ ������
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.P && e.KeyModifiers == KeyModifiers.Control)
            {
                e.Handled = true; // ��������, ��� ������� ���� ����������

                // ��������� ���� �� ������
                connectMenu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            if (e.Key == Key.Q && e.KeyModifiers == KeyModifiers.Control)
            {
                e.Handled = true; // ��������, ��� ������� ���� ����������

                // ��������� ���� �� ������
                exitMenu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
            {
                e.Handled = true; // ��������, ��� ������� ���� ����������

                // ��������� ���� �� ������
                connectBD.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }


        }

    }
}