import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

import adminRoutes from '@/admin/router/adminRoutes'

import HomeView from '@/views/HomeView.vue'

const router = createRouter({

  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [

    ...adminRoutes,

    {
      path: '/',
      component: HomeView
    },
    {
      path: '/index.html',
      redirect: '/'
    }
  ],
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const requiresAdmin = to.matched.some(record => record.meta?.requiresAdmin)

  const role = authStore.role || localStorage.getItem('role') || ''
  const username = authStore.userName || localStorage.getItem('username') || ''

  if (requiresAdmin && role.toLowerCase() !== 'admin' && username.toLowerCase() !== 'admin') {
    next({ path: '/' })
    return
  }

  next()
})

export default router
