using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitFullNameIntoFirstAndLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: add new columns as nullable so we can populate them first
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "People",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "People",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Step 2: populate from existing FullName using a space-split strategy.
            // FirstName = everything before the first space (or the whole value if no space).
            // LastName  = everything after the first space (empty string if no space).
            migrationBuilder.Sql(@"
                UPDATE People
                SET
                    FirstName = CASE
                        WHEN CHARINDEX(' ', RTRIM(FullName)) > 0
                        THEN LEFT(RTRIM(FullName), CHARINDEX(' ', RTRIM(FullName)) - 1)
                        ELSE RTRIM(FullName)
                    END,
                    LastName = CASE
                        WHEN CHARINDEX(' ', RTRIM(FullName)) > 0
                        THEN LTRIM(SUBSTRING(RTRIM(FullName), CHARINDEX(' ', RTRIM(FullName)) + 1, LEN(FullName)))
                        ELSE ''
                    END
            ");

            // Step 3: make columns NOT NULL now that data is populated
            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "People",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "People",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Step 4: drop the old column
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "People");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: restore FullName from FirstName + LastName
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "People",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE People
                SET FullName = CASE
                    WHEN LEN(RTRIM(LastName)) > 0
                    THEN RTRIM(FirstName) + ' ' + RTRIM(LastName)
                    ELSE RTRIM(FirstName)
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "People",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.DropColumn(name: "FirstName", table: "People");
            migrationBuilder.DropColumn(name: "LastName", table: "People");
        }
    }
}
