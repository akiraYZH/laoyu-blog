import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { BlogPost, PagedResult } from '@/types'

export interface CreateBlogPostInput {
  title: string
  slug: string
  content: string
}

export const useBlogPostStore = defineStore('blogPost', () => {
  // 文章列表
  const posts = ref<BlogPost[]>([])
  const currentPost = ref<BlogPost | null>(null)

  // 分页数据
  const page = ref(1)
  const pageSize = ref(10)
  const totalPages = ref(0)
  const totalItems = ref(0)

  // 请求状态
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchPosts(requestedPage = 1, requestedPageSize = 10): Promise<void> {
    loading.value = true
    error.value = null

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
      error.value = caughtError instanceof Error ? caughtError.message : '获取文章时发生未知错误'

      throw caughtError
    } finally {
      loading.value = false
    }
  }

  async function createBlogPost(input: CreateBlogPostInput): Promise<BlogPost> {
    loading.value = true
    error.value = null

    try {
      const response = await fetch('/api/blogs', {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify(input),
      })

      if (!response.ok) {
        throw new Error(`创建文章失败：HTTP ${response.status}`)
      }

      const createdPost: BlogPost = await response.json()

      // 把新文章放在当前列表最前面
      posts.value.unshift(createdPost)
      totalItems.value += 1

      return createdPost
    } catch (caughtError) {
      error.value = caughtError instanceof Error ? caughtError.message : '创建文章时发生未知错误'

      throw caughtError
    } finally {
      loading.value = false
    }
  }

  async function fetchPostBySlug(slug: string): Promise<BlogPost | null> {
    loading.value = true
    error.value = null
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
      error.value = caughtError instanceof Error ? caughtError.message : '获取文章时发生未知错误'

      throw caughtError
    } finally {
      loading.value = false
    }
  }

  async function loadPostBySlug(slug: string): Promise<BlogPost | null> {
    error.value = null

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
    page,
    pageSize,
    totalPages,
    totalItems,
    loading,
    error,
    fetchPosts,
    loadPostBySlug,
    createBlogPost,
  }
})
