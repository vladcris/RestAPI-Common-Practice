using Microsoft.EntityFrameworkCore;
using MyBGList.Models;

namespace MyBGGList.Test.Helpers;
public class DatabaseFixture
{
    public ApplicationDbContext DbContext { get;}
    public DatabaseFixture()
    {
        var cnnString = new TestConfig().ConnectionString;

        var optionBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionBuilder.UseNpgsql(cnnString);

        DbContext = new ApplicationDbContext(optionBuilder.Options);
        DbContext.Database.Migrate();
    }
}

