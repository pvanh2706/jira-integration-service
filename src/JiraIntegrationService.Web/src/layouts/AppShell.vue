<template>
  <el-container class="app-shell">
    <el-aside width="264px" class="app-shell__sidebar">
      <router-link to="/products" class="app-shell__brand" aria-label="Jira Integration Service">
        <span class="app-shell__brand-mark">JI</span>
        <span>
          <strong>Jira Integration</strong>
          <small>Admin console</small>
        </span>
      </router-link>

      <el-menu :default-active="activeMenu" router class="app-shell__menu">
        <el-menu-item index="/products">
          <el-icon><Grid /></el-icon>
          <span>Products</span>
        </el-menu-item>
        <el-menu-item index="/issues/create">
          <el-icon><Tickets /></el-icon>
          <span>Create issue</span>
        </el-menu-item>
        <el-menu-item index="/issues/status">
          <el-icon><Operation /></el-icon>
          <span>Issue status</span>
        </el-menu-item>
        <el-menu-item index="/settings">
          <el-icon><Setting /></el-icon>
          <span>Settings</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container class="app-shell__body">
      <el-header class="app-shell__header">
        <div>
          <el-breadcrumb separator="/" class="app-shell__breadcrumb">
            <el-breadcrumb-item v-for="item in breadcrumbs" :key="item.path" :to="item.path">
              {{ item.label }}
            </el-breadcrumb-item>
          </el-breadcrumb>
          <p class="app-shell__eyebrow">Jira Server v2</p>
          <h1>{{ pageTitle }}</h1>
        </div>
        <el-tag round type="success">No login mode</el-tag>
      </el-header>

      <el-main class="app-shell__main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { Grid, Operation, Setting, Tickets } from '@element-plus/icons-vue'
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const activeMenu = computed(() => {
  if (route.path.startsWith('/products')) {
    return '/products'
  }

  return route.path
})

const pageTitle = computed(() => String(route.meta.title ?? 'Jira Integration'))

const breadcrumbs = computed(() => {
  if (route.path.startsWith('/products/') && route.params.code) {
    return [
      { label: 'Products', path: '/products' },
      { label: String(route.params.code), path: route.path },
    ]
  }

  if (route.path.startsWith('/issues/create')) {
    return [
      { label: 'Issues', path: '/issues/create' },
      { label: 'Create', path: '/issues/create' },
    ]
  }

  if (route.path.startsWith('/issues/status')) {
    return [
      { label: 'Issues', path: '/issues/status' },
      { label: 'Status', path: '/issues/status' },
    ]
  }

  if (route.path.startsWith('/settings')) {
    return [{ label: 'Settings', path: '/settings' }]
  }

  return [{ label: 'Products', path: '/products' }]
})
</script>
