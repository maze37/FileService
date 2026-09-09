using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.CreateTable(
                name: "media_assets",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_extension = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    expected_chunks_count = table.Column<long>(type: "bigint", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsTemporary = table.Column<bool>(type: "boolean", nullable: false),
                    storage_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    storage_prefix = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    storage_bucket = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    storage_full_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    owner_context = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    owner_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedWhen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedWhen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    asset_kind = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_asset_type",
                schema: "files",
                table: "media_assets",
                column: "AssetType");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_owner",
                schema: "files",
                table: "media_assets",
                columns: new[] { "owner_context", "owner_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_status",
                schema: "files",
                table: "media_assets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ux_media_assets_storage_key",
                schema: "files",
                table: "media_assets",
                columns: new[] { "storage_bucket", "storage_prefix", "storage_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets",
                schema: "files");
        }
    }
}
