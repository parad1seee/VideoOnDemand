using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Base.CursorPagination;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Base.CursorPagination;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces
{
    public interface IUserService
    {
        PaginationResponseModel<UserTableRowResponseModel> GetAll(PaginationRequestModel<UserTableColumn> model, bool getAdmins = false);

        CursorPaginationBaseResponseModel<UserTableRowResponseModel> GetAll(CursorPaginationRequestModel<UserTableColumn> model, bool getAdmins = false);

        Task<UserResponseModel> SwitchUserActiveState(int id);

        UserResponseModel SoftDeleteUser(int id);

        void HardDeleteUser(int id);

        Task<UserResponseModel> EditProfileAsync(int id, UserProfileRequestModel model);

        Task<UserResponseModel> GetProfileAsync(int id);

        UserResponseModel DeleteAvatar(int userId);

        Task<UserDeviceResponseModel> SetDeviceToken(DeviceTokenRequestModel model, int userId);
    }
}
