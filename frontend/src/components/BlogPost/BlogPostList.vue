<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useBlogPostStore } from '@/stores/blogPost'
import BlogPostCard from './BlogPostCard.vue'

const blogPostStore = useBlogPostStore()
const { posts, loading, error } = storeToRefs(blogPostStore)

onMounted(() => {
  blogPostStore.fetchPosts()
})
</script>

<template>
  <section>
    <h1 class="text-2xl font-bold mb-5">Blog Posts</h1>
    <p v-if="loading">Loading...</p>

    <p v-else-if="error">
      {{ error }}
    </p>

    <p v-else-if="posts.length === 0">No posts available.</p>

    <div v-else class="grid gap-5 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
      <BlogPostCard v-for="post in posts" :key="post.id" :post="post" />
    </div>
  </section>
</template>
