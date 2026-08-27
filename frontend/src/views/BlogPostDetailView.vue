<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useRoute } from 'vue-router'
import { MdPreview } from 'md-editor-v3'
import { useBlogPostStore } from '@/stores/blogPost'
import 'md-editor-v3/lib/preview.css'

const route = useRoute()
const blogPostStore = useBlogPostStore()
const { currentPost, loading, error } = storeToRefs(blogPostStore)
const notFound = ref(false)

const formattedCreatedAt = computed(() => {
  if (!currentPost.value) {
    return ''
  }

  return new Intl.DateTimeFormat('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date(currentPost.value.createdAtUtc))
})

watch(
  () => route.params.slug,
  async (slug) => {
    notFound.value = false

    if (typeof slug !== 'string') {
      notFound.value = true
      return
    }

    try {
      const post = await blogPostStore.loadPostBySlug(slug)
      notFound.value = post === null
    } catch {
      // Store 已经保存了供页面展示的错误信息。
    }
  },
  { immediate: true },
)
</script>

<template>
  <main>
    <p v-if="loading">Loading...</p>

    <p v-else-if="error">
      {{ error }}
    </p>

    <p v-else-if="notFound">Post not found.</p>

    <article v-else-if="currentPost">
      <header class="mb-8">
        <h1>{{ currentPost.title }}</h1>

        <time :datetime="currentPost.createdAtUtc">
          {{ formattedCreatedAt }}
        </time>
      </header>
      <MdPreview
        id="blog-post-preview"
        :model-value="currentPost.content"
        class="sm:px-12 lg:px-24 xl:px-48 w-full"
      />
    </article>
  </main>
</template>
