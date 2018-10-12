using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PICI0025_Reporting_Service
{
    public class ThreadWorker
    {

        char[] SPLITTER = new char[] { '-' };
        DataInterface _DataInterface;
        Config _Config;

        string GetCleanColumnName(string name)
        {
            if (_Config.ColumnCleanerMap.ContainsKey(name))
                return _Config.ColumnCleanerMap[name].ToString();
            return name;
        }

        ThreadWorker() { }

        public ThreadWorker(Config config)
        {
            _DataInterface = new DataInterface(config.ConnectionString);
            _Config = config;
        }

        public class LogInfoEventArgs : EventArgs
        {
            public enum LogInfoType { Success, Informational, ERROR }
            public LogInfoType Type { get; set; }
            public string Message { get; set; }
            public bool SendEmail { get; set; }

        }

        public event EventHandler<LogInfoEventArgs> LogEvent;

        public void DoWork()
        {


            DataTable table = _DataInterface.GetPendingReportDeliveries();
            if (table.Rows.Count != 0)
            {
                LogEvent?.Invoke(this, new LogInfoEventArgs()
                {
                    Message = "DoWork called with " + table.Rows.Count.ToString() + " entries",
                    Type = LogInfoEventArgs.LogInfoType.Informational,
                    SendEmail = false
                });

                foreach (DataRow row in table.Rows)
                {
                    string accession_no = row["accession_no"].ToString();
                    int acc_report_id = Convert.ToInt32(row["acc_report_id"]);
                    string guid = row["PDF_GUID"].ToString();
                    string med_rec_no = row["med_rec_no"].ToString();
                    string barcode = row["barcode"].ToString();
                    string directory_prefix = row["directory_prefix"].ToString();
                    DateTime lock_date = Convert.ToDateTime(row["lock_date"]);
                    string copyFrom = Path.Combine(_Config.BaseDirectoryInput, directory_prefix, guid + ".pdf");
                    string[] pieces = barcode.Split(SPLITTER);
                    string facility_code = pieces[2];
                    string facility = _Config.FacilityMap[facility_code].ToString();
                    string PSN = pieces[3];
                    string filename = "PICI0025-" + facility_code + "-" + PSN + " " + accession_no + " " + lock_date.ToString("MM-dd-yyyy hh-mm tt");
                    string copyToNonredacted = Path.Combine(_Config.BaseDirectoryOutput, facility, filename + ".pdf");
                    string copyToRedacted = Path.Combine(_Config.BaseDirectoryRedacted, filename + "_redacted.pdf");
                    if (!File.Exists(copyFrom))
                    {
                        LogEvent?.Invoke(this, new LogInfoEventArgs()
                        {
                            Message = "Unable to find expected file " + copyFrom,
                            Type = LogInfoEventArgs.LogInfoType.ERROR,
                            SendEmail = true
                        });
                    }

                    else
                    {
                        File.Copy(copyFrom, copyToNonredacted, true);
                        var images = OncoSeek.Core.PDFHelper.RenderImagesFromPDF(copyToNonredacted, 200);
                        bool FirstPage = true;
                        foreach (var image in images)
                        {
                            OncoSeek.Core.ImageHelper.RedactImage(image, 0.58f, (FirstPage) ? 0.115f : 0.105f, 0.38f, 0.045f);
                            FirstPage = false;
                        }
                        OncoSeek.Core.PDFHelper.RenderImagesToPDF(images, copyToRedacted);
                        LogEvent?.Invoke(this, new LogInfoEventArgs()
                        {
                            Message = "Successfully generated report " + filename,
                            Type = LogInfoEventArgs.LogInfoType.Success,
                            SendEmail = true
                        });
                        _DataInterface.MarkReportAsDelivered(acc_report_id);
                    }
                }

                DataTable tableTrialData = _DataInterface.GetTrialData();
                Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
                HashSet<string> columns = new HashSet<string>();
                char[] singleQuoteRemover = new char[] { '\'' };
                char[] doubleQuoteRemover = new char[] { '"' };
                string[] lineSplitter = new string[] { "\r\n" };
                char[] colonSpliter = new char[] { ':' };

                foreach (DataRow row in tableTrialData.Rows)

                {
                    string unique = row["AccessionNumber"].ToString();
                    Dictionary<string, string> current = new Dictionary<string, string>();
                    results[unique] = current;
                    foreach (DataColumn column in tableTrialData.Columns)
                    {
                        if (column.ColumnName != "WorksheetData")
                        {
                            current[column.ColumnName] = row[column].ToString();
                            if (!columns.Contains(column.ColumnName))
                                columns.Add(column.ColumnName);
                        }
                        else
                        {
                            //Worksheet data follows an attribute-value model and could change over time
                            string worksheetData = row[column].ToString().TrimEnd(doubleQuoteRemover).TrimStart(doubleQuoteRemover);
                            string[] cols = worksheetData.Split(lineSplitter, StringSplitOptions.None);
                            foreach (string col in cols)
                            {
                                int index = col.IndexOf(':');
                                string attribute = col.Substring(0, index);
                                string value = col.Substring(index + 1).TrimStart(singleQuoteRemover).TrimEnd(singleQuoteRemover);
                                current[attribute] = value;
                                if (!columns.Contains(attribute))
                                    columns.Add(attribute);
                            }
                        }
                    }
                }

                char tab = '\t';
                StringBuilder sb = new StringBuilder(100 * 1024);
                bool First = true;
                foreach (string columnKey in columns)
                {
                    if (!First)
                        sb.Append(tab);
                    sb.Append(GetCleanColumnName(columnKey));
                    First = false;
                }
                sb.Append(System.Environment.NewLine);

                foreach (string id in results.Keys)
                {
                    First = true;
                    foreach (string columnKey in columns)
                    {
                        if (!First)
                            sb.Append(tab);
                        if (results[id].ContainsKey(columnKey))
                            sb.Append(results[id][columnKey]);
                        First = false;
                    }
                    sb.Append(System.Environment.NewLine);
                }

                string resultTSV = sb.ToString();
                File.WriteAllText(_Config.ResultsFilePath, resultTSV);
            }
        }

    }
}

