<template>
  <section class="page-stack">
    <div class="toolbar">
      <div>
        <h2>Create issue</h2>
        <p>Form dong sinh tu field mappings va preview Jira payload dung logic server.</p>
      </div>
      <div class="button-row">
        <el-button
          :disabled="!canUseForm"
          :loading="previewMutation.isPending.value"
          @click="previewPayload"
        >
          Preview payload
        </el-button>
        <el-button
          type="primary"
          :disabled="!canUseForm"
          :loading="createMutation.isPending.value"
          @click="createIssue"
        >
          Create Jira issue
        </el-button>
      </div>
    </div>

    <ApiResultPanel
      v-if="resultMessage"
      type="success"
      :title="resultMessage"
      :description="resultDescription"
    />

    <div class="two-column">
      <div class="surface">
        <el-form label-position="top">
          <el-row :gutter="16">
            <el-col :xs="24" :md="12">
              <el-form-item label="Product">
                <el-select
                  v-model="selectedProductCode"
                  filterable
                  placeholder="Select product"
                  class="full-width"
                >
                  <el-option
                    v-for="product in activeProducts"
                    :key="product.code"
                    :label="`${product.code} - ${product.name}`"
                    :value="product.code"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :xs="24" :md="12">
              <el-form-item label="Issue type">
                <el-select
                  v-model="selectedIssueTypeCode"
                  :disabled="!selectedProductCode"
                  filterable
                  placeholder="Select issue type"
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
          </el-row>
        </el-form>

        <el-alert
          v-if="selectionError"
          :title="selectionError"
          type="warning"
          show-icon
          :closable="false"
          class="mb-16"
        />
        <el-alert
          v-if="queryError"
          :title="queryError"
          type="error"
          show-icon
          :closable="false"
          class="mb-16"
        />

        <el-skeleton v-if="isLoadingMappings" :rows="6" animated />
        <DynamicIssueForm
          v-else
          ref="dynamicFormRef"
          :mappings="fieldMappings"
          @data-change="issueData = $event"
        />
      </div>

      <div class="surface preview-panel">
        <h3>Request data</h3>
        <JsonPreview :value="createRequest" />

        <h3>Server Jira payload preview</h3>
        <JsonPreview :value="previewMutation.data.value ?? { jiraRequest: null }" />
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useMutation, useQuery } from '@tanstack/vue-query'
import { ElMessage } from 'element-plus'
import { computed, ref, watch } from 'vue'

import ApiResultPanel from '../components/ApiResultPanel.vue'
import DynamicIssueForm from '../components/DynamicIssueForm.vue'
import JsonPreview from '../components/JsonPreview.vue'
import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import { issuesApi } from '../services/issuesApi'
import type { IssueData } from '../types/issues'

type DynamicIssueFormExpose = {
  validate: () => boolean
  buildData: () => IssueData
}

const selectedProductCode = ref('')
const selectedIssueTypeCode = ref('')
const dynamicFormRef = ref<DynamicIssueFormExpose>()
const issueData = ref<IssueData>({})
const createdIssue = ref<{ jiraIssueId?: string; jiraIssueKey?: string }>()

const productsQuery = useQuery({
  queryKey: ['admin', 'products'],
  queryFn: adminApi.getProducts,
})

const issueTypesQuery = useQuery({
  queryKey: ['admin', 'issueTypes', selectedProductCode],
  queryFn: () => adminApi.getIssueTypes(selectedProductCode.value),
  enabled: computed(() => Boolean(selectedProductCode.value)),
})

const fieldMappingsQuery = useQuery({
  queryKey: ['admin', 'fieldMappings', selectedProductCode, selectedIssueTypeCode],
  queryFn: () => adminApi.getFieldMappings(selectedProductCode.value, selectedIssueTypeCode.value),
  enabled: computed(() => Boolean(selectedProductCode.value && selectedIssueTypeCode.value)),
})

const previewMutation = useMutation({
  mutationFn: () => issuesApi.previewCreateIssue(createRequest.value),
  onSuccess: () => ElMessage.success('Payload preview refreshed.'),
})

const createMutation = useMutation({
  mutationFn: () => issuesApi.createIssue(createRequest.value),
  onSuccess: (result) => {
    createdIssue.value = result
    ElMessage.success(`Jira issue created: ${result.jiraIssueKey ?? result.jiraIssueId}`)
  },
})

const activeProducts = computed(() => (productsQuery.data.value ?? []).filter((product) => product.isActive))
const activeIssueTypes = computed(() => (issueTypesQuery.data.value ?? []).filter((issueType) => issueType.isActive))
const fieldMappings = computed(() => fieldMappingsQuery.data.value ?? [])
const canUseForm = computed(() => Boolean(selectedProductCode.value && selectedIssueTypeCode.value))
const isLoadingMappings = computed(
  () => issueTypesQuery.isLoading.value || fieldMappingsQuery.isLoading.value,
)
const createRequest = computed(() => ({
  productCode: selectedProductCode.value,
  issueTypeCode: selectedIssueTypeCode.value,
  data: issueData.value,
}))
const selectionError = computed(() => {
  if (!activeProducts.value.length && !productsQuery.isLoading.value) {
    return 'Chua co active product. Hay tao product truoc.'
  }
  if (selectedProductCode.value && !activeIssueTypes.value.length && !issueTypesQuery.isLoading.value) {
    return 'Product nay chua co active issue type.'
  }
  if (selectedIssueTypeCode.value && !fieldMappings.value.length && !fieldMappingsQuery.isLoading.value) {
    return 'Issue type nay chua co field mapping.'
  }
  return ''
})
const queryError = computed(() => {
  const error =
    productsQuery.error.value ?? issueTypesQuery.error.value ?? fieldMappingsQuery.error.value
  return error ? describeApiError(error, 'Khong tai duoc cau hinh tao issue.') : ''
})
const resultMessage = computed(() =>
  createdIssue.value
    ? `Created ${createdIssue.value.jiraIssueKey ?? createdIssue.value.jiraIssueId}`
    : '',
)
const resultDescription = computed(() =>
  createdIssue.value
    ? `Jira issue id: ${createdIssue.value.jiraIssueId ?? '-'}; key: ${
        createdIssue.value.jiraIssueKey ?? '-'
      }`
    : '',
)

watch(
  activeProducts,
  (products) => {
    if (!selectedProductCode.value && products.length > 0) {
      selectedProductCode.value = products[0].code
    }
  },
  { immediate: true },
)

watch(
  activeIssueTypes,
  (issueTypes) => {
    if (!issueTypes.some((issueType) => issueType.issueTypeCode === selectedIssueTypeCode.value)) {
      selectedIssueTypeCode.value = issueTypes[0]?.issueTypeCode ?? ''
    }
  },
  { immediate: true },
)

watch([selectedProductCode, selectedIssueTypeCode], () => {
  issueData.value = {}
  createdIssue.value = undefined
  previewMutation.reset()
})

async function previewPayload() {
  if (!validateDynamicForm()) {
    return
  }

  await previewMutation.mutateAsync()
}

async function createIssue() {
  if (!validateDynamicForm()) {
    return
  }

  await createMutation.mutateAsync()
}

function validateDynamicForm() {
  if (!dynamicFormRef.value?.validate()) {
    ElMessage.warning('Hay kiem tra lai cac field bat buoc hoac JSON object/array.')
    return false
  }

  issueData.value = dynamicFormRef.value.buildData()
  return true
}
</script>
