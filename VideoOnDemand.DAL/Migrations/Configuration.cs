using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VideoOnDemand.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoOnDemand.DAL.Migrations
{
    public static class DbInitializer
    {
        public static void Initialize(DataContext context, IConfiguration Configuration, IServiceProvider serviceProvider)
        {
            context.Database.Migrate();

            #region Roles

            new List<ApplicationRole>
            {
                new ApplicationRole { Name = "User", NormalizedName = "User" },
                new ApplicationRole { Name = "Admin", NormalizedName = "Admin" },
                new ApplicationRole { Name = "SuperAdmin", NormalizedName = "SuperAdmin" }
            }
            .ForEach(i =>
            {
                if (!context.Roles.Any(x => x.Name == i.Name))
                    context.Roles.Add(i);
            });

            context.SaveChanges();

            #endregion

            #region Creating Super Admins

            var superadmins = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    UserName = "admin@superadmin.com",
                    Email = "admin@superadmin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "dev@superadmin.com",
                    Email = "dev@superadmin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "dev2@superadmin.com",
                    Email = "dev2@superadmin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "qa@superadmin.com",
                    Email = "qa@superadmin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "qa2@superadmin.com",
                    Email = "qa2@superadmin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
            };

            var admins = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    UserName = "admin@admin.com",
                    Email = "admin@admin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "dev@admin.com",
                    Email = "dev@admin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "dev2@admin.com",
                    Email = "dev2@admin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "qa@admin.com",
                    Email = "qa@admin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
                new ApplicationUser
                {
                    UserName = "qa2@admin.com",
                    Email = "qa2@admin.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false,
                    SecurityStamp = Guid.NewGuid().ToString()
                },
            };

            var password = "Admin123";

            foreach (var user in superadmins)
            {
                if (!context.Users.Any(u => u.UserName == user.UserName))
                    SeedAdmin(user, password, Role.SuperAdmin);
            }

            foreach (var user in admins)
            {
                if (!context.Users.Any(u => u.UserName == user.UserName))
                    SeedAdmin(user, password, Role.Admin);
            }

            void SeedAdmin(ApplicationUser user, string passwordString, string role)
            {
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                IdentityResult result = userManager.CreateAsync(user, passwordString).Result;

                if (result.Succeeded)
                    userManager.AddToRoleAsync(user, role).Wait();
            }

            #endregion 
        }
    }
}