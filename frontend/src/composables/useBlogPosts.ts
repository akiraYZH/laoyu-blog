import { storeToRefs } from 'pinia'
import { message } from 'ant-design-vue'
import { useBlogPostStore } from '@/stores/blogPost'
import { ApiRequestError } from '@/stores/functions/readError'
import type { BlogPost, BlogPostInput } from '@/types'

export function useBlogPosts() {
  const store = useBlogPostStore()

  const {
    posts,
    currentPost,
    categories,
    selectedCategorySlug,
    page,
    pageSize,
    totalPages,
    totalItems,
    loading,
    loadError,
  } = storeToRefs(store)

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
  ): Promise<void> {
    try {
      await store.fetchPosts(requestedPage, requestedPageSize, requestedCategorySlug)
    } catch (error) {
      handleError(error, 'Failed to load posts.')
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

  return {
    posts,
    currentPost,
    categories,
    selectedCategorySlug,
    page,
    pageSize,
    totalPages,
    totalItems,
    loading,
    loadError,
    getPosts,
    getPost,
    getCategories,
    createPost,
    updatePost,
    deletePost,
  }
}
