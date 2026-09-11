import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { apiFetch } from '@/api/apiFetch'
import { useBlogPostStore } from '@/stores/blogPost'
import type { BlogPost, PagedResult } from '@/types'

vi.mock('@/api/apiFetch', () => ({
  apiFetch: vi.fn(),
}))

const mockedApiFetch = vi.mocked(apiFetch)

const publishedPost: BlogPost = {
  id: 7,
  slug: 'testing-vue-stores',
  title: 'Testing Vue Stores',
  content: 'A practical introduction to testing Pinia stores.',
  categories: [
    {
      id: 2,
      name: 'Vue',
      slug: 'vue',
    },
  ],
  status: 'Published',
  publishedAtUtc: '2026-09-11T12:00:00Z',
  createdAtUtc: '2026-09-10T12:00:00Z',
}

describe('blog post store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mockedApiFetch.mockReset()
  })

  it('loads a filtered page and stores the pagination data', async () => {
    const result: PagedResult<BlogPost> = {
      items: [publishedPost],
      page: 2,
      pageSize: 5,
      totalPages: 3,
      totalItems: 11,
    }

    mockedApiFetch.mockResolvedValue(
      new Response(JSON.stringify(result), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )

    const store = useBlogPostStore()

    await store.fetchPosts(2, 5, 'vue')

    expect(mockedApiFetch).toHaveBeenCalledExactlyOnceWith(
      '/api/blogs?page=2&pageSize=5&categorySlug=vue',
    )
    expect(store.posts).toEqual([publishedPost])
    expect(store.selectedCategorySlug).toBe('vue')
    expect(store.page).toBe(2)
    expect(store.pageSize).toBe(5)
    expect(store.totalPages).toBe(3)
    expect(store.totalItems).toBe(11)
    expect(store.loadError).toBeNull()
    expect(store.loading).toBe(false)
  })

  it('records the error and restores loading when loading a page fails', async () => {
    mockedApiFetch.mockResolvedValue(new Response(null, { status: 500 }))

    const store = useBlogPostStore()

    await expect(store.fetchPosts()).rejects.toThrow('获取文章失败：HTTP 500')
    expect(store.loadError).toBe('获取文章失败：HTTP 500')
    expect(store.loading).toBe(false)
  })

  it('uses a cached post instead of sending another request', async () => {
    const store = useBlogPostStore()
    store.posts = [publishedPost]

    const result = await store.loadPostBySlug(publishedPost.slug)

    expect(result).toEqual(publishedPost)
    expect(store.currentPost).toEqual(publishedPost)
    expect(mockedApiFetch).not.toHaveBeenCalled()
  })
})
