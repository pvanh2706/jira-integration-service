<template>
  <div class="tab-stack">
    <div class="table-toolbar">
      <div class="toolbar-title">
        <h3>Field mappings</h3>
        <p>Map du lieu request sang Jira fields cho tung issue type.</p>
      </div>
      <el-select v-model="selectedIssueTypeCode" class="table-toolbar__filter" placeholder="Issue type">
        <el-option
          v-for="issueType in activeIssueTypes"
          :key="issueType.issueTypeCode"
          :label="issueType.issueTypeCode"
          :value="issueType.issueTypeCode"
        />
      </el-select>
      <el-button
        type="primary"
        :icon="Plus"
        :disabled="!selectedIssueTypeCode"
        @click="openCreateDialog"
      >
        New mapping
      </el-button>
    </div>

    <el-alert
      v-if="!activeIssueTypes.length && !issueTypesQuery.isLoading.value"
      title="Hay tao issue type truoc khi cau hinh field mapping."
      type="warning"
      show-icon
      :closable="false"
    />
    <el-alert
      v-if="errorMessage"
      :title="errorMessage"
      type="error"
      show-icon
      :closable="false"
    />

    <el-table
      v-loading="fieldMappingsQuery.isLoading.value || issueTypesQuery.isLoading.value"
      :data="fieldMappings"
      empty-text="No field mappings"
    >
      <el-table-column prop="sortOrder" label="#" width="70" />
      <el-table-column prop="sourcePath" label="Source path" min-width="180" />
      <el-table-column prop="jiraField" label="Jira field" min-width="160" />
      <el-table-column label="Type/shape" width="170">
        <template #default="{ row }">
          <div class="stack-compact">
            <span>{{ row.valueType }}</span>
            <small>{{ row.valueShape }}</small>
          </div>
        </template>
      </el-table-column>
      <el-table-column label="Required" width="110">
        <template #default="{ row }">
          <el-tag :type="row.isRequired ? 'warning' : 'info'" effect="plain">
            {{ row.isRequired ? 'Required' : 'Optional' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="Status" width="110">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'info'" effect="plain">
            {{ row.isActive ? 'Active' : 'Inactive' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="Action" width="160" align="right" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEditDialog(row)">Edit</el-button>
          <el-button link type="danger" @click="confirmDelete(row)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'create' ? 'Create field mapping' : `Edit ${form.sourcePath}`"
      width="760px"
      destroy-on-close
      @closed="formRef?.clearValidate()"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <el-row :gutter="16">
          <el-col :xs="24" :md="12">
            <el-form-item label="Source path" prop="sourcePath">
              <el-input v-model="form.sourcePath" :disabled="isSaving" placeholder="data.summary" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira field" prop="jiraField">
              <el-input v-model="form.jiraField" :disabled="isSaving" placeholder="summary" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :xs="24" :md="8">
            <el-form-item label="Value type" prop="valueType">
              <el-select v-model="form.valueType" :disabled="isSaving">
                <el-option v-for="item in valueTypes" :key="item" :label="item" :value="item" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="8">
            <el-form-item label="Value shape" prop="valueShape">
              <el-select v-model="form.valueShape" :disabled="isSaving">
                <el-option v-for="item in valueShapes" :key="item" :label="item" :value="item" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="8">
            <el-form-item label="Sort order" prop="sortOrder">
              <el-input-number v-model="form.sortOrder" :disabled="isSaving" :min="0" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :xs="24" :md="8">
            <el-form-item label="Required">
              <el-switch v-model="form.isRequired" :disabled="isSaving" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="8">
            <el-form-item label="Active">
              <el-switch v-model="form.isActive" :disabled="isSaving" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="Default value">
          <el-input v-model="form.defaultValue" :disabled="isSaving" clearable />
        </el-form-item>

        <el-form-item label="Transform config JSON" prop="transformConfigJson">
          <el-input
            v-model="form.transformConfigJson"
            :disabled="isSaving"
            type="textarea"
            :rows="4"
            placeholder='{"trim": true}'
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="isSaving" @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="isSaving" @click="submit">
          {{ dialogMode === 'create' ? 'Create' : 'Save changes' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { Plus } from '@element-plus/icons-vue'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { computed, reactive, ref, watch } from 'vue'

import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import type {
  IssueFieldMappingAdminResponse,
  UpsertIssueFieldMappingAdminRequest,
} from '../types/admin'

const valueTypes = ['string', 'number', 'boolean', 'date', 'object', 'array']
const valueShapes = ['raw', 'name', 'id', 'value', 'arrayOfName', 'arrayOfId']

const props = defineProps<{
  productCode: string
}>()

const queryClient = useQueryClient()
const formRef = ref<FormInstance>()
const selectedIssueTypeCode = ref('')
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const editingId = ref<number>()
const form = reactive({
  sourcePath: '',
  jiraField: '',
  valueType: 'string',
  valueShape: 'raw',
  isRequired: false,
  defaultValue: '',
  sortOrder: 10,
  isActive: true,
  transformConfigJson: '',
})

const issueTypesQuery = useQuery({
  queryKey: ['admin', 'issueTypes', computed(() => props.productCode)],
  queryFn: () => adminApi.getIssueTypes(props.productCode),
  enabled: computed(() => Boolean(props.productCode)),
})

const fieldMappingsQuery = useQuery({
  queryKey: ['admin', 'fieldMappings', computed(() => props.productCode), selectedIssueTypeCode],
  queryFn: () => adminApi.getFieldMappings(props.productCode, selectedIssueTypeCode.value),
  enabled: computed(() => Boolean(props.productCode && selectedIssueTypeCode.value)),
})

const createMutation = useMutation({
  mutationFn: (payload: UpsertIssueFieldMappingAdminRequest) =>
    adminApi.createFieldMapping(props.productCode, selectedIssueTypeCode.value, payload),
  onSuccess: async () => {
    ElMessage.success('Field mapping created.')
    dialogVisible.value = false
    await invalidateFieldMappings()
  },
})

const updateMutation = useMutation({
  mutationFn: ({ id, payload }: { id: number; payload: UpsertIssueFieldMappingAdminRequest }) =>
    adminApi.updateFieldMapping(id, payload),
  onSuccess: async () => {
    ElMessage.success('Field mapping updated.')
    dialogVisible.value = false
    await invalidateFieldMappings()
  },
})

const deleteMutation = useMutation({
  mutationFn: (id: number) => adminApi.deleteFieldMapping(id),
  onSuccess: async () => {
    ElMessage.success('Field mapping deleted.')
    await invalidateFieldMappings()
  },
})

const activeIssueTypes = computed(() => (issueTypesQuery.data.value ?? []).filter((item) => item.isActive))
const fieldMappings = computed(() => fieldMappingsQuery.data.value ?? [])
const errorMessage = computed(() => {
  const error = issueTypesQuery.error.value ?? fieldMappingsQuery.error.value
  return error ? describeApiError(error, 'Khong tai duoc field mappings.') : ''
})
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const rules: FormRules = {
  sourcePath: [{ required: true, message: 'Source path is required.', trigger: 'blur' }],
  jiraField: [{ required: true, message: 'Jira field is required.', trigger: 'blur' }],
  valueType: [{ required: true, message: 'Value type is required.', trigger: 'change' }],
  valueShape: [{ required: true, message: 'Value shape is required.', trigger: 'change' }],
  transformConfigJson: [
    {
      validator: (_rule, value, callback) => {
        const raw = String(value ?? '').trim()
        if (!raw) {
          callback()
          return
        }

        try {
          JSON.parse(raw)
          callback()
        } catch {
          callback(new Error('Transform config must be valid JSON.'))
        }
      },
      trigger: 'blur',
    },
  ],
}

watch(
  activeIssueTypes,
  (items) => {
    if (!selectedIssueTypeCode.value && items.length > 0) {
      selectedIssueTypeCode.value = items[0].issueTypeCode
    }
  },
  { immediate: true },
)

function openCreateDialog() {
  dialogMode.value = 'create'
  editingId.value = undefined
  Object.assign(form, {
    sourcePath: '',
    jiraField: '',
    valueType: 'string',
    valueShape: 'raw',
    isRequired: false,
    defaultValue: '',
    sortOrder: nextSortOrder(),
    isActive: true,
    transformConfigJson: '',
  })
  dialogVisible.value = true
}

function openEditDialog(mapping: IssueFieldMappingAdminResponse) {
  dialogMode.value = 'edit'
  editingId.value = mapping.id
  Object.assign(form, {
    sourcePath: mapping.sourcePath,
    jiraField: mapping.jiraField,
    valueType: mapping.valueType,
    valueShape: mapping.valueShape,
    isRequired: mapping.isRequired,
    defaultValue: mapping.defaultValue ?? '',
    sortOrder: mapping.sortOrder,
    isActive: mapping.isActive,
    transformConfigJson: mapping.transformConfigJson ?? '',
  })
  dialogVisible.value = true
}

async function submit() {
  await formRef.value?.validate()
  const payload = toPayload()

  if (dialogMode.value === 'create') {
    await createMutation.mutateAsync(payload)
    return
  }

  await updateMutation.mutateAsync({
    id: editingId.value!,
    payload,
  })
}

async function confirmDelete(mapping: IssueFieldMappingAdminResponse) {
  try {
    await ElMessageBox.confirm(`Delete mapping ${mapping.sourcePath}?`, 'Delete field mapping', {
      type: 'warning',
      confirmButtonText: 'Delete',
      cancelButtonText: 'Cancel',
      confirmButtonClass: 'el-button--danger',
    })
  } catch {
    return
  }

  await deleteMutation.mutateAsync(mapping.id)
}

function toPayload(): UpsertIssueFieldMappingAdminRequest {
  return {
    sourcePath: form.sourcePath.trim(),
    jiraField: form.jiraField.trim(),
    valueType: form.valueType,
    valueShape: form.valueShape,
    isRequired: form.isRequired,
    defaultValue: optional(form.defaultValue),
    sortOrder: form.sortOrder,
    isActive: form.isActive,
    transformConfigJson: optional(form.transformConfigJson),
  }
}

function nextSortOrder() {
  const currentMax = fieldMappings.value.reduce((max, item) => Math.max(max, item.sortOrder), 0)
  return currentMax + 10
}

async function invalidateFieldMappings() {
  await queryClient.invalidateQueries({
    queryKey: ['admin', 'fieldMappings', props.productCode, selectedIssueTypeCode.value],
  })
}

function optional(value: string) {
  const trimmed = value.trim()
  return trimmed ? trimmed : undefined
}
</script>
