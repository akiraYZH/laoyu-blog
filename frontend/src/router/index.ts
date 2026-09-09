import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import { useAuthStore } from '@/stores/auth'

const defaultDescription =
  'Practical full-stack engineering tutorials built with ASP.NET Core, PostgreSQL, Vue, TypeScript, testing, and Docker.'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: {
        title: 'Laoyu Blog | Practical Full-Stack Engineering',
        description: defaultDescription,
      },
    },
    {
      path: '/categories/:slug',
      name: 'categoryPosts',
      component: () => import('../views/CategoryPostsView.vue'),
      props: true,
      meta: {
        title: 'Articles by Category | Laoyu Blog',
        description: 'Browse practical full-stack engineering tutorials by category.',
      },
    },
    {
      path: '/about',
      name: 'about',
      component: () => import('../views/AboutView.vue'),
      meta: {
        title: 'About | Laoyu Blog',
        description: 'Learn how Laoyu Blog documents full-stack engineering decisions and results.',
      },
    },
    {
      path: '/contact',
      name: 'contact',
      component: () => import('../views/ContactView.vue'),
      meta: {
        title: 'Contact | Laoyu Blog',
        description: 'Report a problem, suggest an improvement, or contact the Laoyu Blog project.',
      },
    },
    {
      path: '/privacy',
      name: 'privacy',
      component: () => import('../views/PrivacyPolicyView.vue'),
      meta: {
        title: 'Privacy Policy | Laoyu Blog',
        description: 'How Laoyu Blog handles site, advertising, and contact information.',
      },
    },
    {
      path: '/terms',
      name: 'terms',
      component: () => import('../views/TermsOfUseView.vue'),
      meta: {
        title: 'Terms of Use | Laoyu Blog',
        description: 'The terms governing use of Laoyu Blog articles, examples, and site features.',
      },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: {
        title: 'Admin Login | Laoyu Blog',
        description: 'Administrator access for Laoyu Blog.',
        noindex: true,
      },
    },
    {
      path: '/create-blog',
      name: 'createBlog',
      component: () => import('../views/CreateBlogView.vue'),
      meta: {
        title: 'Create Post | Laoyu Blog',
        description: 'Create a Laoyu Blog article.',
        requiresAuth: true,
        noindex: true,
      },
    },
    {
      path: '/blogs/:slug',
      name: 'blogDetail',
      component: () => import('../views/BlogPostDetailView.vue'),
      meta: {
        title: 'Article | Laoyu Blog',
        description: 'Read a practical full-stack engineering article on Laoyu Blog.',
      },
    },
    {
      path: '/blogs/:slug/edit',
      name: 'updateBlog',
      component: () => import('../views/UpdateBlogView.vue'),
      meta: {
        title: 'Edit Post | Laoyu Blog',
        description: 'Edit a Laoyu Blog article.',
        requiresAuth: true,
        noindex: true,
      },
    },
    {
      path: '/:pathMatch(.*)',
      name: 'notFound',
      component: () => import('../views/NotFoundView.vue'),
      meta: {
        title: 'Page Not Found | Laoyu Blog',
        description: 'The requested Laoyu Blog page could not be found.',
        noindex: true,
      },
    },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return {
      name: 'login',
      query: {
        redirect: to.fullPath,
      },
    }
  }

  if (to.name === 'login' && authStore.isAuthenticated) {
    return {
      name: 'home',
    }
  }
})

router.afterEach((to, _from, failure) => {
  if (failure) {
    return
  }

  document.title =
    typeof to.meta.title === 'string'
      ? to.meta.title
      : 'Laoyu Blog | Practical Full-Stack Engineering'

  const description = document.querySelector<HTMLMetaElement>('meta[name="description"]')

  if (description) {
    description.content =
      typeof to.meta.description === 'string' ? to.meta.description : defaultDescription
  }

  const robots = document.querySelector<HTMLMetaElement>('meta[name="robots"]')

  if (robots) {
    robots.content = to.meta.noindex === true ? 'noindex, nofollow' : 'index, follow'
  }
})

export default router
