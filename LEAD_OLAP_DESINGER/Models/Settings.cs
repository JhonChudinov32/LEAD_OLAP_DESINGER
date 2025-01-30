using LEAD_OLAP_DESINGER.ViewModels;
using System.IO;
using LEAD_OLAP_DESINGER.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace LEAD_OLAP_DESINGER.Models
{
    public static class Settings
    {
        public static void SettingUpdate()
        {
            string jsonFileName = "setting.json";
            string jsonFilePath = Path.Combine(DirectoryHelper.GetResDirectory(), "config", jsonFileName);

            string json = File.ReadAllText(jsonFilePath);

            var root = JsonConvert.DeserializeObject<JObject>(json);
            SettingItem data = root["data"].ToObject<SettingItem>();

            MainWindowViewModel.ReporterLayer_id = data.ReporterLayer_id;
            MainWindowViewModel.System_id = data.System_id;
            MainWindowViewModel.JoinWidth = data.JoinWidth;
        }
    }
}
