<template>
  <div class="tab-stack">
    <div class="table-toolbar">
      <div class="toolbar-title">
        <h3>Issue type mappings</h3>
        <p>Map issue type noi bo sang Jira issue type id/name.</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="openCreateDialog">New issue type</el-button>
    </div>

    <el-alert
      v-if="errorMessage"
      :title="errorMessage"
      type="error"
      show-icon
      :closable="false"
    />

    <el-table
      v-loading="issueTypesQuery.isLoading.value"
      :data="issueTypes"
      empty-text="No issue type mappings"
    >
      <el-table-column prop="issueTypeCode" label="Code" width="140" />
      <el-table-column prop="jiraIssueTypeId" label="Jira type id" width="160">
        <template #default="{ row }">{{ row.jiraIssueTypeId || '-' }}</template>
      </el-table-column>
      <el-table-column prop="jiraIssueTypeName" label="Jira type name" min-width="180">
        <template #default="{ row }">{{ row.jiraIssueTypeName || '-' }}</template>
      </el-table-column>
      <el-table-column label="Status" width="120">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'info'" effect="plain">
            {{ row.isActive ? 'Active' : 'Inactive' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="Updated" width="160">
        <template #default="{ row }">{{ formatDate(row.updatedAt) }}</template>
      </el-table-column>
      <el-table-column label="Action" width="100" align="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEditDialog(row)">Edit</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'create' ? 'Create issue type' : `Edit ${form.issueTypeCode}`"
      width="560px"
      destroy-on-close
      @closed="formRef?.clearValidate()"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <el-form-item label="Issue type code" prop="issueTypeCode">
          <el-input
            v-model="form.issueTypeCode"
            :disabled="dialogMode === 'edit' || isSaving"
            placeholder="BUG"
          />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira issue type id" prop="jiraIssueTypeId">
              <el-input v-model="form.jiraIssueTypeId" :disabled="isSaving" placeholder="10001" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira issue type name" prop="jiraIssueTypeName">
              <el-input v-model="form.jiraIssueTypeName" :disabled="isSaving" placeholder="Bug" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="Active">
          <el-switch v-model="form.isActive" :disabled="isSaving" />
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
import dayjs from 'dayjs'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { computed, reactive, ref } from 'vue'

import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import type {
  CreateIssueTypeMappingAdminRequest,
  IssueTypeMappingAdminResponse,
  UpdateIssueTypeMappingAdminRequest,
} from '../types/admin'

const props = defineProps<{
  productCode: string
}>()

const queryClient = useQueryClient()
const formRef = ref<FormInstance>()
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const form = reactive({
  issueTypeCode: '',
  jiraIssueTypeId: '',
  jiraIssueTypeName: '',
  isActive: true,
})

const issueTypesQuery = useQuery({
  queryKey: ['admin', 'issueTypes', computed(() => props.productCode)],
  queryFn: () => adminApi.getIssueTypes(props.productCode),
  enabled: computed(() => Boolean(props.productCode)),
})

const createMutation = useMutation({
  mutationFn: (payload: CreateIssueTypeMappingAdminRequest) =>
    adminApi.createIssueType(props.productCode, payload),
  onSuccess: async (issueType) => {
    ElMessage.success(`Issue type ${issueType.issueTypeCode} created.`)
    dialogVisible.value = false
    await invalidateIssueTypes()
  },
})

const updateMutation = useMutation({
  mutationFn: ({
    issueTypeCode,
    payload,
  }: {
    issueTypeCode: string
    payload: UpdateIssueTypeMappingAdminRequest
  }) => adminApi.updateIssueType(props.productCode, issueTypeCode, payload),
  onSuccess: async (issueType) => {
    ElMessage.success(`Issue type ${issueType.issueTypeCode} updated.`)
    dialogVisible.value = false
    await invalidateIssueTypes()
  },
})

const issueTypes = computed(() => issueTypesQuery.data.value ?? [])
const errorMessage = computed(() =>
  issueTypesQuery.error.value
    ? describeApiError(issueTypesQuery.error.value, 'Khong tai duoc issue types.')
    : '',
)
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const requireTarget = (_rule: unknown, _value: unknown, callback: (error?: Error) => void) => {
  if (!form.jiraIssueTypeId.trim() && !form.jiraIssueTypeName.trim()) {
    callback(new Error('Jira issue type id or name is required.'))
    return
  }

  callback()
}

const rules: FormRules = {
  issueTypeCode: [{ required: true, message: 'Issue type code is required.', trigger: 'blur' }],
  jiraIssueTypeId: [{ validator: requireTarget, trigger: 'blur' }],
  jiraIssueTypeName: [{ validator: requireTarget, trigger: 'blur' }],
}

function openCreateDialog() {
  dialogMode.value = 'create'
  Object.assign(form, {
    issueTypeCode: '',
    jiraIssueTypeId: '',
    jiraIssueTypeName: '',
    isActive: true,
  })
  dialogVisible.value = true
}

function openEditDialog(issueType: IssueTypeMappingAdminResponse) {
  dialogMode.value = 'edit'
  Object.assign(form, {
    issueTypeCode: issueType.issueTypeCode,
    jiraIssueTypeId: issueType.jiraIssueTypeId ?? '',
    jiraIssueTypeName: issueType.jiraIssueTypeName ?? '',
    isActive: issueType.isActive,
  })
  dialogVisible.value = true
}

async function submit() {
  await formRef.value?.validate()

  if (dialogMode.value === 'create') {
    await createMutation.mutateAsync({
      issueTypeCode: form.issueTypeCode.trim(),
      jiraIssueTypeId: optional(form.jiraIssueTypeId),
      jiraIssueTypeName: optional(form.jiraIssueTypeName),
      isActive: form.isActive,
    })
    return
  }

  await updateMutation.mutateAsync({
    issueTypeCode: form.issueTypeCode,
    payload: {
      jiraIssueTypeId: optional(form.jiraIssueTypeId),
      jiraIssueTypeName: optional(form.jiraIssueTypeName),
      isActive: form.isActive,
    },
  })
}

async function invalidateIssueTypes() {
  await queryClient.invalidateQueries({ queryKey: ['admin', 'issueTypes', props.productCode] })
}

function optional(value: string) {
  const trimmed = value.trim()
  return trimmed ? trimmed : undefined
}

function formatDate(value: string) {
  return dayjs(value).format('YYYY-MM-DD HH:mm')
}
</script>
