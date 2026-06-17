<template>
  <el-alert
    v-if="type === 'error'"
    :title="title"
    :description="description"
    type="error"
    show-icon
    :closable="false"
  />
  <el-alert
    v-else-if="title"
    :title="title"
    :description="description"
    :type="type"
    show-icon
    :closable="false"
  />
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'success' | 'warning' | 'info' | 'error'
    title?: string
    description?: string
  }>(),
  {
    type: 'info',
    title: '',
    description: '',
  },
)
</script>
