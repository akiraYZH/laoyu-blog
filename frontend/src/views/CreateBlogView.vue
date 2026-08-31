<script setup lang="ts">
import CreateBlog from '@/components/CreateBlog.vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const createPostCallback = () => {
  router.push({ name: 'home' })
}
</script>

<template>
  <CreateBlog :createPostCallback="createPostCallback" />
</template>
