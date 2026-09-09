import router from '@/router'
import { useAuthStore } from '@/stores/auth'

export async function apiFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<Response> {
  const authStore = useAuthStore()
  const headers = new Headers(init.headers)

  if (authStore.accessToken && authStore.isAuthenticated) {
    headers.set('Authorization', `Bearer ${authStore.accessToken}`)
  }

  const response = await fetch(input, {
    ...init,
    headers,
  })

  if (response.status === 401) {
    const currentRoute = router.currentRoute.value

    authStore.logout()

    if (currentRoute.name !== 'login') {
      await router.push({
        name: 'login',
        query: {
          redirect: currentRoute.fullPath,
        },
      })
    }
  }

  return response
}
