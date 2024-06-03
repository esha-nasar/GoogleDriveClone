using Abp.Domain.Entities;
using GoogleDriveClone.Models;
using Microsoft.EntityFrameworkCore;


namespace GoogleDriveClone.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<GoogleDriveClone.Models.File> Files { get; set; }
     
    }
}
