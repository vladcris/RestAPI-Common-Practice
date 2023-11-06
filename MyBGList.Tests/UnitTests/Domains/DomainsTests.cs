using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyBGGList.Test.Helpers;
using MyBGList.Models;

namespace MyBGList.Tests.UnitTests.Domains;
public class DomainsTests : IClassFixture<DatabaseFixture>
{
    private readonly ApplicationDbContext _context;

    public DomainsTests(DatabaseFixture context)
    {
        _context = context.DbContext;
    }

    //[Fact]
    public void Ensure_Database_Created() {
        var database = _context.Database.IsNpgsql();

        database.Should().BeTrue();
    }
}
