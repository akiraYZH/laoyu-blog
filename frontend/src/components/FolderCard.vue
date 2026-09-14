<script setup lang="ts">
import { RouterLink } from 'vue-router'
import type { RouteLocationRaw } from 'vue-router'

defineProps<{
  name: string
  to: RouteLocationRaw
}>()
</script>

<template>
  <RouterLink
    :to="to"
    class="group flex min-h-28 flex-col justify-between rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:-translate-y-0.5 hover:border-emerald-400 hover:shadow-md"
  >
    <svg
      class="h-9 w-9 text-amber-500 transition group-hover:text-amber-600"
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden="true"
    >
      <path
        d="M3 5.25A2.25 2.25 0 0 1 5.25 3h4.129c.597 0 1.169.237 1.591.659l1.122 1.122c.14.141.331.219.53.219h6.128A2.25 2.25 0 0 1 21 7.25v9.5A2.25 2.25 0 0 1 18.75 19H5.25A2.25 2.25 0 0 1 3 16.75V5.25Z"
      />
    </svg>

    <span class="mt-3 font-semibold text-slate-800 group-hover:text-emerald-700 break-words">
      {{ name }}
    </span>
  </RouterLink>
</template>
