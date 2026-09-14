<script setup lang="ts">
import { useBlogPosts } from '@/composables/useBlogPosts'
import FolderCard from '@/components/FolderCard.vue'

const { categories } = useBlogPosts()
</script>

<template>
  <section v-if="categories.length > 0" aria-labelledby="category-folder-heading" class="mb-8">
    <h1 id="category-folder-heading" class="mb-4 text-2xl font-bold text-slate-900">
      Browse by category
    </h1>

    <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
      <FolderCard
        v-for="category in categories"
        :key="category.id"
        :name="category.name"
        :to="{ name: 'categoryPosts', params: { slug: category.slug } }"
      />
    </div>
  </section>
</template>
