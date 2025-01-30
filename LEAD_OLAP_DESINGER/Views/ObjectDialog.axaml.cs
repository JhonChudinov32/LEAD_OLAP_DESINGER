using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Data.SqlClient;
using LEAD_OLAP_DESINGER.MsgBox;
using LEAD_OLAP_DESINGER.ViewModels;
using LEAD_OLAP_DESINGER.Connection;
using System;
using LEAD_OLAP_DESINGER.Models;
using static LEAD_OLAP_DESINGER.Models.DBParameters;
using Npgsql;

namespace LEAD_OLAP_DESINGER;

public partial class ObjectDialog : Window
{
    public bool IsReturnValue, IsUpdate = false;

    public int ObjectType;

    // Возвращаемые значения
    public string ReporterClass_id, ReporterAggregate_id, ReporterDimension_id, ReturnTable_id;
    public string ReturnObjectName, ReturnSelectStatement, ReturnWhereStatement;
    public bool ReturnIsNumeric, ReturnIsFloat;

    public List<BindValues> ReporterClasses, AggregateFunctions, ReporterDimensions, ReporterTables;

    // Конструктор
    public ObjectDialog(string ReporterTable_id)
    {
        InitializeComponent();
        ReporterClasses = new List<BindValues>();
        AggregateFunctions = new List<BindValues>();
        ReporterDimensions = new List<BindValues>();
        ReporterTables = new List<BindValues>();

        BindValues ThisValue;
        int ThisIndex;
        bool HasValues;
        int SelectIndex = 0;
        try
        {
            if (MainWindowViewModel.dbms == DBMS.MS)
            {
                using (var dbConnection = new Connect())
                {
                    // Доступ к соединению
                    SqlConnection connection = dbConnection.Cnn;


                    // Заполнение списка классов

                    HasValues = false;

                    string commandStringClasses = "SELECT tid, ClassName FROM ReporterClasses with (nolock) WHERE System_id = @SystemId";
                    // Выполнение операций с базой данных
                    using (var command = new SqlCommand(commandStringClasses, connection))
                    {
                        command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ThisIndex = ClassCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                ReporterClasses.Add(ThisValue);
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    // Заполнение списка таблиц
                    if (HasValues) ClassCombo.SelectedIndex = 0;

                    string commandStringTables = "SELECT tid, TableAlias FROM ReporterTables with (nolock) WHERE System_id = @SystemId";

                    using (var command = new SqlCommand(commandStringTables, connection))
                    {
                        command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);


                        using (var reader = command.ExecuteReader())
                        {
                            HasValues = false;

                            TablesCombo.Items.Clear();


                            while (reader.Read())
                            {
                                ThisIndex = TablesCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                ReporterTables.Add(ThisValue);
                                if (reader.GetValue(0).ToString() == ReporterTable_id) SelectIndex = ThisIndex;
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    if (HasValues) TablesCombo.SelectedIndex = SelectIndex;

                    // Заполнение функций агрегирования
                    HasValues = false;

                    string commandStringAggregate = "SELECT tid, AggregateName FROM ReporterAggregates with (nolock) ORDER BY AggregateName ASC";
                    using (var command = new SqlCommand(commandStringAggregate, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            HasValues = false;

                            AggregatesCombo.Items.Clear();


                            while (reader.Read())
                            {
                                ThisIndex = AggregatesCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                AggregateFunctions.Add(ThisValue);
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    if (HasValues) AggregatesCombo.SelectedIndex = 0;

                    // Заполнение списка измерений
                    HasValues = false;

                    string commandStringDimensions = "SELECT tid, DimensionName FROM ReporterDimensions with (nolock) ORDER BY DimensionName asc";
                    using (var command = new SqlCommand(commandStringDimensions, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            HasValues = false;

                            DimensionsCombo.Items.Clear();


                            while (reader.Read())
                            {
                                ThisIndex = DimensionsCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                ReporterDimensions.Add(ThisValue);
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    if (HasValues) DimensionsCombo.SelectedIndex = 0;
                }
            }
            else if (MainWindowViewModel.dbms == DBMS.PG)
            {
                using (var dbConnection = new ConnectPG())
                {
                    // Доступ к соединению с PostgreSQL
                    NpgsqlConnection connection = dbConnection.Cnn;

                    // Заполнение списка классов
                    HasValues = false;

                    string commandStringClasses = "SELECT tid, classname FROM reporterclasses WHERE system_id = @SystemId";

                    // Выполнение операций с базой данных
                    using (var command = new NpgsqlCommand(commandStringClasses, connection))
                    {
                        command.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ThisIndex = ClassCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                ReporterClasses.Add(ThisValue);
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    // Заполнение списка таблиц
                    if (HasValues) ClassCombo.SelectedIndex = 0;

                    string commandStringTables = "SELECT tid, tablealias FROM reportertables WHERE system_id = @SystemId";

                    using (var command = new NpgsqlCommand(commandStringTables, connection))
                    {
                        command.Parameters.AddWithValue("@SystemId", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(MainWindowViewModel.System_id));

                        using (var reader = command.ExecuteReader())
                        {
                            HasValues = false;

                            TablesCombo.Items.Clear();

                            while (reader.Read())
                            {
                                ThisIndex = TablesCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                ReporterTables.Add(ThisValue);
                                if (reader.GetValue(0).ToString() == ReporterTable_id) SelectIndex = ThisIndex;
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    if (HasValues) TablesCombo.SelectedIndex = SelectIndex;

                    // Заполнение функций агрегирования
                    HasValues = false;

                    string commandStringAggregate = "SELECT tid, aggregatename FROM reporteraggregates ORDER BY aggregatename ASC";
                    using (var command = new NpgsqlCommand(commandStringAggregate, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            HasValues = false;

                            AggregatesCombo.Items.Clear();

                            while (reader.Read())
                            {
                                ThisIndex = AggregatesCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                AggregateFunctions.Add(ThisValue);
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    if (HasValues) AggregatesCombo.SelectedIndex = 0;

                    // Заполнение списка измерений
                    HasValues = false;

                    string commandStringDimensions = "SELECT tid, dimensionname FROM reporterdimensions ORDER BY dimensionname ASC";
                    using (var command = new NpgsqlCommand(commandStringDimensions, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            HasValues = false;

                            DimensionsCombo.Items.Clear();

                            while (reader.Read())
                            {
                                ThisIndex = DimensionsCombo.Items.Add(reader.GetString(1));

                                ThisValue = new BindValues();
                                ThisValue.Identifier = reader.GetValue(0).ToString();
                                ThisValue.Index = ThisIndex;

                                ReporterDimensions.Add(ThisValue);
                                HasValues = true;
                            }
                            reader.Close();
                        }
                    }

                    if (HasValues) DimensionsCombo.SelectedIndex = 0;
                }
            }
           
        }
        catch (Exception ex)
        {
            MessageDialog.Show("", ex.Message);
        }
    }

    // Установить значение для флага IsFloat
   

    public void SetFloatValue(bool FloatValue)
    {
        IsFloat.IsChecked = FloatValue;
    }

    // Установить позицию в комбобоксах
    public void SetComboPosition(string Identifier, List<BindValues> ThisList, int ComboNumber)
    {
        BindValues ThisValue;
        for (int i = 0; i < ThisList.Count; i++)
        {
            ThisValue = ThisList[i];
            if (ThisValue.Identifier == Identifier)
            {
                if (ComboNumber == 1)
                {
                    ClassCombo.SelectedIndex = ThisValue.Index;
                }
                else if (ComboNumber == 2)
                {
                    AggregatesCombo.SelectedIndex = ThisValue.Index;
                }
                else if (ComboNumber == 3)
                {
                    DimensionsCombo.SelectedIndex = ThisValue.Index;
                }
                else if (ComboNumber == 4)
                {
                    TablesCombo.SelectedIndex = ThisValue.Index;
                }
                break;
            }
        }
    }

    // Обработчик для кнопки "Отмена"
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsReturnValue = false;
        Close();
    }

    // Обработчик для кнопки "Ок"
    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        IsReturnValue = true;

        bool IsContinue = true;

        if (ReporterClasses.Count == 0)
        {
            IsContinue = false;
            MessageDialog.Show("Ошибка", "Нет данных о классах");
        }

        if ((AggregateFunctions.Count == 0) && (TabControl.SelectedIndex == 1))
        {
            IsContinue = false;
            MessageDialog.Show("Ошибка", "Нет данных о функциях агрегирования");
        }

        if ((ReporterDimensions.Count == 0) && (TabControl.SelectedIndex == 2))
        {
            IsContinue = false;
            MessageDialog.Show("Ошибка", "Нет данных о детализируемых измерениях");
        }

        if (ReporterTables.Count == 0)
        {
            IsContinue = false;
            MessageDialog.Show("Ошибка", "Нет данных о доступных таблицах");
        }

        if (ObjectName.Text.Trim().Length < 2)
        {
            IsContinue = false;
            MessageDialog.Show("Ошибка", "Название объекта не может быть менее 2 символов");
        }

        if ((IsContinue) && (!IsUpdate))
        {
            using (var dbConnection = new Connect())
            {
                // Доступ к соединению
                SqlConnection connection = dbConnection.Cnn;

                string commandString = "SELECT cnt = count(*) FROM ReporterObjects with (nolock) WHERE ObjectName = @ObjectName AND System_id = @System_id";

                // Выполнение операций с базой данных
                using (var command = new SqlCommand(commandString, connection))
                {
                    command.Parameters.AddWithValue("@ObjectName", ObjectName.Text);
                    command.Parameters.AddWithValue("@SystemId", MainWindowViewModel.System_id);

                    int ThisCounter = 0;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ThisCounter = reader.GetInt32(0);
                        }
                        reader.Close();
                    }
                    if (ThisCounter > 0)
                    {
                        IsContinue = false;
                        MessageDialog.Show("Ошибка", "Объект с таким именем был определен ранее");
                    }
                }
            }

        }

        if (SelectStatement.Text.Trim().Length < 2)
        {
            IsContinue = false;
            MessageDialog.Show("Ошибка", "Текст выборки не может быть менее 2 символов");
        }

        BindValues ThisValue;

        if (IsContinue)
        {
            for (int s = 0; s < ReporterClasses.Count; s++)
            {
                ThisValue = ReporterClasses[s];
                if (ThisValue.Index == ClassCombo.SelectedIndex) ReporterClass_id = ThisValue.Identifier;
            }

            for (int s = 0; s < ReporterTables.Count; s++)
            {
                ThisValue = ReporterTables[s];
                if (ThisValue.Index == TablesCombo.SelectedIndex) ReturnTable_id = ThisValue.Identifier;
            }

            for (int s = 0; s < AggregateFunctions.Count; s++)
            {
                ThisValue = AggregateFunctions[s];
                if (ThisValue.Index == AggregatesCombo.SelectedIndex) ReporterAggregate_id = ThisValue.Identifier;
            }

            for (int s = 0; s < ReporterDimensions.Count; s++)
            {
                ThisValue = ReporterDimensions[s];
                if (ThisValue.Index == DimensionsCombo.SelectedIndex) ReporterDimension_id = ThisValue.Identifier;
            }

            ObjectType = TabControl.SelectedIndex;
            ReturnObjectName = ObjectName.Text;
            ReturnSelectStatement = SelectStatement.Text;
            ReturnWhereStatement = WhereStatement.Text;
            ReturnIsNumeric = (bool)IsNumeric.IsChecked;
            ReturnIsFloat = (bool)IsFloat.IsChecked;
            Close();
        }
        else
        {
            IsReturnValue = false;
        }
    }
}