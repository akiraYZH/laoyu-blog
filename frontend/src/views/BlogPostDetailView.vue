<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { MdPreview } from 'md-editor-v3'
import { useBlogPosts } from '@/composables/useBlogPosts'
import 'md-editor-v3/lib/preview.css'

const route = useRoute()
const router = useRouter()
const { currentPost, loading, loadError: error, getPost } = useBlogPosts()
const notFound = ref(false)

function goBack() {
  if (window.history.state?.back) {
    router.back()
    return
  }

  router.push({ name: 'home' })
}

const formattedCreatedAt = computed(() => {
  if (!currentPost.value) {
    return ''
  }

  return new Intl.DateTimeFormat('en', {
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

    const post = await getPost(slug)
    notFound.value = post === null
  },
  { immediate: true },
)
</script>

<template>
  <main>
    <div class="mb-5">
      <a-button @click="goBack"> ← Previous </a-button>
    </div>

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
