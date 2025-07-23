using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace krestiki_noliki_api.Models;

public partial class KrestikiNolikiContext : DbContext
{
    public KrestikiNolikiContext()
    {
    }

    public KrestikiNolikiContext(DbContextOptions<KrestikiNolikiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<Move> Moves { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Data Source=LUCYPYAN\\SQLEXPRESS;Initial Catalog=krestiki_noliki;Integrated Security=True;Trust Server Certificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Games__3214EC07AA71A0CA");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime");
            entity.Property(e => e.CurrentTurn).HasDefaultValue(1);
            entity.Property(e => e.State)
                .HasMaxLength(20)
                .HasDefaultValue("InProgress");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Move>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Moves__3214EC0792AA81EE");

            entity.HasIndex(e => new { e.GameId, e.RequestHash }, "UQ_Game_RequestHash").IsUnique();

            entity.HasIndex(e => new { e.GameId, e.X, e.Y }, "UQ_Game_XY").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.RequestHash).HasMaxLength(100);

            entity.HasOne(d => d.Game).WithMany(p => p.Moves)
                .HasForeignKey(d => d.GameId)
                .HasConstraintName("FK__Moves__GameId__52593CB8");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
