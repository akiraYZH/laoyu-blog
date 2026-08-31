<script setup lang="ts">
import { useRouter } from 'vue-router'
import CreateBlog from '@/components/CreateBlog.vue'

const router = useRouter()

const handleCreated = () => {
  router.push({ name: 'home' })
}
</script>

<template>
  <CreateBlog @created="handleCreated" />
</template>
