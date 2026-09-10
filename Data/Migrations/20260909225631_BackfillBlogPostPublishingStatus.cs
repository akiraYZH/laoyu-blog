using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace laoyu_blog_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBlogPostPublishingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            """
            UPDATE "BlogPosts"
            SET "Status" = 'Published',
                "PublishedAtUtc" = COALESCE(
                    "PublishedAtUtc",
                    "CreatedAtUtc"
                )
            WHERE "Status" = '';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
