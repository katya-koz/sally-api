using DocumentFormat.OpenXml.Bibliography;
using SixLabors.Fonts.Unicode;
using System.Data;
using System.Text.RegularExpressions;

namespace SALLY_API.Reports
{
    public class IPACReport : Report 
    {
        DataSet dataSet;

        public IPACReport(DataSet dataset, string name = "IPACReport")
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            this.name = name;
            dataSet = dataset;
            report = GenerateReport();
        }

        public IPACReport(IPACReport otherReport)
        {
            name = new string(otherReport.name);
            dataSet = otherReport.dataSet.Copy();
            report = GenerateReport();
        }

        public override Stream GenerateReport()
        {
            return ReportHelper.GenerateExcelReportWithChart(dataSet );

        }


    }
}
