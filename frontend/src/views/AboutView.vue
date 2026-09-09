<script setup lang="ts">
const repositoryUrl = 'https://github.com/akiraYZH/laoyu-blog-backend'
</script>

<template>
  <article class="mx-auto max-w-3xl">
    <p class="text-sm font-semibold uppercase tracking-widest text-emerald-700">About Laoyu Blog</p>
    <h1 class="mt-3 text-4xl font-bold tracking-tight text-slate-950">
      Practical engineering, documented honestly
    </h1>

    <p class="mt-6 text-lg leading-8 text-slate-700">
      Laoyu Blog is a hands-on learning project about designing and shipping a modern full-stack
      application. Instead of presenting isolated code snippets, the articles follow real features
      from the first design decision through implementation, debugging, testing, and deployment.
    </p>

    <section class="mt-12" aria-labelledby="topics-heading">
      <h2 id="topics-heading" class="text-2xl font-bold text-slate-900">What you will find here</h2>
      <div class="mt-6 grid gap-5 sm:grid-cols-3">
        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h3 class="font-semibold text-slate-900">Backend</h3>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            ASP.NET Core APIs, DTOs, services, EF Core, PostgreSQL, authentication, and testing.
          </p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h3 class="font-semibold text-slate-900">Frontend</h3>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            Vue, TypeScript, Pinia, routing, reusable components, and accessible interfaces.
          </p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-5">
          <h3 class="font-semibold text-slate-900">Delivery</h3>
          <p class="mt-2 text-sm leading-6 text-slate-600">
            Docker workflows, configuration, database migrations, verification, and deployment.
          </p>
        </div>
      </div>
    </section>

    <section class="mt-12" aria-labelledby="editorial-heading">
      <h2 id="editorial-heading" class="text-2xl font-bold text-slate-900">
        How the articles are written
      </h2>
      <p class="mt-4 leading-7 text-slate-700">
        Each tutorial focuses on one problem, explains why the chosen design exists, identifies the
        files being changed, and includes a way to verify the result. When an approach has a
        limitation or remains untested, the article should say so directly.
      </p>
    </section>

    <section
      class="mt-12 rounded-2xl border border-emerald-200 bg-emerald-50 p-6"
      aria-labelledby="open-project-heading"
    >
      <h2 id="open-project-heading" class="text-xl font-bold text-slate-900">
        An open learning project
      </h2>
      <p class="mt-3 leading-7 text-slate-700">
        The application source is public so readers can compare an article with the evolving
        implementation, report a problem, or suggest an improvement.
      </p>
      <a
        class="mt-4 inline-flex font-semibold text-emerald-800 underline underline-offset-4"
        :href="repositoryUrl"
        target="_blank"
        rel="noopener noreferrer"
      >
        View the project on GitHub
      </a>
    </section>
  </article>
</template>
