using VideoOnDemand.PdfGenerator.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VideoOnDemand.PdfGenerator.Implementations
{
    public class HtmlToPdfConverter: IHtmlToPdfConverter
    {
        /// <summary>
        /// Source: https://github.com/wkhtmltopdf/wkhtmltopdf/releases
        /// </summary>
        private string _executableFilePath = null;

        public HtmlToPdfConverter(string executableFilePath)
        {
            _executableFilePath = executableFilePath;
        }

        public byte[] ConvertHtmlToPdf(string html)
        {                    
            var startInfo = new ProcessStartInfo
            {
                FileName = _executableFilePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = "-q -n --disable-smart-shrinking - -"
            };

            MemoryStream pdf = new MemoryStream();
            StreamReader streamReader = new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(html)));

            int exitCode = 0;

            using (var process = Process.Start(startInfo))
            {
                StreamWriter stdin = process.StandardInput;
                stdin.AutoFlush = true;
                stdin.Write(streamReader.ReadToEnd());
                stdin.Dispose();

                process.StandardOutput.BaseStream.CopyTo(pdf);
                process.StandardOutput.Close();
                pdf.Position = 0;

                process.WaitForExit(5000);

                exitCode = process.ExitCode;
            }                     

            return pdf.ToArray();
        }
    }
}
