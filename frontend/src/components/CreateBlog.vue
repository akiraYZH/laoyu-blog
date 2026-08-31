<script setup lang="ts">
import BlogPostForm from '@/components/BlogPost/BlogPostForm.vue'
import { useBlogPosts } from '@/composables/useBlogPosts'
import type { BlogPostInput } from '@/types'

const emit = defineEmits<{
  created: []
}>()

const { createPost, loading } = useBlogPosts()

const createBlog = async (input: BlogPostInput) => {
  const createdPost = await createPost(input)

  if (createdPost) {
    emit('created')
  }
}
</script>

<template>
  <BlogPostForm
    submit-label="Create post"
    auto-generate-slug
    :loading="loading"
    @submit="createBlog"
  />
</template>
