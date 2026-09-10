<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useAuth } from '@/composables/useAuth'

defineOptions({ name: 'AppHeader' })

const route = useRoute()
const { isAuthenticated, logout } = useAuth()

const showLoginOption = computed(() => Object.prototype.hasOwnProperty.call(route.query, 'login'))
</script>

<template>
  <header class="border-b border-slate-200 bg-white">
    <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
      <RouterLink to="/" class="text-xl font-bold text-slate-900">Laoyu Blog</RouterLink>

      <nav aria-label="Primary navigation" class="flex flex-wrap items-center gap-x-5 gap-y-2">
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink>
        <RouterLink to="/contact">Contact</RouterLink>
        <template v-if="isAuthenticated">
          <RouterLink to="/create-blog"> Create Blog </RouterLink>

          <a-button type="link" class="p-0!" @click="logout"> Logout </a-button>
        </template>

        <RouterLink v-else-if="showLoginOption" to="/login"> Login </RouterLink>
      </nav>
    </div>
  </header>
</template>
