using System.Data;

namespace SALLY_API.Reports
{
    internal class OutdatedBadgeFirmwareReport : Report
    {
        DataSet dataSet;

        public OutdatedBadgeFirmwareReport(DataSet dataset, string name = "outdatedBadgesFirmwareReport")
        {
            this.name = name;
            this.dataSet = dataset;
            report = GenerateReport();
        }

       

        public override Stream GenerateReport()
        {
            return ReportHelper.GenerateExcelReportFromDataset(dataSet);
            
        }
    }
    internal class OutdatedBadgeFirmwareReportByDepartment: Report
    {
        DataSet dataSet;

        public OutdatedBadgeFirmwareReportByDepartment(DataSet dataset, string name = "outdatedBadgesFirmwareReportByDepartment")
        {
            this.name = name;
            this.dataSet = dataset;
            report = GenerateReport();
        }



        public override Stream GenerateReport()
        {
            return ReportHelper.GenerateExcelReportFromDataset(dataSet);

        }

    }
}
