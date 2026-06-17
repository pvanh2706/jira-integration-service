import axios, { AxiosHeaders, type AxiosError, type AxiosRequestConfig } from 'axios'
import { ElMessage } from 'element-plus'

import {
  DEFAULT_API_BASE_URL,
  getStoredAppSettings,
} from '../stores/appSettings'
import type { ApiClientError, ApiErrorResponse, ApiResponse } from '../types/api'

const INTERNAL_AUTH_HEADER = 'X-Internal-Auth'

export const httpClient = axios.create({
  timeout: 30_000,
})

httpClient.interceptors.request.use((config) => {
  const settings = getStoredAppSettings()
  const token = settings.internalAuthToken.trim()

  config.baseURL = normalizeApiBaseUrl(settings.apiBaseUrl)
  config.headers = AxiosHeaders.from(config.headers)

  if (token) {
    config.headers.set(INTERNAL_AUTH_HEADER, token)
  }

  return config
})

httpClient.interceptors.response.use(
  (response) => {
    const payload = response.data as ApiResponse<unknown> | unknown

    if (isApiResponse(payload)) {
      response.data = payload.data
    }

    return response
  },
  (error) => Promise.reject(toApiClientError(error)),
)

export async function apiRequest<T>(config: AxiosRequestConfig): Promise<T> {
  const response = await httpClient.request<T>(config)

  return response.data
}

export function notifyApiError(error: unknown, fallbackMessage = 'Request failed.') {
  ElMessage.error(describeApiError(error, fallbackMessage))
}

export function describeApiError(error: unknown, fallbackMessage = 'Request failed.') {
  const apiError = toApiClientError(error)
  const suffix = apiError.traceId ? ` TraceId: ${apiError.traceId}` : ''

  return `${apiError.message || fallbackMessage}${suffix}`
}

export function toApiClientError(error: unknown): ApiClientError {
  if (isApiClientError(error)) {
    return error
  }

  if (axios.isAxiosError(error)) {
    return fromAxiosError(error)
  }

  if (error instanceof Error) {
    return assignApiError(error.message, undefined, undefined, undefined, error)
  }

  return assignApiError('Request failed.', undefined, undefined, undefined, error)
}

function fromAxiosError(error: AxiosError): ApiClientError {
  const responseData = error.response?.data

  if (isApiErrorResponse(responseData)) {
    return assignApiError(
      responseData.message,
      error.response?.status,
      responseData.errorCode,
      responseData.traceId,
      responseData,
    )
  }

  const message = error.response?.status
    ? `Request failed with status ${error.response.status}.`
    : error.message || 'Cannot connect to API.'

  return assignApiError(message, error.response?.status, undefined, undefined, responseData)
}

function assignApiError(
  message: string,
  status?: number,
  errorCode?: string,
  traceId?: string,
  details?: unknown,
) {
  const error = new Error(message) as ApiClientError
  error.name = 'ApiClientError'
  error.status = status
  error.errorCode = errorCode
  error.traceId = traceId
  error.details = details

  return error
}

function isApiResponse(payload: unknown): payload is ApiResponse<unknown> {
  return (
    typeof payload === 'object' &&
    payload !== null &&
    (payload as { success?: unknown }).success === true &&
    'data' in payload
  )
}

function isApiErrorResponse(payload: unknown): payload is ApiErrorResponse {
  return (
    typeof payload === 'object' &&
    payload !== null &&
    (payload as { success?: unknown }).success === false &&
    typeof (payload as { message?: unknown }).message === 'string'
  )
}

function isApiClientError(error: unknown): error is ApiClientError {
  return error instanceof Error && error.name === 'ApiClientError'
}

function normalizeApiBaseUrl(value: string) {
  const normalizedValue = value.trim() || DEFAULT_API_BASE_URL

  return normalizedValue.endsWith('/')
    ? normalizedValue.slice(0, normalizedValue.length - 1)
    : normalizedValue
}
