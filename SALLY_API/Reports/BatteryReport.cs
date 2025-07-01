using System.Data;
using System.Text.RegularExpressions;

namespace SALLY_API.Reports
{
    public class BatteryReport:Report 
    {
        DataSet dataSet;

        public BatteryReport(DataSet dataset, string name = "Battery Summary")
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            this.name = name;
            dataSet = dataset;
            report = GenerateReport();
        }

        public BatteryReport(BatteryReport otherReport)
        {
            name = new string(otherReport.name);
            dataSet = otherReport.dataSet.Copy();
            report = GenerateReport();
        }
        public override Stream GenerateReport()
        {
            return ReportHelper.GenerateExcelReportFromDataset(dataSet);

        }

        


    }
}
