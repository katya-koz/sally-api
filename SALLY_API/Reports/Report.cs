
namespace SALLY_API.Reports
{
    public abstract class Report : IDisposable
    {
        public Stream report;
        public string name; // report name

        public async Task<string> DownloadReport(string fileDownloadLocation)
        {

            using (Stream contentStream = report,
               fileStream = new FileStream(
                   $"{fileDownloadLocation}\\{name}_{DateTime.Now:yyyy-MM-dd--hh-mm}.xlsx",
                   FileMode.Create,
                   FileAccess.Write,
                   FileShare.None))
            {
                await contentStream.CopyToAsync(fileStream);
            }


            return fileDownloadLocation;
        }

        

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        public abstract Stream GenerateReport();
    }
}
