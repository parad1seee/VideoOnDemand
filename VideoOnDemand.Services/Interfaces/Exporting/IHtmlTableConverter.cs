using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.Exporting
{
    public interface IHtmlTableConverter
    {
        /// <summary>
        /// Get html table with list of objects
        /// </summary>
        /// <param name="htmlFilePath">Path to html template</param>
        /// <param name="tablePlaceholcerName">Content placeholder in html template</param>
        /// <param name="objects">List of objects</param>
        /// <param name="caption">Table caption</param>
        /// <returns>Html string</returns>
        Task<string> CreateHtmlTable<T>(string htmlFilePath, string tablePlaceholcerName, List<T> objects, string caption = null);
    }
}
