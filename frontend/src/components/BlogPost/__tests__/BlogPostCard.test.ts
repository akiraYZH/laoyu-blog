import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import BlogPostCard from '@/components/BlogPost/BlogPostCard.vue'
import type { BlogPost } from '@/types'

const post: BlogPost = {
  id: 7,
  slug: 'testing-vue-components',
  title: 'Testing Vue Components',
  content: 'This content should be visible inside the article card.',
  categories: [
    { id: 1, name: 'Vue', slug: 'vue' },
    { id: 2, name: 'Testing', slug: 'testing' },
  ],
  tags: [],
  status: 'Published',
  publishedAtUtc: '2026-09-11T12:00:00Z',
  createdAtUtc: '2026-09-10T12:00:00Z',
}

describe('BlogPostCard', () => {
  it('renders the post content, categories, and detail route', () => {
    const wrapper = mount(BlogPostCard, {
      props: { post },
      global: {
        stubs: {
          RouterLink: {
            name: 'RouterLink',
            props: ['to'],
            template: '<a><slot /></a>',
          },
          'a-card': {
            props: ['title'],
            template: '<section><h2>{{ title }}</h2><slot /></section>',
          },
          'a-tag': {
            template: '<span><slot /></span>',
          },
        },
      },
    })

    expect(wrapper.text()).toContain(post.title)
    expect(wrapper.text()).toContain(post.content)
    expect(wrapper.text()).toContain('Vue')
    expect(wrapper.text()).toContain('Testing')
    expect(wrapper.getComponent({ name: 'RouterLink' }).props('to')).toEqual({
      name: 'blogDetail',
      params: { slug: post.slug },
    })
  })
})
