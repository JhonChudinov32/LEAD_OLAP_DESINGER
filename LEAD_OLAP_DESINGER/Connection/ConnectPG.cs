using LEAD_OLAP_DESINGER.MsgBox;
using LEAD_OLAP_DESINGER.ViewModels;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAD_OLAP_DESINGER.Connection
{
    public class ConnectPG : IDisposable
    {
        public NpgsqlConnection Cnn { get; private set; }

        public ConnectPG()
        {
            Cnn = CreateConnection();
            if (Cnn == null)
            {
                Dispose();
                throw new InvalidOperationException("Не удалось установить соединение с базой данных.");
            }
        }

        private NpgsqlConnection CreateConnection()
        {
            try
            {
                var connection = new NpgsqlConnection
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
