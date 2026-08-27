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
        <p class="line-clamp-3">
          {{ post.content }}
        </p>
      </a-card>
    </RouterLink>
  </article>
</template>
