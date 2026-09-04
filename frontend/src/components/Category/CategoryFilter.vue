<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useBlogPosts } from '@/composables/useBlogPosts'

const route = useRoute()
const router = useRouter()
const { categories, getCategories } = useBlogPosts()

const isAllActive = computed(() => route.name === 'home')

const activeCategorySlug = computed(() =>
  route.name === 'categoryPosts' && typeof route.params.slug === 'string'
    ? route.params.slug
    : null,
)

onMounted(() => {
  void getCategories()
})

const handleCategorySelect = (categorySlug: string | null) => {
  void router.push(
    categorySlug ? { name: 'categoryPosts', params: { slug: categorySlug } } : { name: 'home' },
  )
}
</script>

<template>
  <nav aria-label="Blog categories" class="overflow-x-auto bg-slate-900">
    <div class="flex min-w-max items-stretch px-2">
      <button
        type="button"
        class="border-b-2 px-4 py-3 text-sm font-semibold uppercase tracking-wide transition-colors"
        :class="
          isAllActive
            ? 'border-emerald-400 bg-slate-800 text-white!'
            : 'border-transparent text-slate-200! hover:bg-slate-800 hover:text-white!'
        "
        :aria-pressed="isAllActive"
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
          activeCategorySlug === category.slug
            ? 'border-emerald-400 bg-slate-800 text-white!'
            : 'border-transparent text-slate-200! hover:bg-slate-800 hover:text-white!'
        "
        :aria-pressed="activeCategorySlug === category.slug"
        @click="handleCategorySelect(category.slug)"
      >
        {{ category.name }}
      </button>
    </div>
  </nav>
</template>
