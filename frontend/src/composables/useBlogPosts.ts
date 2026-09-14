import { storeToRefs } from 'pinia'
import { computed } from 'vue'
import { message } from 'ant-design-vue'
import { useBlogPostStore } from '@/stores/blogPost'
import { ApiRequestError } from '@/stores/functions/readError'
import type { BlogPost, BlogPostInput, Category } from '@/types'

export function useBlogPosts() {
  const store = useBlogPostStore()

  const { selectedCategorySlug } = storeToRefs(store)

  function handleError(error: unknown, fallbackMessage: string) {
    if (error instanceof ApiRequestError) {
      const validationErrors = Object.values(error.validationErrors).flat()

      if (validationErrors.length > 0) {
        validationErrors.forEach((errorMessage) => {
          message.error(errorMessage)
        })
        return
      }

      message.error(error.message)
      return
    }

    message.error(error instanceof Error ? error.message : fallbackMessage)
  }

  async function getPosts(
    requestedPage = 1,
    requestedPageSize = 10,
    requestedCategorySlug: string | null = selectedCategorySlug.value,
    tag?: string,
    untaggedOnly: boolean = false,
  ): Promise<void> {
    try {
      await store.fetchPosts(
        requestedPage,
        requestedPageSize,
        requestedCategorySlug,
        tag,
        untaggedOnly,
      )
    } catch (error) {
      handleError(error, 'Failed to load posts.')
    }
  }

  async function getCategoryTags(categorySlug: string): Promise<string[]> {
    try {
      return await store.fetchCategoryTags(categorySlug)
    } catch (error) {
      handleError(error, 'Failed to load category tags.')
      return []
    }
  }

  async function getPost(slug: string): Promise<BlogPost | null> {
    try {
      return await store.loadPostBySlug(slug)
    } catch (error) {
      handleError(error, 'Failed to load post.')
      return null
    }
  }

  async function getCategories(): Promise<void> {
    try {
      await store.fetchCategories()
    } catch (error) {
      handleError(error, 'Failed to load categories.')
    }
  }

  async function createPost(input: BlogPostInput): Promise<BlogPost | null> {
    try {
      const createdPost = await store.createBlogPost(input)
      message.success('Post created successfully.')

      return createdPost
    } catch (error) {
      handleError(error, 'Failed to create post.')
      return null
    }
  }

  async function updatePost(id: number, input: BlogPostInput): Promise<BlogPost | null> {
    try {
      const updatedPost = await store.updateBlogPost(id, input)
      message.success('Post updated successfully.')

      return updatedPost
    } catch (error) {
      handleError(error, 'Failed to update post.')
      return null
    }
  }

  async function publishPost(id: number): Promise<BlogPost | null> {
    try {
      const publishedPost = await store.publishBlogPost(id)
      message.success('Post published successfully.')

      return publishedPost
    } catch (error) {
      handleError(error, 'Failed to publish post.')
      return null
    }
  }

  async function unpublishPost(id: number): Promise<BlogPost | null> {
    try {
      const unpublishedPost = await store.unpublishBlogPost(id)
      message.success('Post unpublished successfully.')

      return unpublishedPost
    } catch (error) {
      handleError(error, 'Failed to unpublish post.')
      return null
    }
  }

  async function deletePost(id: number): Promise<boolean> {
    try {
      await store.deleteBlogPost(id)
      message.success('Post deleted successfully.')

      return true
    } catch (error) {
      handleError(error, 'Failed to delete post.')
      return false
    }
  }

  async function createCategory(name: string): Promise<Category | null> {
    try {
      return await store.createCategory(name)
    } catch (error) {
      handleError(error, 'Failed to create category.')
      return null
    }
  }

  async function updateCategory(id: number, name: string): Promise<Category | null> {
    try {
      return await store.updateCategory(id, name)
    } catch (error) {
      handleError(error, 'Failed to update category.')
      return null
    }
  }

  async function deleteCategory(id: number): Promise<boolean> {
    try {
      await store.deleteCategory(id)
      return true
    } catch (error) {
      handleError(error, 'Failed to delete category.')
      return false
    }
  }

  return {
    posts: computed(() => store.posts),
    currentPost: computed(() => store.currentPost),
    categories: computed(() => store.categories),
    selectedCategorySlug: computed(() => store.selectedCategorySlug),
    page: computed(() => store.page),
    pageSize: computed(() => store.pageSize),
    totalPages: computed(() => store.totalPages),
    totalItems: computed(() => store.totalItems),
    loading: computed(() => store.loading),
    loadError: computed(() => store.loadError),

    getPosts,
    getCategories,
    getCategoryTags,
    createCategory,
    updateCategory,
    deleteCategory,
    getPost,
    createPost,
    updatePost,
    publishPost,
    unpublishPost,
    deletePost,
  }
}
