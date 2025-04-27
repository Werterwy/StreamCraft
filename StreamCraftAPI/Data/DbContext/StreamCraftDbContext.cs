using Microsoft.EntityFrameworkCore;
using StreamCraftAPI.Data.Entities;

namespace StreamCraftAPI.Data.DbContext
{
    public class StreamCraftDbContext : Microsoft.EntityFrameworkCore.DbContext // Fully qualified name to avoid ambiguity  
    {
        public StreamCraftDbContext(DbContextOptions<StreamCraftDbContext> options) : base(options) { }

        public DbSet<Video> Videos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=StreamCraft;Trusted_Connection=True;");
            }
        }
    }
}
