<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useBlogPosts } from '@/composables/useBlogPosts'

const router = useRouter()
const { categories, selectedCategorySlug, getCategories } = useBlogPosts()

onMounted(() => {
  void getCategories()
})

const handleCategorySelect = (categorySlug: string | null) => {
  void router.push({
    name: 'home',
    query: categorySlug ? { category: categorySlug } : {},
  })
}
</script>

<template>
  <nav aria-label="Blog categories" class="overflow-x-auto bg-slate-900">
    <div class="flex min-w-max items-stretch px-2">
      <button
        type="button"
        class="border-b-2 px-4 py-3 text-sm font-semibold uppercase tracking-wide transition-colors"
        :class="
          selectedCategorySlug === null
            ? 'border-emerald-400 bg-slate-800 text-white!'
            : 'border-transparent text-slate-200! hover:bg-slate-800 hover:text-white!'
        "
        :aria-pressed="selectedCategorySlug === null"
        @click="handleCategorySelect(null)"
      >
        All
      </button>

      <button
        v-for="category in categories"
        :key="category.id"
        type="button"
        class="border-b-2 px-4 py-3 text-sm font-semibold uppercase tracking-wide transition-colors"
        :class="
          selectedCategorySlug === category.slug
            ? 'border-emerald-400 bg-slate-800 text-white!'
            : 'border-transparent text-slate-200! hover:bg-slate-800 hover:text-white!'
        "
        :aria-pressed="selectedCategorySlug === category.slug"
        @click="handleCategorySelect(category.slug)"
      >
        {{ category.name }}
      </button>
    </div>
  </nav>
</template>
