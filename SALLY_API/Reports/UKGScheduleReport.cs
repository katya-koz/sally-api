using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Linq;
using System.Text;

namespace SALLY_API.Reports
{
    public class UKGScheduleReport : Report
    {
        public string ReportName { get; set; }
        public DateTime GeneratedOn { get; set; }


        public string ParseXLSX(string filelocation)
        {
            var csvBuilder = new StringBuilder();
            List<UKGUsersSchedule> schedules = new List<UKGUsersSchedule>();

            using (var workbook = new XLWorkbook(filelocation))
            {
                var worksheet = workbook.Worksheets.Worksheet(1); // Use first sheet
                var rows = worksheet.RangeUsed().RowsUsed();

                foreach (var row in rows)
                {
                    var cells = row.Cells();
                    var values = new string[cells.Count()];

                    for (int i = 0; i < cells.Count(); i++)
                    {
                        values[i] = $"\"{cells.ElementAt(i).GetValue<string>().Replace("\"", "\"\"")}\"";
                    }

                    csvBuilder.AppendLine(string.Join(",", values));
                }
            }

            string csvContent = csvBuilder.ToString();
            return csvContent;

        }
    

        public void UploadReport(string csv)
        {
            string[] rows = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<UKGUsersSchedule> schedules = new List<UKGUsersSchedule>();


            foreach (string row in rows)
            {
                if (CheckRow(row))
                {
                    string[] columns = row.Split(',');

                    if (columns.Length != 4)
                    {
                        Console.WriteLine($"Skipping malformed row: {row}");
                        continue;
                    }

                    try
                    {
                        DateOnly reportDate = DateOnly.Parse(columns[0].Trim());
                        string name = columns[3].Trim();
                         (TimeOnly startTime, TimeOnly endTime) = ParseTimes(columns[1]);

                        UKGUsersSchedule schedule = new UKGUsersSchedule(reportDate, name, startTime, endTime);
                        schedules.Add(schedule);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing row: {row} - {ex.Message}");
                    }
                }
            }


            // Optional: do something with the list
            foreach (var schedule in schedules)
            {
                Console.WriteLine(schedule);
            }


        }
        private static bool CheckRow(string row)
        {
            return row.Contains(',');

        }
        private static (TimeOnly Start, TimeOnly End) ParseTimes(string value)
        {
            string[] parts = value.Split('-');
            if (parts.Length != 2)
                throw new FormatException("Invalid time range format. Expected format: HH:mm-HH:mm");

            TimeOnly start = TimeOnly.Parse(parts[0].Trim());
            TimeOnly end = TimeOnly.Parse(parts[1].Trim());

            return (start, end);
        }

        public override Stream GenerateReport()
        {

            return new MemoryStream();


        }
    }

    public class UKGUsersSchedule
    {
        public DateOnly ReportDate { get; set; }
        public string Name { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }


        public UKGUsersSchedule()
        {
            ReportDate = new DateOnly();
            Name = "";
            StartTime = new TimeOnly();
            EndTime = new TimeOnly();
        }

        // Full constructor
        public UKGUsersSchedule(DateOnly reportDate, string name, TimeOnly startTime, TimeOnly endTime)
        {
            ReportDate = reportDate;
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
        }

        // ToString override
        public override string ToString()
        {
            return $"Date: {ReportDate}, Name: {Name}, Start: {StartTime}, End: {EndTime}";
        }
    }
}
