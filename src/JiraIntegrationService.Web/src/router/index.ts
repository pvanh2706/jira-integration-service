import { createRouter, createWebHistory } from 'vue-router'

import CreateIssuePage from '../pages/CreateIssuePage.vue'
import IssueStatusPage from '../pages/IssueStatusPage.vue'
import ProductDetailPage from '../pages/ProductDetailPage.vue'
import ProductsPage from '../pages/ProductsPage.vue'
import SettingsPage from '../pages/SettingsPage.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/products',
    },
    {
      path: '/products',
      name: 'products',
      component: ProductsPage,
      meta: { title: 'Products' },
    },
    {
      path: '/products/:code',
      name: 'product-detail',
      component: ProductDetailPage,
      meta: { title: 'Product detail' },
    },
    {
      path: '/issues/create',
      name: 'create-issue',
      component: CreateIssuePage,
      meta: { title: 'Create issue' },
    },
    {
      path: '/issues/status',
      name: 'issue-status',
      component: IssueStatusPage,
      meta: { title: 'Issue status' },
    },
    {
      path: '/settings',
      name: 'settings',
      component: SettingsPage,
      meta: { title: 'Settings' },
    },
  ],
})

export default router
