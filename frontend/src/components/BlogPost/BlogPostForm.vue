<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { MdEditor } from 'md-editor-v3'
import { useBlogPosts } from '@/composables/useBlogPosts'
import { useUploadImage } from '@/composables/useUploadImage'
import type { BlogPostInput } from '@/types'
import 'md-editor-v3/lib/style.css'

interface Props {
  initialValues?: BlogPostInput | null
  submitLabel?: string
  autoGenerateSlug?: boolean
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  initialValues: null,
  submitLabel: 'Save post',
  autoGenerateSlug: false,
  loading: false,
})

const emit = defineEmits<{
  submit: [input: BlogPostInput]
}>()

const title = ref('')
const slug = ref('')
const content = ref('')
const categoryNames = ref<string[]>([])
const { uploadImage } = useUploadImage()
const { categories, getCategories } = useBlogPosts()

const categoryOptions = computed(() =>
  categories.value.map((category) => ({
    label: category.name,
    value: category.name,
  })),
)

onMounted(() => {
  getCategories()
})

watch(
  () => props.initialValues,
  (values) => {
    if (!values) {
      return
    }

    title.value = values.title
    slug.value = values.slug
    content.value = values.content
    categoryNames.value = [...values.categoryNames]
  },
  { immediate: true },
)

watch(title, (value) => {
  if (props.autoGenerateSlug) {
    slug.value = value.toLocaleLowerCase().trim().split(/\s+/).join('-')
  }
})

const submitForm = () => {
  emit('submit', {
    title: title.value,
    slug: slug.value,
    content: content.value,
    categoryNames: [...categoryNames.value],
  })
}
</script>

<template>
  <form @submit.prevent="submitForm">
    <div class="mb-5">
      <a-typography-title :level="3">Title</a-typography-title>
      <a-input v-model:value="title" placeholder="Title" />
    </div>

    <div class="mb-5">
      <a-typography-title :level="3">Slug</a-typography-title>
      <a-input v-model:value="slug" placeholder="Slug" />
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
      <MdEditor v-model="content" :on-upload-img="uploadImage" />
    </div>

    <a-button type="primary" html-type="submit" :loading="loading">
      {{ submitLabel }}
    </a-button>
  </form>
</template>
