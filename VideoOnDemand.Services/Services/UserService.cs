using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities;
using VideoOnDemand.Domain.Entities.Chat;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.RequestModels;
using VideoOnDemand.Models.RequestModels.Base.CursorPagination;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Models.ResponseModels.Base.CursorPagination;
using VideoOnDemand.ScheduledTasks;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Profile = VideoOnDemand.Domain.Entities.Identity.Profile;

namespace VideoOnDemand.Services.Services
{
    public class UserService : IUserService
    {
        private IHttpContextAccessor _httpContextAccessor = null;
        private IUnitOfWork _unitOfWork;
        private readonly OneWeekAfterRegistration _weekAfterRegistration;
        private IImageService _imageService;
        private IMapper _mapper = null;
        private IJWTService _jwtService;

        private bool _isUserSuperAdmin = false;
        private bool _isUserAdmin = false;
        private int? _userId = null;

        public UserService(IUnitOfWork unitOfWork, IImageService imageService, IMapper mapper, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, IJWTService jwtService)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _imageService = imageService;
            _mapper = mapper;
            _jwtService = jwtService;

            var context = httpContextAccessor.HttpContext;

            if (context?.User != null)
            {
                _isUserSuperAdmin = context.User.IsInRole(Role.SuperAdmin);
                _isUserAdmin = context.User.IsInRole(Role.Admin);

                try
                {
                    _userId = context.User.GetUserId();
                }
                catch
                {
                    _userId = null;
                }
            }

            _weekAfterRegistration = serviceProvider.GetScheduledTask<OneWeekAfterRegistration>();
        }

        public PaginationResponseModel<UserTableRowResponseModel> GetAll(PaginationRequestModel<UserTableColumn> model, bool getAdmins = false)
        {
            if (!_isUserSuperAdmin && !_isUserAdmin)
                throw new CustomException(HttpStatusCode.Forbidden, "role", "You don't have permissions");

            List<UserTableRowResponseModel> response = new List<UserTableRowResponseModel>();

            var search = !string.IsNullOrEmpty(model.Search) && model.Search.Length > 1;

            //!x.UserRoles.Any(w => (_userIsSuperAdmin && w.Role.Name != Role.Admin) && w.Role.Name != Role.SuperAdmin)

            var users = _unitOfWork.Repository<ApplicationUser>().Get(x => !x.IsDeleted
                                            && !x.UserRoles.Any(w => w.Role.Name == Role.SuperAdmin)
                                            && (!search || (x.Email.Contains(model.Search) || x.Profile.FirstName.Contains(model.Search) || x.Profile.LastName.Contains(model.Search)))
                                            && (getAdmins ? x.UserRoles.Any(w => w.Role.Name == Role.Admin) : x.UserRoles.Any(w => w.Role.Name == Role.User))
                                            && (_isUserSuperAdmin || !x.UserRoles.Any(w => (w.Role.Name == Role.Admin))))
                                        .TagWith(nameof(GetAll) + "_GetUsers")
                                        .Include(w => w.UserRoles)
                                            .ThenInclude(w => w.Role)
                                        .Select(x => new
                                        {
                                            Email = x.Email,
                                            FirstName = x.Profile.FirstName,
                                            LastName = x.Profile.LastName,
                                            IsBlocked = !x.IsActive,
                                            RegisteredAt = x.RegistratedAt,
                                            Id = x.Id
                                        });


            if (search)
                users = users.Where(x => x.Email.Contains(model.Search) || x.FirstName.Contains(model.Search) || x.LastName.Contains(model.Search));

            int count = users.Count();

            if (model.Order != null)
                users = users.OrderBy(model.Order.Key.ToString(), model.Order.Direction == SortingDirection.Asc);

            users = users.Skip(model.Offset).Take(model.Limit);

            response = users.Select(x => new UserTableRowResponseModel
            {
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                IsBlocked = x.IsBlocked,
                RegisteredAt = x.RegisteredAt.ToISO(),
                Id = x.Id
            }).ToList();

            return new PaginationResponseModel<UserTableRowResponseModel>(response, count);
        }

        public CursorPaginationBaseResponseModel<UserTableRowResponseModel> GetAll(CursorPaginationRequestModel<UserTableColumn> model, bool getAdmins = false)
        {
            if (!_isUserSuperAdmin && !_isUserAdmin)
                throw new CustomException(HttpStatusCode.Forbidden, "role", "You don't have permissions");

            var search = !string.IsNullOrEmpty(model.Search) && model.Search.Length > 1;

            var users = _unitOfWork.Repository<ApplicationUser>().Get(x => !x.IsDeleted
                                            && !x.UserRoles.Any(w => w.Role.Name == Role.SuperAdmin)
                                            && (!search || (x.Email.Contains(model.Search) || x.Profile.FirstName.Contains(model.Search) || x.Profile.LastName.Contains(model.Search)))
                                            && (getAdmins ? x.UserRoles.Any(w => w.Role.Name == Role.Admin) : x.UserRoles.Any(w => w.Role.Name == Role.User))
                                            && (_isUserSuperAdmin || !x.UserRoles.Any(w => (w.Role.Name == Role.Admin))))
                                        .TagWith(nameof(GetAll) + "_GetUsers")
                                        .Select(x => new
                                        {
                                            Email = x.Email,
                                            FirstName = x.Profile.FirstName,
                                            LastName = x.Profile.LastName,
                                            IsBlocked = !x.IsActive,
                                            RegisteredAt = x.RegistratedAt,
                                            Id = x.Id
                                        });

            if (model.Order != null)
                users = users.OrderBy(model.Order.Key.ToString(), model.Order.Direction == SortingDirection.Asc);

            var userList = users.ToList();

            var offset = 0;
            if (model.LastId.HasValue)
            {
                var item = userList.FirstOrDefault(u => u.Id == model.LastId);

                if (item is null)
                    throw new CustomException(HttpStatusCode.BadRequest, "lastId", "There is no user with specific id in selection");

                offset = userList.IndexOf(item) + 1;
            }

            users = users.Skip(offset).Take(model.Limit + 1);

            var response = users.Select(x => new UserTableRowResponseModel
            {
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                IsBlocked = x.IsBlocked,
                RegisteredAt = x.RegisteredAt.ToISO(),
                Id = x.Id
            });

            int? nextCursorId = null;
            if (users.Count() > model.Limit)
            {
                response = response.Take(model.Limit);
                nextCursorId = response.AsEnumerable().LastOrDefault()?.Id;
            }

            return new CursorPaginationBaseResponseModel<UserTableRowResponseModel>(response.ToList(), nextCursorId);
        }

        public async Task<UserResponseModel> SwitchUserActiveState(int id)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(w => w.Id == id && !w.UserRoles.Any(x => x.Role.Name == Role.SuperAdmin) && (!w.UserRoles.Any(x => x.Role.Name == Role.Admin) || _isUserSuperAdmin))
                                      .TagWith(nameof(SwitchUserActiveState) + "_GetUser")
                                      .Include(w => w.Profile)
                                      .Include(t => t.Tokens)
                                      .FirstOrDefault();

            if (user == null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User is not found");

            user.IsActive = !user.IsActive;

            if (!user.IsActive)
                await _jwtService.ClearUserTokens(user);

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            return _mapper.Map<UserResponseModel>(user);
        }

        public UserResponseModel SoftDeleteUser(int id)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(w => w.Id == id && !w.UserRoles.Any(x => x.Role.Name == Role.SuperAdmin) && (!w.UserRoles.Any(x => x.Role.Name == Role.Admin) || _isUserSuperAdmin))
                                      .TagWith(nameof(SoftDeleteUser) + "_GetUser")
                                      .Include(w => w.Profile)
                                      .FirstOrDefault();

            if (user == null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User is not found");

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            // TODO: Remove if not used ScheduledTasks
            _weekAfterRegistration.Remove(user.Id);

            return _mapper.Map<UserResponseModel>(user.Profile);
        }

        public void HardDeleteUser(int id)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(w => w.Id == id && !w.UserRoles.Any(x => x.Role.Name == Role.SuperAdmin) && ((!w.UserRoles.Any(x => x.Role.Name == Role.Admin) || _isUserSuperAdmin)))
                                      .TagWith(nameof(SoftDeleteUser) + "_GetUser")
                                      .Include(w => w.Profile)
                                      .Include(w => w.Messages)
                                      .FirstOrDefault();

            if (user == null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User is not found");

            // remove user's avatar image
            if (user.Profile.AvatarId.HasValue)
                _imageService.RemoveImage(user.Profile.AvatarId.Value);

            // remove images from messages
            foreach (var message in user.Messages)
                if (message.ImageId.HasValue)
                    _imageService.RemoveImage(message.ImageId.Value);

            // remove last messages from user's chats
            // TODO: FIX THIS CRUTCH
            var chatsWithLastMessage = _unitOfWork.Repository<Chat>().Get(x => x.Users.Any(w => w.UserId == user.Id) && x.LastMessageId.HasValue && x.LastMessage.CreatorId == user.Id)
                .ToList();

            foreach (var chat in chatsWithLastMessage)
            {
                chat.LastMessageId = null;
                _unitOfWork.Repository<Chat>().Update(chat);
            }

            _unitOfWork.Repository<ApplicationUser>().Delete(user);
            _unitOfWork.SaveChanges();

            // TODO: Remove if not used ScheduledTasks
            _weekAfterRegistration.Remove(user.Id);
        }

        public async Task<UserResponseModel> EditProfileAsync(int id, UserProfileRequestModel model)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(w => w.Id == id && (_userId == id || (!w.UserRoles.Any(x => x.Role.Name == Role.SuperAdmin) && (!w.UserRoles.Any(x => x.Role.Name == Role.Admin) || _isUserSuperAdmin))))
                .TagWith(nameof(EditProfileAsync) + "_GetUser")
                .Include(w => w.Profile)
                    .ThenInclude(w => w.Avatar)
                .FirstOrDefault();

            if (user == null)
                return null;
            else if (user.Profile == null)
                user.Profile = new Profile();

            _mapper.Map(model, user.Profile);

            // If user pass avatar id - attach it to profile
            if (model.ImageId.HasValue)
                AddAvatar(user, model.ImageId.Value);
            else if (user.Profile.Avatar != null && !model.ImageId.HasValue)
            {
                // If  user have avatar but in model - null
                await _imageService.RemoveImage(user.Profile.AvatarId.Value);
                user.Profile.Avatar = null;
            }

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            return _mapper.Map<UserResponseModel>(user.Profile);
        }

        public async Task<UserResponseModel> GetProfileAsync(int id)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().Get(w => w.Id == id)
                                        .TagWith(nameof(GetProfileAsync) + "_GetUser")
                                       .Include(w => w.Profile)
                                       .FirstOrDefaultAsync();

            if (user == null)
                return null;

            return _mapper.Map<UserResponseModel>(user.Profile);
        }

        public UserResponseModel DeleteAvatar(int userId)
        {
            var user = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Id == userId)
                .TagWith(nameof(DeleteAvatar) + "_GetUser")
                .Include(x => x.Profile)
                    .ThenInclude(x => x.Avatar)
                .FirstOrDefault();

            if (user.Profile.Avatar == null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User has no avatar");

            // Remove image from database
            _imageService.RemoveImage(user.Profile.AvatarId.Value);

            // Make avatar null
            user.Profile.Avatar = null;
            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            return _mapper.Map<UserResponseModel>(user.Profile);
        }

        public async Task<UserDeviceResponseModel> SetDeviceToken(DeviceTokenRequestModel model, int userId)
        {
            var user = await _unitOfWork.Repository<ApplicationUser>().Get(x => x.Id == userId)
                   .TagWith(nameof(SetDeviceToken) + "_GetUser")
                   .Include(x => x.Devices)
                   .FirstOrDefaultAsync();

            var device = new UserDevice
            {
                DeviceToken = model.DeviceToken,
                AddedAt = DateTime.UtcNow,
                IsVerified = true,
                IsActive = true
            };

            user.Devices.Add(device);

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            var result = _mapper.Map<UserDeviceResponseModel>(device);
            return result;
        }

        private ImageResponseModel AddAvatar(ApplicationUser user, int avatarId)
        {
            var image = _unitOfWork.Repository<Image>().Find(i => i.Id == avatarId && i.IsActive);

            if (image == null)
                throw new ArgumentException($"Can't find image with given id {avatarId}", "id");

            // If avatar already exist - try to remove it
            if (user.Profile.Avatar != null)
                _imageService.RemoveImage(user.Profile.AvatarId.Value);

            user.Profile.Avatar = image;

            return _mapper.Map<ImageResponseModel>(user.Profile.Avatar);
        }
    }
}
