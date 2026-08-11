using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laoyu_blog_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBlogPostSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
        """
        UPDATE "BlogPosts"
        SET "Slug" = 'post-' || "Id"::text
        WHERE "Slug" IS NULL;
        """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.Sql(
        """
        UPDATE "BlogPosts"
        SET "Slug" = NULL
        WHERE "Slug" = 'post-' || "Id"::text;
        """);
        }
    }
}
