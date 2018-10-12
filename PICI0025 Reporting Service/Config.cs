using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Runtime;
using System.Reflection;
using System.IO;

namespace PICI0025_Reporting_Service
{
    public class Config
    {

        public string BaseDirectoryInput { get; set; }
        public string BaseDirectoryOutput { get; set; }
        public string BaseDirectoryRedacted { get; set; }
        public string ResultsFilePath { get; set; }
        public int MainLoopPeriod { get; set; }
        public string ConnectionString { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string SMTPServer { get; set; }
        public int SMTPPort { get; set; }
        public Dictionary<string, object> ColumnCleanerMap { get; set; }
        public Dictionary<string, object> FacilityMap { get; set; }


        public static Config LoadFromJSON(string JSON)
        {
            Config c = new Config();
            JavaScriptSerializer json = new JavaScriptSerializer();
            Dictionary<string, object> settings = json.Deserialize<Dictionary<string, object>>(JSON);
            if (settings.ContainsKey("BaseDirectoryInput"))
                c.BaseDirectoryInput = settings["BaseDirectoryInput"].ToString();
            if (settings.ContainsKey("BaseDirectoryOutput"))
                c.BaseDirectoryOutput = settings["BaseDirectoryOutput"].ToString();
            if (settings.ContainsKey("BaseDirectoryRedacted"))
                c.BaseDirectoryRedacted = settings["BaseDirectoryRedacted"].ToString();
            if (settings.ContainsKey("ResultsFilePath"))
                c.ResultsFilePath = settings["ResultsFilePath"].ToString();
            if (settings.ContainsKey("SMTPServer"))
                c.SMTPServer = settings["SMTPServer"].ToString();
            if (settings.ContainsKey("MainLoopPeriod"))
                c.MainLoopPeriod = Convert.ToInt32(settings["MainLoopPeriod"].ToString());
            if (settings.ContainsKey("SMTPPort"))
                c.SMTPPort = Convert.ToInt32(settings["SMTPPort"].ToString());
            if (settings.ContainsKey("ConnectionString"))
                c.ConnectionString = settings["ConnectionString"].ToString();
            if (settings.ContainsKey("ColumnCleanerMap"))
                c.ColumnCleanerMap = settings["ColumnCleanerMap"] as Dictionary<string, object>;
            if (settings.ContainsKey("FacilityMap"))
                c.FacilityMap = settings["FacilityMap"] as Dictionary<string, object>;
            return c;
        }

        public static string GetDefaultConfigJSON()
        {
            
            string filename = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "config.json");
            return File.ReadAllText(filename);
        }
        public static Config GetDefaultConfig()
        {
            return Config.LoadFromJSON(GetDefaultConfigJSON());
        }



    }
}
