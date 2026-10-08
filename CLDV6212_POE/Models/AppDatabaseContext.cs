using Microsoft.EntityFrameworkCore;

namespace CLDV6212_POE.Models
{
    public class AppDatabaseContext : DbContext
    {

        //------------------------------------------------------------------------------------------//
        //Our Application Database Context
        public AppDatabaseContext(DbContextOptions<AppDatabaseContext> options) : base(options)
        {

        }
        //------------------------------------------------------------------------------------------//

        //------------------------------------------------------------------------------------------//
        //Our Database Sets for our tables
        public DbSet<User> Users { get; set; }
        //------------------------------------------------------------------------------------------//
    }
}
//------------------------------------------0_0-ENDOFFILE-0_0---------------------------------------//