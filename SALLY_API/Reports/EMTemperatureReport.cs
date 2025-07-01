using System.Data;
using System.Text.RegularExpressions;
using Quartz;

namespace SALLY_API.Reports
{
    internal class EMTemperatureReport : Report
    {
        DataSet dataSet;
        string department;

        public EMTemperatureReport(DataSet dataset, string department, string name = "EMTemperatureReport")
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            this.name = name + $"_{rgx.Replace(department,"").Replace(" ", "-")}";
            this.dataSet = dataset;
            report = GenerateReport();
            this.department = department;
        }

        public EMTemperatureReport(EMTemperatureReport otherReport)
        {
            this.name = new string(otherReport.name);
            this.dataSet = otherReport.dataSet.Copy();
            report = GenerateReport();
            this.department = new string(otherReport.department);
        }

        public override Stream GenerateReport()
        {
            return ReportHelper.GenerateExcelReportFromDataset(dataSet);

        }

    }

}
