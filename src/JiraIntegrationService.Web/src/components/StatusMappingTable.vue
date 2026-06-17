<template>
  <div class="tab-stack">
    <div class="table-toolbar">
      <div class="toolbar-title">
        <h3>Status mappings</h3>
        <p>Map standard status sang Jira status/transition theo issue type.</p>
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
        New status
      </el-button>
    </div>

    <el-alert
      v-if="!activeIssueTypes.length && !issueTypesQuery.isLoading.value"
      title="Hay tao issue type truoc khi cau hinh status mapping."
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
      v-loading="statusMappingsQuery.isLoading.value || issueTypesQuery.isLoading.value"
      :data="statusMappings"
      empty-text="No status mappings"
    >
      <el-table-column prop="standardStatus" label="Standard status" min-width="160" />
      <el-table-column prop="jiraStatusName" label="Jira status" min-width="180" />
      <el-table-column label="Transition" min-width="220">
        <template #default="{ row }">
          <div class="stack-compact">
            <span>{{ row.jiraTransitionId || '-' }}</span>
            <small>{{ row.jiraTransitionName || '-' }}</small>
          </div>
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
      :title="dialogMode === 'create' ? 'Create status mapping' : `Edit ${form.standardStatus}`"
      width="620px"
      destroy-on-close
      @closed="formRef?.clearValidate()"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <el-row :gutter="16">
          <el-col :xs="24" :md="12">
            <el-form-item label="Standard status" prop="standardStatus">
              <el-select
                v-model="form.standardStatus"
                :disabled="isSaving"
                filterable
                allow-create
                default-first-option
              >
                <el-option v-for="item in standardStatuses" :key="item" :label="item" :value="item" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira status name" prop="jiraStatusName">
              <el-input v-model="form.jiraStatusName" :disabled="isSaving" placeholder="In Progress" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira transition id">
              <el-input v-model="form.jiraTransitionId" :disabled="isSaving" placeholder="31" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :md="12">
            <el-form-item label="Jira transition name">
              <el-input v-model="form.jiraTransitionName" :disabled="isSaving" placeholder="Start Progress" />
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { computed, reactive, ref, watch } from 'vue'

import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import type {
  StatusMappingAdminResponse,
  UpsertStatusMappingAdminRequest,
} from '../types/admin'

const standardStatuses = ['OPEN', 'IN_PROGRESS', 'WAITING', 'DONE', 'CANCELLED']

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
  standardStatus: 'OPEN',
  jiraStatusName: '',
  jiraTransitionId: '',
  jiraTransitionName: '',
  isActive: true,
})

const issueTypesQuery = useQuery({
  queryKey: ['admin', 'issueTypes', computed(() => props.productCode)],
  queryFn: () => adminApi.getIssueTypes(props.productCode),
  enabled: computed(() => Boolean(props.productCode)),
})

const statusMappingsQuery = useQuery({
  queryKey: ['admin', 'statusMappings', computed(() => props.productCode), selectedIssueTypeCode],
  queryFn: () => adminApi.getStatusMappings(props.productCode, selectedIssueTypeCode.value),
  enabled: computed(() => Boolean(props.productCode && selectedIssueTypeCode.value)),
})

const createMutation = useMutation({
  mutationFn: (payload: UpsertStatusMappingAdminRequest) =>
    adminApi.createStatusMapping(props.productCode, selectedIssueTypeCode.value, payload),
  onSuccess: async () => {
    ElMessage.success('Status mapping created.')
    dialogVisible.value = false
    await invalidateStatusMappings()
  },
})

const updateMutation = useMutation({
  mutationFn: ({ id, payload }: { id: number; payload: UpsertStatusMappingAdminRequest }) =>
    adminApi.updateStatusMapping(id, payload),
  onSuccess: async () => {
    ElMessage.success('Status mapping updated.')
    dialogVisible.value = false
    await invalidateStatusMappings()
  },
})

const deleteMutation = useMutation({
  mutationFn: (id: number) => adminApi.deleteStatusMapping(id),
  onSuccess: async () => {
    ElMessage.success('Status mapping deleted.')
    await invalidateStatusMappings()
  },
})

const activeIssueTypes = computed(() => (issueTypesQuery.data.value ?? []).filter((item) => item.isActive))
const statusMappings = computed(() => statusMappingsQuery.data.value ?? [])
const errorMessage = computed(() => {
  const error = issueTypesQuery.error.value ?? statusMappingsQuery.error.value
  return error ? describeApiError(error, 'Khong tai duoc status mappings.') : ''
})
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const rules: FormRules = {
  standardStatus: [{ required: true, message: 'Standard status is required.', trigger: 'change' }],
  jiraStatusName: [{ required: true, message: 'Jira status name is required.', trigger: 'blur' }],
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
    standardStatus: 'OPEN',
    jiraStatusName: '',
    jiraTransitionId: '',
    jiraTransitionName: '',
    isActive: true,
  })
  dialogVisible.value = true
}

function openEditDialog(mapping: StatusMappingAdminResponse) {
  dialogMode.value = 'edit'
  editingId.value = mapping.id
  Object.assign(form, {
    standardStatus: mapping.standardStatus,
    jiraStatusName: mapping.jiraStatusName,
    jiraTransitionId: mapping.jiraTransitionId ?? '',
    jiraTransitionName: mapping.jiraTransitionName ?? '',
    isActive: mapping.isActive,
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

async function confirmDelete(mapping: StatusMappingAdminResponse) {
  try {
    await ElMessageBox.confirm(
      `Delete status mapping ${mapping.standardStatus} -> ${mapping.jiraStatusName}?`,
      'Delete status mapping',
      {
        type: 'warning',
        confirmButtonText: 'Delete',
        cancelButtonText: 'Cancel',
        confirmButtonClass: 'el-button--danger',
      },
    )
  } catch {
    return
  }

  await deleteMutation.mutateAsync(mapping.id)
}

function toPayload(): UpsertStatusMappingAdminRequest {
  return {
    standardStatus: form.standardStatus.trim(),
    jiraStatusName: form.jiraStatusName.trim(),
    jiraTransitionId: optional(form.jiraTransitionId),
    jiraTransitionName: optional(form.jiraTransitionName),
    isActive: form.isActive,
  }
}

async function invalidateStatusMappings() {
  await queryClient.invalidateQueries({
    queryKey: ['admin', 'statusMappings', props.productCode, selectedIssueTypeCode.value],
  })
}

function optional(value: string) {
  const trimmed = value.trim()
  return trimmed ? trimmed : undefined
}
</script>
