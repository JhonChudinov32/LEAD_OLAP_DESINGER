using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LEAD_OLAP_DESINGER.Connection;
using LEAD_OLAP_DESINGER.Models;
using LEAD_OLAP_DESINGER.ViewModels;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using static LEAD_OLAP_DESINGER.Models.DBParameters;

namespace LEAD_OLAP_DESINGER;

public partial class JoinPropertiesDialog : Window
{
    public ArrayList ThisList;
    public Boolean IsReturnValue;
    public String Join_id, WhereStatement;
    public JoinPropertiesDialog(string joinId, string valSourceTableName, string valSourceColumnName, string valTargetTableName, string valTargetColumnName, string valJoinCondition)
    {
        InitializeComponent();
        this.IsReturnValue = false;
        ThisList = new ArrayList();

        if (MainWindowViewModel.dbms == DBMS.MS)
        {
            using (var dbConnection = new Connect())
            {
                SqlConnection connection = dbConnection.Cnn;

                string stringQuery = @"SELECT tid, JoinName, JoinFunction FROM ReporterJoins WITH (NOLOCK) ORDER BY tid ASC";

                using (var command = new SqlCommand(stringQuery, connection))
                {

                    using (var objectReader = command.ExecuteReader())
                    {
                        string thisItem, thisKey;
                        bool hasValues = false;
                        int tempIndex = -1;
                        int currentIndex = -1;

                        while (objectReader.Read())
                        {
                            hasValues = true;
                            thisItem = $"{objectReader.GetString(2)} ({objectReader.GetString(1)})";
                            thisKey = objectReader.GetValue(0).ToString();
                            tempIndex = AddItem(thisItem, thisKey);

                            if (objectReader.GetValue(0).ToString() == joinId)
                            {
                                currentIndex = tempIndex;
                            }
                        }

                        if (hasValues)
                        {
                            if (currentIndex == -1)
                            {
                                currentIndex = 0;
                            }
                            JoinsList.SelectedIndex = currentIndex;
                        }
                        objectReader.Close();
                    }


                }

            }
        }
        else if (MainWindowViewModel.dbms == DBMS.PG)
        {
            using (var dbConnection = new ConnectPG())
            {
                // ���������� � PostgreSQL
                NpgsqlConnection connection = dbConnection.Cnn;

                string stringQuery = @"SELECT tid, JoinName, JoinFunction FROM ReporterJoins ORDER BY tid ASC";

                using (var command = new NpgsqlCommand(stringQuery, connection))
                {
                    using (var objectReader = command.ExecuteReader())
                    {
                        string thisItem, thisKey;
                        bool hasValues = false;
                        int tempIndex = -1;
                        int currentIndex = -1;

                        while (objectReader.Read())
                        {
                            hasValues = true;

                            // �������������� ������ �� PostgreSQL (���� ����������)
                            thisItem = $"{objectReader.GetString(2)} ({objectReader.GetString(1)})";
                            thisKey = objectReader.GetInt32(0).ToString(); // ������������� GetInt32 ��� ��������� int

                            // ��������� � ������ (�����������, ��� AddItem �������� � ����� �������)
                            tempIndex = AddItem(thisItem, thisKey);

                            // �������� �� ������������� joinId
                            if (objectReader.GetInt32(0).ToString() == joinId)
                            {
                                currentIndex = tempIndex;
                            }
                        }

                        if (hasValues)
                        {
                            // ���� ������� ������ �� ��� ������, �������� ������ �������
                            if (currentIndex == -1)
                            {
                                currentIndex = 0;
                            }

                            // ������������� ��������� ������ � JoinsList
                            JoinsList.SelectedIndex = currentIndex;
                        }

                        objectReader.Close();
                    }
                }
            }
        }


            // ��������� �������� ��� ��������� �����
        SourceTableName.Text = valSourceTableName;
        SourceColumnName.Text = valSourceColumnName;
        TargetTableName.Text = valTargetTableName;
        TargetColumnName.Text = valTargetColumnName;
        JoinCondition.Text = valJoinCondition;
    }
    public int AddItem(string itemValue, string itemIdentifier)
    {

        JoinsList.Items.Add(itemValue);

        // ������� �������� ��������
        BindValues thisBinder = new BindValues
        {
            Index = JoinsList.Items.Count - 1, // ��������� ������
            Identifier = itemIdentifier
        };

        // ��������� ������ �������� � �������������� ���������
        return ThisList.Add(thisBinder);
       
    }

    // ���������� ������ "������"
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    // ���������� ������ "�������"
    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        this.IsReturnValue = true;
        this.WhereStatement = JoinCondition.Text;

        int SelectedIndex = JoinsList.SelectedIndex;
       
        BindValues? ThisBinder = null; // ������� BindValues nullable
        this.Join_id = "";

        for (int i = 0; i < ThisList.Count; i++)
        {
            ThisBinder = ThisList[i] as BindValues?; // ������� ������������� � BindValues?
            if (ThisBinder?.Index == SelectedIndex)
            {
                this.Join_id = ThisBinder?.Identifier;
                break;
            }
        }

        this.Close();
    }

}