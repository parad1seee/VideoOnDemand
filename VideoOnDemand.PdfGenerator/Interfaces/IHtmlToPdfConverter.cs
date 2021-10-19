using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.PdfGenerator.Interfaces
{
    public interface IHtmlToPdfConverter
    {
        byte[] ConvertHtmlToPdf(string html);
    }
}
