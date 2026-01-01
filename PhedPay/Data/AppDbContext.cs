using Microsoft.EntityFrameworkCore;
using PhedPay.Models;

namespace PhedPay.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<TransactionEntity> Transactions { get; set; }
    }

    public class AppDbOracleContext : DbContext
    {
        public AppDbOracleContext(DbContextOptions<AppDbOracleContext> options) : base(options) { }
        public DbSet<Payment> Payments { get; set; }
    }
}

