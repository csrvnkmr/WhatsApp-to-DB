import { createRouter, createWebHistory } from 'vue-router'

import adminRoutes from '@/admin/router/adminRoutes'

import HomeView from '@/views/HomeView.vue'

const router = createRouter({

  history: createWebHistory(import.meta.env.BASE_URL),

  routes: [

    ...adminRoutes,

    {
      path: '/',
      component: HomeView
    }
  ],
})

export default router
