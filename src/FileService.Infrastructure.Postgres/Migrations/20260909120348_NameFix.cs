using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class NameFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "files",
                table: "media_assets",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "files",
                table: "media_assets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedWhen",
                schema: "files",
                table: "media_assets",
                newName: "updated_when");

            migrationBuilder.RenameColumn(
                name: "IsTemporary",
                schema: "files",
                table: "media_assets",
                newName: "is_temporary");

            migrationBuilder.RenameColumn(
                name: "CreatedWhen",
                schema: "files",
                table: "media_assets",
                newName: "created_when");

            migrationBuilder.RenameColumn(
                name: "AssetType",
                schema: "files",
                table: "media_assets",
                newName: "asset_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                schema: "files",
                table: "media_assets",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "files",
                table: "media_assets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_when",
                schema: "files",
                table: "media_assets",
                newName: "UpdatedWhen");

            migrationBuilder.RenameColumn(
                name: "is_temporary",
                schema: "files",
                table: "media_assets",
                newName: "IsTemporary");

            migrationBuilder.RenameColumn(
                name: "created_when",
                schema: "files",
                table: "media_assets",
                newName: "CreatedWhen");

            migrationBuilder.RenameColumn(
                name: "asset_type",
                schema: "files",
                table: "media_assets",
                newName: "AssetType");
        }
    }
}
