using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Services.Interfaces.Exporting;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.Exporting
{
    public class PdfService : IPdfService
    {
        // get pdf document from html string
        public async Task<byte[]> GetPdfFromHtml(string html, DocumentOrientation orientation = DocumentOrientation.Vertical)
        {
            byte[] resultPdf = null;

            using (var htmlSource = new MemoryStream(Encoding.UTF8.GetBytes(html)))
            using (var pdfStream = new MemoryStream())
            {
                PdfDocument pdfDocument = new PdfDocument(new PdfWriter(pdfStream));
                ConverterProperties converterProperties = new ConverterProperties();

                // change orientation if horizontal
                if (orientation == DocumentOrientation.Horizontal)
                    pdfDocument.SetDefaultPageSize(PageSize.A4.Rotate());

                HtmlConverter.ConvertToPdf(htmlSource, pdfDocument, converterProperties);

                resultPdf = pdfStream.ToArray();
            }

            return resultPdf;
        }
    }
}
