import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/about',
      name: 'about',
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
      component: () => import('../views/AboutView.vue'),
    },
    {
      path: '/create-blog',
      name: 'createBlog',
      component: () => import('../views/CreateBlogView.vue'),
    },
    {
      path: '/blogs/:slug',
      name: 'blogDetail',
      component: () => import('../views/BlogPostDetailView.vue'),
    },
    {
      path: '/blogs/:slug/edit',
      name: 'updateBlog',
      component: () => import('../views/UpdateBlogView.vue'),
    },
  ],
})

export default router
