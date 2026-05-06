using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VersionService.Migrations
{
    [DbContext(typeof(VersionService.Data.VersionDbContext))]
    partial class VersionDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("VersionService.Models.Snapshot", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("Content")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<int>("CreatedByUserId")
                    .HasColumnType("int");

                b.Property<int>("FileId")
                    .HasColumnType("int");

                b.Property<string>("Message")
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<DateTime>("Timestamp")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("CreatedByUserId");

                b.HasIndex("FileId");

                b.HasIndex("Timestamp");

                b.ToTable("Snapshots");
            });
#pragma warning restore 612, 618
        }
    }
}
