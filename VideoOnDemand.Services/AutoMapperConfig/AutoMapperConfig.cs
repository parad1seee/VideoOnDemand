using VideoOnDemand.Common.Extensions;
using VideoOnDemand.Domain.Entities;
using VideoOnDemand.Domain.Entities.Chat;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Chat;
using VideoOnDemand.Models.ResponseModels.Session;
using Profile = VideoOnDemand.Domain.Entities.Identity.Profile;

namespace VideoOnDemand.Services.StartApp
{
    public class AutoMapperProfileConfiguration : AutoMapper.Profile
    {
        public AutoMapperProfileConfiguration()
        : this("MyProfile")
        {
        }

        protected AutoMapperProfileConfiguration(string profileName)
        : base(profileName)
        {
            CreateMap<Image, ImageResponseModel>();

            CreateMap<UserDevice, UserDeviceResponseModel>()
                .ForMember(t => t.AddedAt, opt => opt.MapFrom(src => src.AddedAt.ToISO()));

            #region User model

            CreateMap<UserProfileRequestModel, Profile>()
                .ForMember(t => t.Id, opt => opt.Ignore())
                .ForMember(t => t.User, opt => opt.Ignore());

            CreateMap<Profile, UserResponseModel>()
                .ForMember(t => t.Avatar, opt => opt.MapFrom(x => x.Avatar))
                .ForMember(t => t.Email, opt => opt.MapFrom(x => x.User != null ? x.User.Email : ""))
                .ForMember(t => t.PhoneNumber, opt => opt.MapFrom(x => x.User != null ? x.User.PhoneNumber : ""))
                .ForMember(t => t.IsBlocked, opt => opt.MapFrom(x => x.User != null ? !x.User.IsActive : false));

            CreateMap<ApplicationUser, UserBaseResponseModel>()
               .IncludeAllDerived();

            CreateMap<ApplicationUser, UserResponseModel>()
                .ForMember(x => x.Avatar, opt => opt.MapFrom(x => x.Profile.Avatar))
                .ForMember(x => x.FirstName, opt => opt.MapFrom(x => x.Profile.FirstName))
                .ForMember(x => x.LastName, opt => opt.MapFrom(x => x.Profile.LastName))
                .ForMember(x => x.IsBlocked, opt => opt.MapFrom(x => !x.IsActive))
                .IncludeAllDerived();

            CreateMap<ApplicationUser, UserRoleResponseModel>();

            #endregion

            #region Chat

            CreateMap<Message, ChatMessageBaseResponseModel>()
                .ForMember(t => t.CreatedAt, opt => opt.MapFrom(x => x.CreatedAt.ToISO()))
                .ForMember(t => t.Image, opt => opt.MapFrom(x => x.Image));

            CreateMap<Message, ChatMessageResponseModel>()
                .ForMember(t => t.CreatedAt, opt => opt.MapFrom(x => x.CreatedAt.ToISO()))
                .ForMember(t => t.Image, opt => opt.MapFrom(x => x.Image));

            #endregion
        }
    }
}
