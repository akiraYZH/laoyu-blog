<script setup lang="ts">
import { ref, watch } from 'vue'
import { MdEditor } from 'md-editor-v3'
import { useUploadImage } from '@/composables/useUploadImage'
import { useBlogPostStore } from '@/stores/blogPost.ts'
import 'md-editor-v3/lib/style.css'

const title = ref('')
const slug = ref('')
const text = ref('# Hello Editor')
const { uploadImage } = useUploadImage()
const { createBlogPost } = useBlogPostStore()

watch(title, () => {
  slug.value = title.value.toLocaleLowerCase().split(' ').join('-')
})

const createBlog = () => {
  createBlogPost({ title: title.value, slug: slug.value, content: text.value })
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
  <div>
    <a-typography-title :level="3">Content</a-typography-title>
    <MdEditor v-model="text" :on-upload-img="uploadImage" />
  </div>
  <div>
    <a-button type="primary" @click="createBlog">Primary</a-button>
  </div>
</template>
