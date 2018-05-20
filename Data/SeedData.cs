using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Houdini.Models;

namespace Houdini.Data
{
	public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var roles = new[] { Constants.OwnerRole, Constants.FranchiseRole, Constants.CustomerRole };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole { Name = role });
                }
            }
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            await EnsureUserHasRole(userManager, "abrahamlincoln2325@gmail.com", Constants.OwnerRole);
            await EnsureUserHasRole(userManager, "mattlebraun32423@gmail.com", Constants.FranchiseRole);
            await EnsureUserHasStoreID(userManager, "mattlebraun32423@gmail.com", 2);
            await EnsureUserHasRole(userManager, "david123@example.com", Constants.CustomerRole); //pw: Dog&Cat1
          
        }
        private static async Task EnsureUserHasRole(
        UserManager<ApplicationUser> userManager, string userName, string role)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user != null && !await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        private static async Task EnsureUserHasStoreID(
        UserManager<ApplicationUser> userManager, string userName, int storeID)
        {
            var user = await userManager.FindByNameAsync(userName);
            user.StoreID = storeID;
            await userManager.UpdateAsync(user);
        }

       
    }
}
