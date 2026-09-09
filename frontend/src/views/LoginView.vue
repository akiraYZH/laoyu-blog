<script setup lang="ts">
import { reactive } from 'vue'
import type { LoginInput } from '@/types'
import { useAuth } from '@/composables/useAuth'

const form = reactive<LoginInput>({
  email: '',
  password: '',
})

const { submitting, login } = useAuth()

async function handleSubmit(): Promise<void> {
  await login({
    email: form.email,
    password: form.password,
  })
}
</script>

<template>
  <section class="mx-auto max-w-md">
    <div class="rounded-xl border border-slate-200 bg-white p-8 shadow-sm">
      <h1 class="mb-6 text-2xl font-bold text-slate-900">Admin Login</h1>

      <a-form :model="form" layout="vertical" @finish="handleSubmit">
        <a-form-item
          label="Email"
          name="email"
          :rules="[
            {
              required: true,
              message: 'Please enter your email.',
            },
            {
              type: 'email',
              message: 'Please enter a valid email.',
            },
          ]"
        >
          <a-input
            v-model:value="form.email"
            type="email"
            autocomplete="email"
            placeholder="admin@example.com"
          />
        </a-form-item>

        <a-form-item
          label="Password"
          name="password"
          :rules="[
            {
              required: true,
              message: 'Please enter your password.',
            },
          ]"
        >
          <a-input-password
            v-model:value="form.password"
            autocomplete="current-password"
            placeholder="Password"
          />
        </a-form-item>

        <a-button type="primary" html-type="submit" :loading="submitting" block> Login </a-button>
      </a-form>
    </div>
  </section>
</template>
