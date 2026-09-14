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
      <RouterLink
        to="/"
        class="group flex items-center gap-2 text-2xl font-black tracking-tight text-slate-900 transition-colors duration-300 hover:text-slate-600"
      >
        <img
          src="/favicon.svg"
          alt="Logo"
          class="h-8 w-8 transition-transform duration-300 group-hover:scale-110"
        />
        <span>YZH Code</span>
      </RouterLink>

      <nav aria-label="Primary navigation" class="flex flex-wrap items-center gap-x-5 gap-y-2">
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink>
        <RouterLink to="/contact">Contact</RouterLink>
        <template v-if="isAuthenticated">
          <RouterLink to="/create-blog"> Create Blog </RouterLink>
          <RouterLink to="/manage-categories"> Categories </RouterLink>

          <a-button type="link" class="p-0!" @click="logout"> Logout </a-button>
        </template>

        <RouterLink v-else-if="showLoginOption" to="/login"> Login </RouterLink>
      </nav>
    </div>
  </header>
</template>
