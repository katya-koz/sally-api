using System;
using System.Data;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart; // Add this line
using ClosedXML.Excel;

namespace SALLY_API.Reports
{
    internal class ReportHelper
    {
        internal static MemoryStream GenerateExcelReportFromDataset(DataSet data)
        {
            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                foreach (DataTable table in data.Tables)
                {
                    var worksheet = workbook.Worksheets.Add(table.TableName);
                    worksheet.Cell(1, 1).InsertTable(table);
                    worksheet.Columns().AdjustToContents();
                }
                workbook.SaveAs(stream);

            }
            stream.Position = 0;
            return stream;
        }

        public static MemoryStream GenerateExcelReportWithChart(DataSet data)
        {
            // First, generate an Excel file with ClosedXML (or any library) and load data into worksheets.
            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                foreach (DataTable table in data.Tables)
                {
                    var ws = workbook.Worksheets.Add(table.TableName);
                    ws.Cell(1, 1).InsertTable(table);
                    ws.Columns().AdjustToContents();
                    ws.Name = data.Tables[0].TableName;
                }
                workbook.SaveAs(stream);
            }
            stream.Position = 0;
            // Now add the chart using EPPlus
            stream= AddBarChartToExcel(stream);
            return stream;
        }

        private static MemoryStream AddBarChartToExcel(MemoryStream stream)
        {
            // Load or create the Excel package from the provided stream.
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(stream))

            {
                // Get the first worksheet or create one if none exists.
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                // If the worksheet is empty, add sample data.

                // Determine the last row with data.
                Console.WriteLine("part 1");
                int lastRow = worksheet.Dimension.End.Row;

                // Create a clustered bar chart.
                var chart = worksheet.Drawings.AddChart("BarChart", eChartType.BarClustered);
                chart.Title.Text = "Sample Bar Chart";
                Console.WriteLine("Part 2");
                // Set the chart position (row, rowOffset, column, columnOffset) and size (in pixels).
                chart.SetPosition(1, 0, 3, 0); // Position chart at row 2, column D
                chart.SetSize(800, 600);
                Console.WriteLine("Part 3");

                // Define the series:
                // - The first parameter is the range with the Y-axis values (from column B).
                // - The second parameter is the range with the X-axis category labels (from column A).
                var dataRange = worksheet.Cells[$"B2:B{lastRow}"];
                var categoryRange = worksheet.Cells[$"A2:A{lastRow}"];
                chart.Series.Add(dataRange, categoryRange);
                chart.Title.Text = "Compliance Rate by Department";
                Console.WriteLine("Part 3");

                // Save the changes back to the stream.
                package.Save();
                Console.WriteLine("Part 4");

                var updatedBytes = package.GetAsByteArray();
                Console.WriteLine("Part 5");

                return new MemoryStream(updatedBytes);

            }
            stream.Position = 0;

            return stream; 

        }
    }
}
