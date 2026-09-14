<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useBlogPosts } from '@/composables/useBlogPosts'
import BlogPostCard from './BlogPostCard.vue'
import FolderCard from '@/components/FolderCard.vue'
import { useAuth } from '@/composables/useAuth'

const props = defineProps<{
  categorySlug: string
}>()

const route = useRoute()
const router = useRouter()

const {
  posts,
  loading,
  loadError: error,
  page,
  pageSize,
  totalItems,
  getPosts,
  getCategoryTags,
  publishPost,
  unpublishPost,
  deletePost,
} = useBlogPosts()
const { isAuthenticated } = useAuth()

const categoryTags = ref<string[]>([])
const currentTag = ref<string | undefined>(undefined)

watch(
  () => [props.categorySlug, route.query.tag],
  async ([categorySlug, tag]) => {
    currentTag.value = (tag as string) || undefined

    if (currentTag.value) {
      // If we are inside a tag folder, we fetch posts for this tag, untaggedOnly is false
      void getPosts(1, pageSize.value, categorySlug as string, currentTag.value, false)
    } else {
      // If we are at the root of the category
      // 1. Fetch tags (folders)
      categoryTags.value = await getCategoryTags(categorySlug as string)
      // 2. Fetch untagged posts (cards)
      void getPosts(1, pageSize.value, categorySlug as string, undefined, true)
    }
  },
  { immediate: true },
)

const handlePageChange = (requestedPage: number, requestedPageSize: number) => {
  const isUntaggedOnly = !currentTag.value
  getPosts(requestedPage, requestedPageSize, props.categorySlug, currentTag.value, isUntaggedOnly)
}

const handlePublish = async (id: number) => {
  await publishPost(id)
}

const handleUnpublish = async (id: number) => {
  await unpublishPost(id)
}

const handleDelete = async (id: number) => {
  const deleted = await deletePost(id)

  if (deleted && posts.value.length === 0 && page.value > 1) {
    const isUntaggedOnly = !currentTag.value
    await getPosts(
      page.value - 1,
      pageSize.value,
      props.categorySlug,
      currentTag.value,
      isUntaggedOnly,
    )
  }
}

const goBackToCategory = () => {
  router.push({ name: 'categoryPosts', params: { slug: props.categorySlug } })
}
</script>

<template>
  <section>
    <div class="mb-5 flex items-center justify-between">
      <h1 class="text-2xl font-bold flex items-center gap-2">
        <span
          v-if="currentTag"
          class="text-slate-500 hover:text-emerald-600 cursor-pointer"
          @click="goBackToCategory"
        >
          {{ props.categorySlug }}
        </span>
        <span v-else>Blog Posts</span>

        <span v-if="currentTag" class="text-slate-400">/</span>
        <span v-if="currentTag" class="text-emerald-600">{{ currentTag }}</span>
      </h1>
    </div>

    <p v-if="loading">Loading...</p>

    <p v-else-if="error">
      {{ error }}
    </p>

    <div v-else>
      <a-space direction="vertical" :size="20" class="w-full">
        <div class="grid gap-5 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
          <!-- Folders -->
          <template v-if="!currentTag">
            <FolderCard
              v-for="tag in categoryTags"
              :key="tag"
              :name="tag"
              :to="{
                name: 'categoryPosts',
                params: { slug: props.categorySlug },
                query: { tag: tag },
              }"
            />
          </template>

          <!-- Cards -->
          <div v-for="post in posts" :key="post.id" class="flex flex-col gap-2">
            <div v-if="isAuthenticated" class="flex items-center justify-between">
              <a-tag :color="post.status === 'Draft' ? 'orange' : 'green'" class="m-0!">
                {{ post.status }}
              </a-tag>

              <a-space>
                <a-button
                  v-if="post.status === 'Draft'"
                  type="primary"
                  @click="handlePublish(post.id)"
                >
                  Publish
                </a-button>

                <a-popconfirm
                  v-if="post.status === 'Published'"
                  title="Unpublish this post?"
                  :description="`This will hide ${post.title} from visitors.`"
                  ok-text="Unpublish"
                  cancel-text="Cancel"
                  @confirm="handleUnpublish(post.id)"
                >
                  <a-button>Unpublish</a-button>
                </a-popconfirm>

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

        <p
          v-if="posts.length === 0 && (!categoryTags || categoryTags.length === 0)"
          class="text-slate-500"
        >
          No posts available.
        </p>

        <a-flex v-if="totalItems > 0" justify="center" class="mt-8">
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
