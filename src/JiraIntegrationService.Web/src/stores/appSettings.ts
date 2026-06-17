import { useLocalStorage } from '@vueuse/core'
import { defineStore } from 'pinia'

export interface AppSettings {
  apiBaseUrl: string
  internalAuthToken: string
}

export const DEFAULT_API_BASE_URL = '/api'
export const DEFAULT_INTERNAL_AUTH_TOKEN = 'dev-internal-token'

export const APP_SETTINGS_STORAGE_KEYS = {
  apiBaseUrl: 'jiraIntegration.apiBaseUrl',
  internalAuthToken: 'jiraIntegration.internalAuthToken',
} as const

export function getStoredAppSettings(): AppSettings {
  return {
    apiBaseUrl: readStoredValue(APP_SETTINGS_STORAGE_KEYS.apiBaseUrl, DEFAULT_API_BASE_URL),
    internalAuthToken: readStoredValue(
      APP_SETTINGS_STORAGE_KEYS.internalAuthToken,
      DEFAULT_INTERNAL_AUTH_TOKEN,
    ),
  }
}

export const useAppSettingsStore = defineStore('appSettings', () => {
  const apiBaseUrl = useLocalStorage(APP_SETTINGS_STORAGE_KEYS.apiBaseUrl, DEFAULT_API_BASE_URL)
  const internalAuthToken = useLocalStorage(
    APP_SETTINGS_STORAGE_KEYS.internalAuthToken,
    DEFAULT_INTERNAL_AUTH_TOKEN,
  )

  function updateSettings(settings: Partial<AppSettings>) {
    if (settings.apiBaseUrl !== undefined) {
      apiBaseUrl.value = normalizeSetting(settings.apiBaseUrl, DEFAULT_API_BASE_URL)
    }

    if (settings.internalAuthToken !== undefined) {
      internalAuthToken.value = normalizeSetting(
        settings.internalAuthToken,
        DEFAULT_INTERNAL_AUTH_TOKEN,
      )
    }
  }

  function resetSettings() {
    apiBaseUrl.value = DEFAULT_API_BASE_URL
    internalAuthToken.value = DEFAULT_INTERNAL_AUTH_TOKEN
  }

  return {
    apiBaseUrl,
    internalAuthToken,
    updateSettings,
    resetSettings,
  }
})

function readStoredValue(key: string, fallback: string) {
  if (typeof localStorage === 'undefined') {
    return fallback
  }

  return normalizeSetting(localStorage.getItem(key), fallback)
}

function normalizeSetting(value: string | null | undefined, fallback: string) {
  const normalizedValue = value?.trim()

  return normalizedValue ? normalizedValue : fallback
}
