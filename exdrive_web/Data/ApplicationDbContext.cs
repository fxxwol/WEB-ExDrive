using exdrive_web.Models;
using Microsoft.EntityFrameworkCore;

namespace exdrive_web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet <Users> Users{ get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            DbContextOptionsBuilder dbContextOptionsBuilder = optionsBuilder.UseMySql("Server=project-01.mysql.database.azure.com;Port=3306;Database=project-01;Uid=Yulii_d@project-01;Pwd=Yulia011_;SslMode=Preferred;",
                new MySqlServerVersion(new Version(8, 0, 26))
            );
        }
    }
}
