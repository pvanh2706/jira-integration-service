<template>
  <section class="page-stack">
    <div class="toolbar">
      <div>
        <h2>Settings</h2>
        <p>Cau hinh local cho API base URL va internal auth token.</p>
      </div>
      <div class="button-row">
        <el-button @click="resetDefaults">Reset defaults</el-button>
        <el-button type="primary" :disabled="!hasChanges" @click="saveSettings">
          Save settings
        </el-button>
      </div>
    </div>

    <div class="surface">
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <el-form-item label="API base URL" prop="apiBaseUrl">
          <el-input v-model="form.apiBaseUrl" placeholder="/api" />
          <p class="helper-text">
            Dung `/api` khi chay Vite dev proxy. Co the doi sang full URL nhu
            `http://localhost:5016/api` neu can.
          </p>
        </el-form-item>
        <el-form-item label="Internal auth token" prop="internalAuthToken">
          <el-input v-model="form.internalAuthToken" show-password placeholder="dev-internal-token" />
          <p class="helper-text">
            Header `X-Internal-Auth` se duoc gan tu gia tri nay cho moi API request.
          </p>
        </el-form-item>
      </el-form>
      <el-alert
        title="No-login mode chi phu hop dev/internal. Sau nay khi co login, token nay nen duoc thay bang auth flow that."
        type="info"
        show-icon
        :closable="false"
      />
    </div>
  </section>
</template>

<script setup lang="ts">
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { storeToRefs } from 'pinia'
import { computed, reactive, ref } from 'vue'

import {
  DEFAULT_API_BASE_URL,
  DEFAULT_INTERNAL_AUTH_TOKEN,
  useAppSettingsStore,
} from '../stores/appSettings'

const settingsStore = useAppSettingsStore()
const { apiBaseUrl, internalAuthToken } = storeToRefs(settingsStore)
const formRef = ref<FormInstance>()

const form = reactive({
  apiBaseUrl: apiBaseUrl.value,
  internalAuthToken: internalAuthToken.value,
})

const rules: FormRules = {
  apiBaseUrl: [
    {
      required: true,
      message: 'API base URL is required.',
      trigger: 'blur',
    },
  ],
  internalAuthToken: [
    {
      required: true,
      message: 'Internal auth token is required.',
      trigger: 'blur',
    },
  ],
}

const hasChanges = computed(
  () =>
    form.apiBaseUrl.trim() !== apiBaseUrl.value ||
    form.internalAuthToken.trim() !== internalAuthToken.value,
)

async function saveSettings() {
  await formRef.value?.validate()

  settingsStore.updateSettings({
    apiBaseUrl: form.apiBaseUrl,
    internalAuthToken: form.internalAuthToken,
  })

  form.apiBaseUrl = apiBaseUrl.value
  form.internalAuthToken = internalAuthToken.value

  ElMessage.success('Settings saved.')
}

function resetDefaults() {
  settingsStore.resetSettings()
  form.apiBaseUrl = DEFAULT_API_BASE_URL
  form.internalAuthToken = DEFAULT_INTERNAL_AUTH_TOKEN

  ElMessage.success('Default settings restored.')
}
</script>
