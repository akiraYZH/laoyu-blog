<script setup lang="ts">
import CategoryFilter from '@/components/Category/CategoryFilter.vue'
import Header from './components/Header.vue'

defineOptions({ name: 'AppLayout' })
</script>

<template>
  <div class="min-h-screen bg-slate-50">
    <Header />
    <CategoryFilter />

    <main class="mx-auto max-w-6xl px-6 py-8">
      <slot></slot>
    </main>
  </div>
</template>
