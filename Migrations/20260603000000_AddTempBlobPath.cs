using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace upload_architecture.Migrations
{
    /// <inheritdoc />
    public partial class AddTempBlobPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "temp_blob_path",
                table: "files",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "temp_blob_path",
                table: "files");
        }
    }
}
