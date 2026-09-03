import { ref } from 'vue'
import { acceptHMRUpdate, defineStore } from 'pinia'
import type { BlogPost, BlogPostInput, Category, PagedResult } from '@/types'
import { createApiRequestError } from '@/stores/functions/readError'

export const useBlogPostStore = defineStore('blogPost', () => {
  // 文章列表
  const posts = ref<BlogPost[]>([])
  const currentPost = ref<BlogPost | null>(null)
  const categories = ref<Category[]>([])

  // 分页数据
  const page = ref(1)
  const pageSize = ref(10)
  const totalPages = ref(0)
  const totalItems = ref(0)

  // 请求状态
  const loading = ref(false)
  const loadError = ref<string | null>(null)

  async function fetchPosts(requestedPage = 1, requestedPageSize = 10): Promise<void> {
    loading.value = true
    loadError.value = null

    try {
      const response = await fetch(`/api/blogs?page=${requestedPage}&pageSize=${requestedPageSize}`)

      if (!response.ok) {
        throw new Error(`获取文章失败：HTTP ${response.status}`)
      }

      const result: PagedResult<BlogPost> = await response.json()

      posts.value = result.items
      page.value = result.page
      pageSize.value = result.pageSize
      totalPages.value = result.totalPages
      totalItems.value = result.totalItems
    } catch (caughtError) {
      loadError.value =
        caughtError instanceof Error ? caughtError.message : '获取文章时发生未知错误'

      throw caughtError
    } finally {
      loading.value = false
    }
  }

  async function fetchCategories(): Promise<void> {
    try {
      const response = await fetch('/api/categories')

      if (!response.ok) {
        throw new Error(`获取分类失败：HTTP ${response.status}`)
      }

      categories.value = await response.json()
    } catch (caughtError) {
      loadError.value =
        caughtError instanceof Error ? caughtError.message : '获取分类时发生未知错误'

      throw caughtError
    }
  }

  async function createBlogPost(input: BlogPostInput): Promise<BlogPost> {
    loading.value = true

    try {
      const response = await fetch('/api/blogs', {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify(input),
      })

      if (!response.ok) {
        throw await createApiRequestError(response)
      }

      const createdPost: BlogPost = await response.json()

      // 把新文章放在当前列表最前面
      posts.value.unshift(createdPost)
      totalItems.value += 1

      return createdPost
    } finally {
      loading.value = false
    }
  }

  async function updateBlogPost(id: number, input: BlogPostInput): Promise<BlogPost> {
    loading.value = true

    try {
      const response = await fetch(`/api/blogs/${id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(input),
      })

      if (!response.ok) {
        throw await createApiRequestError(response)
      }

      const updatedPost: BlogPost = await response.json()
      const postIndex = posts.value.findIndex((post) => post.id === updatedPost.id)

      if (postIndex !== -1) {
        posts.value[postIndex] = updatedPost
      }

      currentPost.value = updatedPost

      return updatedPost
    } finally {
      loading.value = false
    }
  }

  async function deleteBlogPost(id: number): Promise<void> {
    loading.value = true

    try {
      const response = await fetch(`/api/blogs/${id}`, {
        method: 'DELETE',
      })

      if (!response.ok) {
        throw await createApiRequestError(response)
      }

      posts.value = posts.value.filter((post) => post.id !== id)

      if (currentPost.value?.id === id) {
        currentPost.value = null
      }

      totalItems.value = Math.max(0, totalItems.value - 1)
      totalPages.value = Math.ceil(totalItems.value / pageSize.value)
    } finally {
      loading.value = false
    }
  }

  async function fetchPostBySlug(slug: string): Promise<BlogPost | null> {
    loading.value = true
    loadError.value = null
    currentPost.value = null

    try {
      const response = await fetch(`/api/blogs/by-slug/${encodeURIComponent(slug)}`)

      if (response.status === 404) {
        return null
      }

      if (!response.ok) {
        throw new Error(`获取文章失败：HTTP ${response.status}`)
      }

      const post: BlogPost = await response.json()
      currentPost.value = post

      return post
    } catch (caughtError) {
      loadError.value =
        caughtError instanceof Error ? caughtError.message : '获取文章时发生未知错误'

      throw caughtError
    } finally {
      loading.value = false
    }
  }

  async function loadPostBySlug(slug: string): Promise<BlogPost | null> {
    loadError.value = null

    const cachedPost = posts.value.find((post) => post.slug === slug)

    if (cachedPost) {
      currentPost.value = cachedPost
      loading.value = false

      return cachedPost
    }

    return fetchPostBySlug(slug)
  }

  return {
    posts,
    currentPost,
    categories,
    page,
    pageSize,
    totalPages,
    totalItems,
    loading,
    loadError,
    fetchPosts,
    fetchCategories,
    loadPostBySlug,
    createBlogPost,
    updateBlogPost,
    deleteBlogPost,
  }
})

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useBlogPostStore, import.meta.hot))
}
