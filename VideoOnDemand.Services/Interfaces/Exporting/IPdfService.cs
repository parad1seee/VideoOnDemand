using VideoOnDemand.Models.Enums;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.Exporting
{
    public interface IPdfService
    {
        /// <summary>
        /// Generate pdf file from html string
        /// </summary>
        /// <param name="html">Html string</param>
        /// <param name="orientation">Document pages orientation</param>
        /// <returns>Pdf file as bytes array</returns>
        Task<byte[]> GetPdfFromHtml(string html, DocumentOrientation orientation = DocumentOrientation.Vertical);
    }
}
