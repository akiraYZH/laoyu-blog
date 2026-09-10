using laoyu_blog_backend.Models;

namespace LaoyuBlog.Api.Tests;

public sealed class BlogPostDomainTests
{
    [Fact]
    public void NewPost_DefaultsToDraft()
    {
        var post = new BlogPost();

        Assert.Equal(BlogPostStatus.Draft, post.Status);
        Assert.Null(post.PublishedAtUtc);
    }

    [Fact]
    public void Publish_DraftPost_PublishesPost()
    {
        var post = new BlogPost();

        var changed = post.Publish();

        Assert.True(changed);
        Assert.Equal(BlogPostStatus.Published, post.Status);
        Assert.NotNull(post.PublishedAtUtc);
    }

    [Fact]
    public void Publish_PublishedPost_DoesNotChangePublishedTime()
    {
        var post = new BlogPost();
        post.Publish();
        var firstPublishedAtUtc = post.PublishedAtUtc;

        var changed = post.Publish();

        Assert.False(changed);
        Assert.Equal(firstPublishedAtUtc, post.PublishedAtUtc);
    }

    [Fact]
    public void Unpublish_PublishedPost_ReturnsPostToDraft()
    {
        var post = new BlogPost();
        post.Publish();

        var changed = post.Unpublish();

        Assert.True(changed);
        Assert.Equal(BlogPostStatus.Draft, post.Status);
        Assert.Null(post.PublishedAtUtc);
    }

    [Fact]
    public void Unpublish_DraftPost_DoesNotChangePost()
    {
        var post = new BlogPost();

        var changed = post.Unpublish();

        Assert.False(changed);
        Assert.Equal(BlogPostStatus.Draft, post.Status);
        Assert.Null(post.PublishedAtUtc);
    }
}
