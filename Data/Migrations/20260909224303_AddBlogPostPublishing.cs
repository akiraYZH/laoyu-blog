using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laoyu_blog_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogPostPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "BlogPosts",
                type: "timestamp with time zone",
                nullable: true);

            // 第一步：先允许 null，否则旧数据没有合法的 Status
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BlogPosts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // 第二步：把现有文章视为已经发布的文章
            migrationBuilder.Sql(
                """
                UPDATE "BlogPosts"
                SET "Status" = 'Published',
                    "PublishedAtUtc" = "CreatedAtUtc";
                """);

            // 第三步：数据填充完成后，再禁止 Status 为 null
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BlogPosts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BlogPosts");
        }
    }
}
