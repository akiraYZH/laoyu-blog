<script setup lang="ts">
import { watch } from 'vue'
import { useBlogPosts } from '@/composables/useBlogPosts'
import BlogPostCard from './BlogPostCard.vue'

const props = defineProps<{
  categorySlug: string
}>()

const {
  posts,
  loading,
  loadError: error,
  page,
  pageSize,
  totalItems,
  getPosts,
  deletePost,
} = useBlogPosts()

watch(
  () => props.categorySlug,
  (categorySlug) => {
    void getPosts(1, pageSize.value, categorySlug)
  },
  { immediate: true },
)

const handlePageChange = (requestedPage: number, requestedPageSize: number) => {
  getPosts(requestedPage, requestedPageSize, props.categorySlug)
}

const handleDelete = async (id: number) => {
  const deleted = await deletePost(id)

  if (deleted && posts.value.length === 0 && page.value > 1) {
    await getPosts(page.value - 1, pageSize.value, props.categorySlug)
  }
}
</script>

<template>
  <section>
    <h1 class="mb-5 text-2xl font-bold">Blog Posts</h1>

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
              <a-space>
                <RouterLink
                  :to="{
                    name: 'updateBlog',
                    params: { slug: post.slug },
                  }"
                >
                  <a-button>Edit</a-button>
                </RouterLink>

                <a-popconfirm
                  title="Delete this post?"
                  :description="`This will permanently delete ${post.title}.`"
                  ok-text="Delete"
                  cancel-text="Cancel"
                  :ok-button-props="{ danger: true }"
                  @confirm="handleDelete(post.id)"
                >
                  <a-button danger>Delete</a-button>
                </a-popconfirm>
              </a-space>
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
