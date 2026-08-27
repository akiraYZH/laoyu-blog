<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>

<template>
  <header class="border-b border-slate-200 bg-white">
    <div class="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
      <RouterLink to="/" class="text-xl font-bold text-slate-900">Laoyu Blog</RouterLink>

      <nav class="flex items-center gap-6">
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink>
        <RouterLink to="/create-blog"> Create Blog </RouterLink>
      </nav>
    </div>
  </header>
</template>
