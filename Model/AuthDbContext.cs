using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PracticalAssignment.Model
{
    public class AuthDbContext : IdentityDbContext<ApplicationUserStuff>
    {
        private readonly IConfiguration _configuration;
        //public AuthDbContext(DbContextOptions<AuthDbContext> options):base(options){ }
        public AuthDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DbSet<ActiveSession> ActiveSessions { get; set; }

		public DbSet<AuditLog> AuditLogs { get; set; }
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = _configuration.GetConnectionString("AuthConnectionString"); optionsBuilder.UseSqlServer(connectionString);
        }

        public DbSet<PasswordHistory> PasswordHistory { get; set; }

    }




}