<script setup lang="ts">
const issuesUrl = 'https://github.com/akiraYZH/laoyu-blog-backend/issues'
</script>

<template>
  <article class="mx-auto max-w-3xl">
    <p class="text-sm font-semibold uppercase tracking-widest text-emerald-700">Contact</p>
    <h1 class="mt-3 text-4xl font-bold tracking-tight text-slate-950">Questions and feedback</h1>
    <p class="mt-6 text-lg leading-8 text-slate-700">
      Feedback is welcome when an article is unclear, a code example no longer works, or you find an
      accessibility, privacy, or security concern.
    </p>

    <section class="mt-10 rounded-2xl border border-slate-200 bg-white p-7 shadow-sm">
      <h2 class="text-xl font-bold text-slate-900">Use the public issue tracker</h2>
      <p class="mt-3 leading-7 text-slate-700">
        Open a GitHub issue with the relevant page URL, what you expected to happen, and what you
        observed. For code problems, include a minimal reproducible example when possible.
      </p>
      <a
        class="mt-5 inline-flex rounded-lg bg-slate-900 px-5 py-3 font-semibold text-white hover:bg-slate-700"
        :href="issuesUrl"
        target="_blank"
        rel="noopener noreferrer"
      >
        Open a GitHub issue
      </a>
    </section>

    <section class="mt-10" aria-labelledby="safe-contact-heading">
      <h2 id="safe-contact-heading" class="text-xl font-bold text-slate-900">
        Please keep reports safe
      </h2>
      <p class="mt-3 leading-7 text-slate-700">
        Do not post passwords, access tokens, private keys, personal records, or other confidential
        information in a public issue. For questions about site data practices, review the
        <RouterLink class="font-semibold text-emerald-800 underline" to="/privacy">
          Privacy Policy</RouterLink
        >.
      </p>
    </section>
  </article>
</template>
