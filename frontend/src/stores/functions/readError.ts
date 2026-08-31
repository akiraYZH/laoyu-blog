export interface ApiProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export class ApiRequestError extends Error {
  readonly status: number
  readonly validationErrors: Record<string, string[]>

  constructor(message: string, status: number, validationErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
    this.validationErrors = validationErrors
  }
}

export async function createApiRequestError(response: Response): Promise<ApiRequestError> {
  try {
    const problem: ApiProblemDetails = await response.json()

    return new ApiRequestError(
      problem.detail ?? problem.title ?? `HTTP ${response.status}`,
      response.status,
      problem.errors,
    )
  } catch {
    return new ApiRequestError(`HTTP ${response.status}`, response.status)
  }
}
