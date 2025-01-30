using LEAD_OLAP_DESINGER.MsgBox;
using LEAD_OLAP_DESINGER.ViewModels;
using System;
using System.Data.SqlClient;

namespace LEAD_OLAP_DESINGER.Connection
{
    public class Connect : IDisposable
    {
        public SqlConnection Cnn { get; private set; }

        public Connect()
        {
            Cnn = CreateConnection();
            if (Cnn == null)
            {
                Dispose();
                throw new InvalidOperationException("Не удалось установить соединение с базой данных.");
            }
        }

        private SqlConnection CreateConnection()
        {
            try
            {
                var connection = new SqlConnection
                {
                    ConnectionString = MainWindowViewModel.ConnectString
                };
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                MessageDialog.Show("Ошибка подключения", ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            if (Cnn != null)
            {
                Cnn.Dispose();
                Cnn = null;
            }
        }
    }
}
