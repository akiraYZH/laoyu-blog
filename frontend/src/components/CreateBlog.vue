<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { MdEditor } from 'md-editor-v3'
import { message } from 'ant-design-vue'
import { useUploadImage } from '@/composables/useUploadImage'
import { useBlogPostStore } from '@/stores/blogPost.ts'
import { ApiRequestError } from '@/stores/functions/readError'
import 'md-editor-v3/lib/style.css'

interface Props {
  createPostCallback?: () => void
}

const props = withDefaults(defineProps<Props>(), {
  createPostCallback: () => {},
})

const { createPostCallback } = props
const title = ref('')
const slug = ref('')
const text = ref('# Hello Editor')
const categoryNames = ref<string[]>([])
const { uploadImage } = useUploadImage()
const blogPostStore = useBlogPostStore()
const { categories, loading } = storeToRefs(blogPostStore)

const categoryOptions = computed(() =>
  categories.value.map((category) => ({
    label: category.name,
    value: category.name,
  })),
)

onMounted(() => {
  blogPostStore.fetchCategories()
})

watch(title, () => {
  slug.value = title.value.toLocaleLowerCase().split(' ').join('-')
})

const createBlog = async () => {
  try {
    await blogPostStore.createBlogPost({
      title: title.value,
      slug: slug.value,
      content: text.value,
      categoryNames: categoryNames.value,
    })

    createPostCallback()
  } catch (caughtError) {
    if (caughtError instanceof ApiRequestError) {
      const errorMessages = Object.values(caughtError.validationErrors).flat()

      if (errorMessages.length > 0) {
        errorMessages.forEach((errorMessage) => {
          message.error(errorMessage)
        })

        return
      }

      message.error(caughtError.message)
      return
    }

    message.error(
      caughtError instanceof Error ? caughtError.message : 'Failed to create blog post.',
    )
  }
}
</script>

<template>
  <div class="mb-5">
    <a-typography-title :level="3">Title</a-typography-title>
    <a-input v-model:value="title" placeholder="Title"></a-input>
  </div>
  <div class="mb-5">
    <a-typography-title :level="3">Slug</a-typography-title>
    <a-input v-model:value="slug" placeholder="Slug"></a-input>
  </div>
  <div class="mb-5">
    <a-typography-title :level="3">Categories</a-typography-title>
    <a-select
      v-model:value="categoryNames"
      mode="tags"
      class="w-full"
      placeholder="Select or create categories"
      :options="categoryOptions"
    />
  </div>
  <div class="mb-5">
    <a-typography-title :level="3">Content</a-typography-title>
    <MdEditor v-model="text" :on-upload-img="uploadImage" />
  </div>
  <div>
    <a-button type="primary" :loading="loading" @click="createBlog">Create post</a-button>
  </div>
</template>
