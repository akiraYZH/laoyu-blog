<script setup lang="ts">
import { onMounted } from 'vue'
import { useBlogPosts } from '@/composables/useBlogPosts'
import BlogPostCard from './BlogPostCard.vue'

const { posts, loading, loadError: error, page, pageSize, totalItems, getPosts } = useBlogPosts()

onMounted(() => {
  getPosts()
})

const handlePageChange = (requestedPage: number, requestedPageSize: number) => {
  getPosts(requestedPage, requestedPageSize)
}
</script>

<template>
  <section>
    <h1 class="text-2xl font-bold mb-5">Blog Posts</h1>
    <p v-if="loading">Loading...</p>

    <p v-else-if="error">
      {{ error }}
    </p>

    <p v-else-if="posts.length === 0">No posts available.</p>

    <div v-else>
      <a-space direction="vertical" :size="20">
        <div class="grid gap-5 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
          <div v-for="post in posts" :key="post.id" class="flex flex-col gap-2">
            <div class="flex justify-end">
              <RouterLink
                :to="{
                  name: 'updateBlog',
                  params: { slug: post.slug },
                }"
              >
                <a-button>Edit</a-button>
              </RouterLink>
            </div>

            <BlogPostCard :post="post" />
          </div>
        </div>
        <a-flex justify="center">
          <a-pagination
            v-model:current="page"
            v-model:page-size="pageSize"
            :total="totalItems"
            :show-size-changer="true"
            @change="handlePageChange"
          />
        </a-flex>
      </a-space>
    </div>
  </section>
</template>
