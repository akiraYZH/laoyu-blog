<script setup lang="ts">
import BlogPostList from '@/components/BlogPost/BlogPostList.vue'

const props = defineProps<{
  slug: string
}>()
</script>

<template>
  <main>
    <BlogPostList :category-slug="props.slug" />
  </main>
</template>
