export interface BlogPost {
  id: number
  slug: string
  title: string
  content: string
  createdAtUtc: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalPages: number
  totalItems: number
}