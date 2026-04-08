using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Context;
public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().Property(u => u.role).HasConversion<string>();
        modelBuilder.Entity<Like>().HasKey(l => new { l.reviewId, l.userId });
        // 왜래키 연결
        {
            // modelBuilder.Entity<Invite>().HasOne(i => i.UsedByUser).WithMany().HasForeignKey(i => i.usedBy).OnDelete(DeleteBehavior.SetNull);
            // modelBuilder.Entity<Invite>().HasOne(i => i.UsedByUser).WithMany().HasForeignKey(i => i.inviteCode).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Invite>().HasOne(i => i.Creator).WithMany().HasForeignKey(i => i.createdBy).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Review>().HasOne(r => r.User).WithMany().HasForeignKey(r => r.userId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Review>().HasOne(r => r.Building).WithMany().HasForeignKey(r => r.buildingId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Like>().HasOne(l => l.Review).WithMany().HasForeignKey(l => l.reviewId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Like>().HasOne(l => l.User).WithMany().HasForeignKey(l => l.userId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Building>().HasOne(b => b.UploadUser).WithMany().HasForeignKey(b => b.uploadUserId).OnDelete(DeleteBehavior.Cascade);                                                                      
        }
    }

    public DbSet<Building> building { get; set; }
    public DbSet<Invite> invite { get; set; }
    public DbSet<Like> review_like { get; set; }
    public DbSet<Review> review { get; set; }
    public DbSet<User> users { get; set; }
}
