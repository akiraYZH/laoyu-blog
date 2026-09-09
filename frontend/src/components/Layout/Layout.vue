<script setup lang="ts">
import CategoryFilter from '@/components/Category/CategoryFilter.vue'
import Header from './components/Header.vue'
import Footer from './components/Footer.vue'

defineOptions({ name: 'AppLayout' })
</script>

<template>
  <div class="flex min-h-screen flex-col bg-slate-50 text-slate-800">
    <Header />
    <CategoryFilter />

    <main id="main-content" class="mx-auto w-full max-w-6xl flex-1 px-6 py-10">
      <slot></slot>
    </main>

    <Footer />
  </div>
</template>
