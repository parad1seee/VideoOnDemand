using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.Exporting
{
    public interface IXlsService
    {
        /// <summary>
        /// Get xls table with list of objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="objects">List of items</param>
        /// <param name="title">Title of table</param>
        /// <returns>Xls document as bytes array</returns>
        Task<byte[]> GetXlsList<T>(List<T> objects, string title = null);
    }
}
