using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data.Models;

namespace Nexora.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await db.Database.MigrateAsync();

        await EnsureRole(roleManager, "Owner");
        await EnsureRole(roleManager, "Staff");

        if (!await db.SubscriptionPlans.AnyAsync())
        {
            db.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Code = "starter",
                Name = "Starter",
                MonthlyPrice = 0
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureRole(RoleManager<IdentityRole<Guid>> roleManager, string name)
    {
        if (!await roleManager.RoleExistsAsync(name))
            await roleManager.CreateAsync(new IdentityRole<Guid>(name));
    }
}