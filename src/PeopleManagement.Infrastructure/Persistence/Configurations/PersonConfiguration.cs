using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeopleManagement.Domain.People;

namespace PeopleManagement.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(Person.FullNameMaxLength);

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(Person.EmailMaxLength);

        builder.Property(p => p.Phone)
            .IsRequired()
            .HasMaxLength(Person.PhoneMaxLength);

        builder.Property(p => p.ProfilePhotoStoredPath)
            .HasMaxLength(Person.ProfilePhotoStoredPathMaxLength);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(p => p.Email)
            .IsUnique()
            .HasDatabaseName("UX_People_Email");
    }
}
