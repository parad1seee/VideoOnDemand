using OfficeOpenXml;
using OfficeOpenXml.Style;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Services.Interfaces.Exporting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.Exporting
{
    public class XlsService : IXlsService
    {
        public async Task<byte[]> GetXlsList<T>(List<T> objects, string title = null)
        {
            byte[] response = null;

            using (var excel = new ExcelPackage())
            {
                // add new sheet
                excel.Workbook.Worksheets.Add("worksheet1");

                // get added sheet
                var worksheet = excel.Workbook.Worksheets["worksheet1"];

                PropertyInfo[] headerProps = objects.First().GetType().GetProperties();

                #region Title

                if (!string.IsNullOrEmpty(title))
                {
                    worksheet.Row(1).Height = 25;

                    // add title to first row
                    var titleCell = worksheet.Cells[$"A1:{(char)('A' + headerProps.Length)}1"];

                    // add styles (merge title cells and so on)
                    titleCell.Merge = true;
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    titleCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    titleCell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    titleCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleCell.Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
                    titleCell.Style.Font.Color.SetColor(Color.White);
                    titleCell.Value = title;
                }

                #endregion

                #region TableHeader

                var tableHeaderValues = new List<string[]> { new string[headerProps.Length + 1] };

                // add row number header
                tableHeaderValues[0][0] = "#";

                for (int i = 1; i <= headerProps.Length; i++)
                {
                    if (headerProps[i - 1].CanRead)
                        tableHeaderValues[0][i] = headerProps[i - 1].Name.HumanizePascalCase();
                }


                var tableHeader = worksheet.Cells["A2:" + Char.ConvertFromUtf32(tableHeaderValues[0].Length + 64) + "2"];

                // add table headers
                tableHeader.LoadFromArrays(tableHeaderValues);
                tableHeader.Style.Font.Bold = true;
                tableHeader.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                tableHeader.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                tableHeader.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                tableHeader.Style.Fill.PatternType = ExcelFillStyle.Solid;
                tableHeader.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                foreach (var cell in tableHeader)
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                #endregion

                #region Table data and styles

                int headersRowNumber = 2;

                for (int objIndex = 0, curRow = headersRowNumber + 1; objIndex < objects.Count; objIndex++, curRow++)
                {
                    PropertyInfo[] propsValues = objects.First().GetType().GetProperties();

                    // add row number
                    worksheet.Cells[curRow, 1].Value = objIndex + 1;

                    for (int propIndex = 0, col = 'B'; propIndex < propsValues.Length; propIndex++, col++)
                    {
                        if (propsValues[propIndex].CanRead)
                            worksheet.Cells[$"{(char)col}{curRow}"].Value = GetFormattedValue(propsValues[propIndex].GetValue(objects[objIndex]));
                    }
                }

                // set columns width (+5 - additional space)
                for (int i = 2; i < headerProps.Length + 2; i++)
                    worksheet.Column(i).AutoFit(worksheet.Cells[headersRowNumber, i, headersRowNumber + objects.Count, i].Max(x => x.Value != null ? x.Value.ToString().Length : 0) + 5);

                // add table border (+1 - for row number column)
                worksheet.Cells[headersRowNumber, 1, headersRowNumber + objects.Count, headerProps.Length + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                #endregion

                response = excel.GetAsByteArray();
            }

            return response;
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
