using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PracticalAssignment.Model;




namespace PracticalAssignment.Services
{
    public static class IdentityConfigurationExtensions
    {

        public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUserStuff, IdentityRole>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;

              

            })
            .AddEntityFrameworkStores<AuthDbContext>();

            return services;
        }
    }
}
