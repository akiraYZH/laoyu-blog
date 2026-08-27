<script setup lang="ts">
import Header from './components/Header.vue'
</script>

<template>
  <div class="min-h-screen bg-slate-50">
    <Header />

    <main class="mx-auto max-w-6xl px-6 py-8">
      <slot></slot>
    </main>
  </div>
</template>
