using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.Exporting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.Exporting
{
    public class ExportService : IExportService
    {
        private IUserService _userService;
        private IHtmlTableConverter _htmlTableConverter;
        private IPdfService _pdfService;
        private IXlsService _xlsService;

        public ExportService(IUserService userService,
            IHtmlTableConverter htmlTableConverter,
            IPdfService pdfService,
            IXlsService xlsService)
        {
            _userService = userService;
            _htmlTableConverter = htmlTableConverter;
            _pdfService = pdfService;
            _xlsService = xlsService;
        }

        public async Task<byte[]> ExportUsersTable(ExportFormat format, OrderingRequestModel<UserTableColumn, SortingDirection> order)
        {
            var users = await GetUsers(order);
            byte[] resultBytes = null;

            switch (format)
            {
                case ExportFormat.Pdf:
                    var html = await _htmlTableConverter.CreateHtmlTable("wwwroot/HtmlContentTemplates/HtmlLayout.html", "CONTENT", users.ToList(), "Users list");
                    resultBytes = await _pdfService.GetPdfFromHtml(html, DocumentOrientation.Horizontal);
                    break;
                case ExportFormat.Xls:
                    resultBytes = await _xlsService.GetXlsList(users, "Users list");
                    break;
            }

            return resultBytes;
        }

        private async Task<List<UserTableRowResponseModel>> GetUsers(OrderingRequestModel<UserTableColumn, SortingDirection> order)
        {
            var usersRequestModel = new PaginationRequestModel<UserTableColumn>()
            {
                Search = null,
                Order = order,
                Limit = int.MaxValue,
                Offset = 0
            };

            return _userService.GetAll(usersRequestModel).Data;
        }
    }
}
