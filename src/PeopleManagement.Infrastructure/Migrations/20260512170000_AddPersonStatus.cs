using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default value 0 = PersonStatus.Active, so all existing rows become Active.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "People",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "People");
        }
    }
}
