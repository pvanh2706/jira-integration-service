export interface ApiResponse<T> {
  success: true
  data: T
  traceId: string
}

export interface ApiErrorResponse {
  success: false
  errorCode: string
  message: string
  traceId: string
}

export interface ApiClientError extends Error {
  status?: number
  errorCode?: string
  traceId?: string
  details?: unknown
}
