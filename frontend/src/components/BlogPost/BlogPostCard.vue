<script setup lang="ts">
import type { BlogPost } from '@/types'

defineProps<{
  post: BlogPost
}>()
</script>

<template>
  <article>
    <RouterLink
      :to="{
        name: 'blogDetail',
        params: { slug: post.slug },
      }"
    >
      <a-card :title="post.title" :bordered="false" class="w-full">
        <div
          v-if="post.categories.length > 0"
          class="mb-3 flex flex-wrap gap-2"
          aria-label="Article categories"
        >
          <a-tag v-for="category in post.categories" :key="category.id" color="blue" class="m-0!">
            {{ category.name }}
          </a-tag>
        </div>

        <p class="line-clamp-3">
          {{ post.content }}
        </p>
      </a-card>
    </RouterLink>
  </article>
</template>
