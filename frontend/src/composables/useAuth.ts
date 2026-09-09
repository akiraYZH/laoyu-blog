import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import { message } from 'ant-design-vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ApiRequestError } from '@/stores/functions/readError'
import type { LoginInput } from '@/types'

export function useAuth() {
  const router = useRouter()
  const authStore = useAuthStore()
  const { isAuthenticated } = storeToRefs(authStore)
  const route = useRoute()

  const submitting = ref(false)

  async function login(input: LoginInput): Promise<boolean> {
    submitting.value = true

    try {
      await authStore.login(input)

      message.success('Login successful.')

      const redirect = route.query.redirect

      if (typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')) {
        await router.push(redirect)
      } else {
        await router.push({
          name: 'home',
        })
      }

      return true
    } catch (error) {
      if (error instanceof ApiRequestError) {
        message.error(error.message)
      } else {
        message.error(error instanceof Error ? error.message : 'Login failed.')
      }

      return false
    } finally {
      submitting.value = false
    }
  }

  async function logout(): Promise<void> {
    authStore.logout()

    message.success('Logged out.')

    await router.push({
      name: 'home',
    })
  }

  return {
    isAuthenticated,
    submitting,
    login,
    logout,
  }
}
