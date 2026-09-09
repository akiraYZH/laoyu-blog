import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { LoginInput, LoginResponse } from '@/types'
import { createApiRequestError } from '@/stores/functions/readError'

const ACCESS_TOKEN_KEY = 'admin_access_token'
const EXPIRES_AT_KEY = 'admin_token_expires_at'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(sessionStorage.getItem(ACCESS_TOKEN_KEY))

  const expiresAtUtc = ref<string | null>(sessionStorage.getItem(EXPIRES_AT_KEY))

  const isAuthenticated = computed(() => {
    if (!accessToken.value || !expiresAtUtc.value) {
      return false
    }

    return Date.parse(expiresAtUtc.value) > Date.now()
  })

  async function login(input: LoginInput): Promise<void> {
    const response = await fetch('/api/auth/login', {
      method: 'POST',

      headers: {
        'Content-Type': 'application/json',
      },

      body: JSON.stringify(input),
    })

    if (!response.ok) {
      throw await createApiRequestError(response)
    }

    const result: LoginResponse = await response.json()

    accessToken.value = result.accessToken
    expiresAtUtc.value = result.expiresAtUtc

    sessionStorage.setItem(ACCESS_TOKEN_KEY, result.accessToken)

    sessionStorage.setItem(EXPIRES_AT_KEY, result.expiresAtUtc)
  }

  function logout(): void {
    accessToken.value = null
    expiresAtUtc.value = null

    sessionStorage.removeItem(ACCESS_TOKEN_KEY)
    sessionStorage.removeItem(EXPIRES_AT_KEY)
  }

  return {
    accessToken,
    expiresAtUtc,
    isAuthenticated,
    login,
    logout,
  }
})
