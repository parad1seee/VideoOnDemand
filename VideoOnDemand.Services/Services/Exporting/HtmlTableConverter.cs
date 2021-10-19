using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Services.Interfaces.Exporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.Exporting
{
    public class HtmlTableConverter : IHtmlTableConverter
    {
        private string _tableOpen = "<table>";
        private string _tableClose = "</table>";
        private string _captionOpen = "<caption>";
        private string _captionClose = "</caption>";
        private string _tHeadOpen = "<thead>";
        private string _tHeadClose = "</thead>";
        private string _tBodyOpen = "<tbody>";
        private string _tBodyClose = "</tbody>";
        private string _trOpen = "<tr>";
        private string _trClose = "</tr>";
        private string _tdOpen = "<td>";
        private string _tdClose = "</td>";

        public async Task<string> CreateHtmlTable<T>(string htmlFilePath, string tablePlaceholcerName, List<T> objects, string caption = null)
        {
            var htmlTemplate = File.ReadAllText(htmlFilePath);

            StringBuilder resultHtmlTable = new StringBuilder();

            resultHtmlTable.Append(_tableOpen);

            // add table caption
            if (!string.IsNullOrEmpty(caption))
            {
                resultHtmlTable.Append(_captionOpen)
                    .Append(caption)
                    .Append(_captionClose);
            }

            #region Create Header

            // create table header
            if (objects.Any())
            {
                resultHtmlTable.Append(_tHeadOpen).Append(_trOpen);

                // add row number
                resultHtmlTable.Append(_tdOpen).Append("#").Append(_tdClose);

                PropertyInfo[] props = objects.First().GetType().GetProperties();
                foreach (var prop in props)
                {
                    if (prop.CanRead)
                    {
                        resultHtmlTable.Append(_tdOpen)
                            .Append(prop.Name.HumanizePascalCase())
                            .Append(_tdClose);
                    }
                }

                resultHtmlTable.Append(_trClose).Append(_tHeadClose);
            }

            #endregion

            #region Create Body

            resultHtmlTable.Append(_tBodyOpen);

            // create table rows
            for (int i = 0; i < objects.Count; i++)
            {
                resultHtmlTable.Append(_trOpen);

                // add row number
                resultHtmlTable.Append(_tdOpen).Append(i + 1).Append(_tdClose);

                PropertyInfo[] props = objects.First().GetType().GetProperties();
                foreach (var prop in props)
                {
                    if (prop.CanRead)
                    {
                        resultHtmlTable.Append(_tdOpen)
                            .Append(GetFormattedValue(prop.GetValue(objects[i])))
                            .Append(_tdClose);
                    }
                }

                resultHtmlTable.Append(_trClose);
            }

            resultHtmlTable.Append(_tBodyClose);

            #endregion

            resultHtmlTable.Append(_tableClose);

            htmlTemplate = htmlTemplate.Replace($"[%{tablePlaceholcerName.ToUpper()}%]", resultHtmlTable.ToString());
            return htmlTemplate;
        }

        // format values
        private string GetFormattedValue(object val)
        {
            switch (val)
            {
                case DateTime d:
                    return ((DateTime)val).ToString("dd-MM-yyyy HH:mm");
                case bool b when b:
                    return "Yes";
                case bool b when !b:
                    return "No";
                case string s when DateTime.TryParse(s, out DateTime res):
                    return res.ToString("dd-MM-yyyy HH:mm");
            }

            return val?.ToString();
        }
    }
}
