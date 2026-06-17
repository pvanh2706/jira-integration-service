<template>
  <section class="page-stack">
    <div class="toolbar">
      <div>
        <h2>Issue status</h2>
        <p>Check va update status cua Jira issue theo mapping noi bo.</p>
      </div>
      <div class="button-row">
        <el-button :disabled="!canQueryStatus" :loading="getStatusMutation.isPending.value" @click="getStatus">
          Check status
        </el-button>
        <el-button
          type="primary"
          :disabled="!canUpdateStatus"
          :loading="updateStatusMutation.isPending.value"
          @click="updateStatus"
        >
          Update status
        </el-button>
      </div>
    </div>

    <ApiResultPanel
      v-if="statusResultTitle"
      :type="statusResultType"
      :title="statusResultTitle"
      :description="statusResultDescription"
    />

    <div class="surface">
      <el-alert
        v-if="queryError"
        :title="queryError"
        type="error"
        show-icon
        :closable="false"
        class="mb-16"
      />

      <el-form label-position="top">
        <el-row :gutter="16">
          <el-col :xs="24" :md="8">
            <el-form-item label="Product" required>
              <el-select v-model="form.productCode" filterable placeholder="Select product" class="full-width">
                <el-option
                  v-for="product in activeProducts"
                  :key="product.code"
                  :label="`${product.code} - ${product.name}`"
                  :value="product.code"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="8">
            <el-form-item label="Issue type">
              <el-select
                v-model="form.issueTypeCode"
                :disabled="!form.productCode"
                clearable
                filterable
                placeholder="Optional"
                class="full-width"
              >
                <el-option
                  v-for="issueType in activeIssueTypes"
                  :key="issueType.issueTypeCode"
                  :label="issueType.issueTypeCode"
                  :value="issueType.issueTypeCode"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="8">
            <el-form-item label="Standard status">
              <el-select
                v-model="form.standardStatus"
                filterable
                allow-create
                default-first-option
                placeholder="Select status"
                class="full-width"
              >
                <el-option v-for="status in standardStatusOptions" :key="status" :label="status" :value="status" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira issue key">
              <el-input v-model="form.jiraIssueKey" placeholder="CRM-123" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira issue id">
              <el-input v-model="form.jiraIssueId" placeholder="10001" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <el-alert
        v-if="formWarning"
        :title="formWarning"
        type="warning"
        show-icon
        :closable="false"
      />

      <div v-if="statusMappings.length" class="status-suggestion-list">
        <h3>Status suggestions</h3>
        <el-table :data="statusMappings" size="small">
          <el-table-column prop="standardStatus" label="Standard status" min-width="150" />
          <el-table-column prop="jiraStatusName" label="Jira status" min-width="160" />
          <el-table-column label="Transition" min-width="200">
            <template #default="{ row }">
              {{ row.jiraTransitionId || '-' }} / {{ row.jiraTransitionName || '-' }}
            </template>
          </el-table-column>
          <el-table-column label="Use" width="90" align="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="form.standardStatus = row.standardStatus">
                Select
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useMutation, useQuery } from '@tanstack/vue-query'
import { ElMessage } from 'element-plus'
import { computed, reactive, watch } from 'vue'

import ApiResultPanel from '../components/ApiResultPanel.vue'
import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import { issuesApi } from '../services/issuesApi'
import type {
  GetIssueStatusRequest,
  UpdateIssueStatusRequest,
  UpdateIssueStatusResult,
} from '../types/issues'

const defaultStatuses = ['OPEN', 'IN_PROGRESS', 'WAITING', 'DONE', 'CANCELLED']

const form = reactive({
  productCode: '',
  issueTypeCode: '',
  jiraIssueKey: '',
  jiraIssueId: '',
  standardStatus: '',
})
const lastUpdateResult = reactive<{
  value?: UpdateIssueStatusResult
}>({})
const lastGetStatus = reactive<{
  value?: string
}>({})

const productsQuery = useQuery({
  queryKey: ['admin', 'products'],
  queryFn: adminApi.getProducts,
})

const issueTypesQuery = useQuery({
  queryKey: ['admin', 'issueTypes', computed(() => form.productCode)],
  queryFn: () => adminApi.getIssueTypes(form.productCode),
  enabled: computed(() => Boolean(form.productCode)),
})

const statusMappingsQuery = useQuery({
  queryKey: ['admin', 'statusMappings', computed(() => form.productCode), computed(() => form.issueTypeCode)],
  queryFn: () => adminApi.getStatusMappings(form.productCode, form.issueTypeCode),
  enabled: computed(() => Boolean(form.productCode && form.issueTypeCode)),
})

const getStatusMutation = useMutation({
  mutationFn: () => issuesApi.getIssueStatus(toGetStatusRequest()),
  onSuccess: (result) => {
    lastGetStatus.value = result.standardStatus
    lastUpdateResult.value = undefined
    ElMessage.success(`Current standard status: ${result.standardStatus}`)
  },
})

const updateStatusMutation = useMutation({
  mutationFn: () => issuesApi.updateIssueStatus(toUpdateStatusRequest()),
  onSuccess: (result) => {
    lastUpdateResult.value = result
    lastGetStatus.value = undefined
    ElMessage.success(`Status updated to ${result.standardStatus}.`)
  },
})

const activeProducts = computed(() => (productsQuery.data.value ?? []).filter((product) => product.isActive))
const activeIssueTypes = computed(() => (issueTypesQuery.data.value ?? []).filter((issueType) => issueType.isActive))
const statusMappings = computed(() => (statusMappingsQuery.data.value ?? []).filter((mapping) => mapping.isActive))
const standardStatusOptions = computed(() => {
  const values = new Set(defaultStatuses)
  for (const mapping of statusMappings.value) {
    values.add(mapping.standardStatus)
  }
  return Array.from(values)
})
const hasIssueIdentity = computed(() => Boolean(form.jiraIssueId.trim() || form.jiraIssueKey.trim()))
const canQueryStatus = computed(() => Boolean(form.productCode && hasIssueIdentity.value))
const canUpdateStatus = computed(
  () => Boolean(form.productCode && hasIssueIdentity.value && form.standardStatus.trim()),
)
const formWarning = computed(() => {
  if (!activeProducts.value.length && !productsQuery.isLoading.value) {
    return 'Chua co active product.'
  }
  if (form.productCode && !hasIssueIdentity.value) {
    return 'Nhap Jira issue key hoac Jira issue id.'
  }
  if (form.productCode && form.issueTypeCode && !statusMappings.value.length && !statusMappingsQuery.isLoading.value) {
    return 'Issue type nay chua co active status mapping.'
  }
  return ''
})
const queryError = computed(() => {
  const error =
    productsQuery.error.value ?? issueTypesQuery.error.value ?? statusMappingsQuery.error.value
  return error ? describeApiError(error, 'Khong tai duoc cau hinh status.') : ''
})
const statusResultTitle = computed(() => {
  if (lastGetStatus.value) {
    return `Current status: ${lastGetStatus.value}`
  }
  if (lastUpdateResult.value) {
    return `Updated to ${lastUpdateResult.value.standardStatus}`
  }
  return ''
})
const statusResultDescription = computed(() => {
  const result = lastUpdateResult.value
  if (!result) {
    return ''
  }
  return `Jira issue id: ${result.jiraIssueId ?? '-'}; key: ${result.jiraIssueKey ?? '-'}`
})
const statusResultType = computed<'success' | 'info'>(() => (lastUpdateResult.value ? 'success' : 'info'))

watch(
  activeProducts,
  (products) => {
    if (!form.productCode && products.length > 0) {
      form.productCode = products[0].code
    }
  },
  { immediate: true },
)

watch(
  activeIssueTypes,
  (issueTypes) => {
    if (form.issueTypeCode && !issueTypes.some((issueType) => issueType.issueTypeCode === form.issueTypeCode)) {
      form.issueTypeCode = ''
    }
  },
  { immediate: true },
)

watch(
  standardStatusOptions,
  (statuses) => {
    if (!form.standardStatus && statuses.length > 0) {
      form.standardStatus = statuses[0]
    }
  },
  { immediate: true },
)

async function getStatus() {
  if (!ensureIdentity()) {
    return
  }
  await getStatusMutation.mutateAsync()
}

async function updateStatus() {
  if (!ensureIdentity() || !form.standardStatus.trim()) {
    ElMessage.warning('Chon standard status truoc khi update.')
    return
  }
  await updateStatusMutation.mutateAsync()
}

function ensureIdentity() {
  if (!hasIssueIdentity.value) {
    ElMessage.warning('Nhap Jira issue key hoac Jira issue id.')
    return false
  }
  return true
}

function toGetStatusRequest(): GetIssueStatusRequest {
  return {
    productCode: form.productCode,
    jiraIssueId: optional(form.jiraIssueId),
    jiraIssueKey: optional(form.jiraIssueKey),
    issueTypeCode: optional(form.issueTypeCode),
  }
}

function toUpdateStatusRequest(): UpdateIssueStatusRequest {
  return {
    ...toGetStatusRequest(),
    standardStatus: form.standardStatus.trim(),
  }
}

function optional(value: string) {
  const trimmed = value.trim()
  return trimmed ? trimmed : undefined
}
</script>
