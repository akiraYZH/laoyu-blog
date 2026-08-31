<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BlogPostForm from '@/components/BlogPost/BlogPostForm.vue'
import { useBlogPosts } from '@/composables/useBlogPosts'
import type { BlogPost, BlogPostInput } from '@/types'

const route = useRoute()
const router = useRouter()
const { getPost, updatePost, loading, loadError } = useBlogPosts()
const post = ref<BlogPost | null>(null)
const notFound = ref(false)

const initialValues = computed<BlogPostInput | null>(() => {
  if (!post.value) {
    return null
  }

  return {
    title: post.value.title,
    slug: post.value.slug,
    content: post.value.content,
    categoryNames: post.value.categories.map((category) => category.name),
  }
})

watch(
  () => route.params.slug,
  async (slug) => {
    post.value = null
    notFound.value = false

    if (typeof slug !== 'string') {
      notFound.value = true
      return
    }

    const loadedPost = await getPost(slug)
    post.value = loadedPost
    notFound.value = loadedPost === null
  },
  { immediate: true },
)

async function updateBlog(input: BlogPostInput) {
  if (!post.value) {
    return
  }

  const updatedPost = await updatePost(post.value.id, input)

  if (!updatedPost) {
    return
  }

  await router.push({
    name: 'blogDetail',
    params: { slug: updatedPost.slug },
  })
}
</script>

<template>
  <main>
    <a-typography-title :level="2">Edit post</a-typography-title>

    <p v-if="loading && !post">Loading...</p>
    <p v-else-if="loadError">{{ loadError }}</p>
    <p v-else-if="notFound">Post not found.</p>

    <BlogPostForm
      v-else-if="initialValues"
      :initial-values="initialValues"
      submit-label="Update post"
      :loading="loading"
      @submit="updateBlog"
    />
  </main>
</template>
