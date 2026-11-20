using Microsoft.EntityFrameworkCore;
using PropertyShowingBookingApi.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace PropertyShowingBookingApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShowingSlot> Slots => Set<ShowingSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingHistory> BookingHistories => Set<BookingHistory>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store DateTimeOffset as UTC ticks (INTEGER) in SQLite.
        // This allows SQLite to compare and sort date values correctly.
        var dateTimeOffsetConverter =
            new ValueConverter<DateTimeOffset, long>(
                value => value.UtcDateTime.Ticks,
                value => new DateTimeOffset(value, TimeSpan.Zero));

        modelBuilder.Entity<ShowingSlot>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.StartsAt)
                .HasConversion(dateTimeOffsetConverter);

            entity.Property(x => x.CreatedAt)
                .HasConversion(dateTimeOffsetConverter);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Slots_Capacity",
                    "Capacity BETWEEN 1 AND 20");

                t.HasCheckConstraint(
                    "CK_Slots_BookedSeats",
                    "BookedSeats >= 0 AND BookedSeats <= Capacity");
            });

            entity.HasIndex(x => new { x.PropertyId, x.StartsAt });
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CreatedAt)
                .HasConversion(dateTimeOffsetConverter);

            entity.Property(x => x.RenterEmail)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.HasOne(x => x.Slot)
                .WithMany()
                .HasForeignKey(x => x.SlotId);

            entity.HasIndex(x => new { x.SlotId, x.RenterEmail })
                .IsUnique()
                .HasFilter("\"Status\" = 'Confirmed'");
        });

        modelBuilder.Entity<BookingHistory>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ChangedAt)
                .HasConversion(dateTimeOffsetConverter);

            entity.Property(x => x.FromStatus)
                .HasConversion<string>();

            entity.Property(x => x.ToStatus)
                .HasConversion<string>();

            entity.Property(x => x.Actor)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasOne(x => x.Booking)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.BookingId);
        });
    }

}
