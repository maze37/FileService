using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class StorageMetadataAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "storage_metadata_actual_content_type",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_metadata_actual_size_bytes",
                schema: "files",
                table: "media_assets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_metadata_e_tag",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "storage_metadata_actual_content_type",
                schema: "files",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "storage_metadata_actual_size_bytes",
                schema: "files",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "storage_metadata_e_tag",
                schema: "files",
                table: "media_assets");
        }
    }
}
