<script setup lang="ts">
import { RouterLink } from 'vue-router'

defineOptions({ name: 'AppFooter' })

const currentYear = new Date().getFullYear()
const repositoryUrl = 'https://github.com/akiraYZH/laoyu-blog-backend'
</script>

<template>
  <footer class="border-t border-slate-200 bg-slate-950 text-slate-300">
    <div class="mx-auto grid max-w-6xl gap-8 px-6 py-10 md:grid-cols-[1.3fr_1fr]">
      <div>
        <RouterLink to="/" class="text-lg font-bold text-white">YZH Code</RouterLink>
        <p class="mt-3 max-w-xl text-sm leading-6 text-slate-400">
          Practical notes about building a full-stack blog with ASP.NET Core, PostgreSQL, Vue,
          testing, and container-based development.
        </p>
      </div>

      <nav aria-label="Footer navigation" class="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
        <RouterLink class="hover:text-white" to="/about">About</RouterLink>
        <RouterLink class="hover:text-white" to="/contact">Contact</RouterLink>
        <RouterLink class="hover:text-white" to="/privacy">Privacy Policy</RouterLink>
        <RouterLink class="hover:text-white" to="/terms">Terms of Use</RouterLink>
        <a class="hover:text-white" :href="repositoryUrl" target="_blank" rel="noopener noreferrer">
          Source code
        </a>
      </nav>
    </div>

    <div class="border-t border-slate-800 px-6 py-4 text-center text-xs text-slate-500">
      © {{ currentYear }} YZH Code. Built as a practical full-stack engineering project.
    </div>
  </footer>
</template>
