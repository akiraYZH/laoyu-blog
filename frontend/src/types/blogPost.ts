export interface Category {
  id: number
  name: string
  slug: string
}

export interface BlogPost {
  id: number
  slug: string
  title: string
  content: string
  categories: Category[]
  createdAtUtc: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalPages: number
  totalItems: number
}