export interface Category {
  id: number
  name: string
  slug: string
}

export type BlogPostStatus = 'Draft' | 'Published'

export interface BlogPost {
  id: number
  slug: string
  title: string
  content: string
  categories: Category[]
  tags: string[]
  status: BlogPostStatus
  publishedAtUtc: string | null
  createdAtUtc: string
}

export interface BlogPostInput {
  title: string
  slug: string
  content: string
  categoryNames: string[]
  tags: string[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalPages: number
  totalItems: number
}
