<template>
  <div class="tab-stack">
    <div class="table-toolbar field-mapping-toolbar">
      <div class="toolbar-title">
        <h3>Field mappings</h3>
        <p>Map du lieu request sang Jira fields cho tung issue type.</p>
      </div>

      <div class="field-mapping-toolbar__controls">
        <div class="field-mapping-toolbar__selectors">
          <label class="field-mapping-control">
            <span>Issue type</span>
            <el-select
              v-model="selectedIssueTypeCode"
              class="field-mapping-control__input"
              placeholder="Issue type"
            >
              <el-option
                v-for="issueType in activeIssueTypes"
                :key="issueType.issueTypeCode"
                :label="issueType.issueTypeCode"
                :value="issueType.issueTypeCode"
              />
            </el-select>
          </label>

          <label class="field-mapping-control">
            <span>Template</span>
            <el-select
              v-model="selectedTemplateCode"
              class="field-mapping-control__input"
              :disabled="!selectedIssueTypeCode"
              placeholder="Template"
            >
              <el-option
                v-for="template in activeTemplates"
                :key="template.templateCode"
                :label="`${template.name} (${template.mappingCount})`"
                :value="template.templateCode"
              />
            </el-select>
          </label>

          <el-tag
            v-if="selectedIssueTypeCode"
            class="field-mapping-toolbar__updated"
            :type="jiraFieldsUpdatedAt ? 'success' : 'info'"
            effect="plain"
          >
            {{ jiraFieldsUpdatedLabel }}
          </el-tag>
        </div>

        <div class="field-mapping-toolbar__actions">
          <el-button
            :icon="Plus"
            :disabled="!selectedIssueTypeCode"
            @click="openTemplateDialog"
          >
            New template
          </el-button>
          <el-button
            :icon="CopyDocument"
            :disabled="!selectedIssueTypeCode || !selectedTemplateCode"
            @click="openCopyTemplateDialog"
          >
            Copy template
          </el-button>
          <el-button
            :icon="Refresh"
            :loading="syncJiraFieldsMutation.isPending.value"
            :disabled="!canSyncJiraFields"
            @click="reloadJiraFields"
          >
            Reload Jira fields
          </el-button>
          <el-button
            v-if="canSetEasSubTaskDefaults"
            :icon="Refresh"
            :loading="setDefaultsMutation.isPending.value"
            @click="setEasSubTaskDefaults"
          >
            Set EAS defaults
          </el-button>
          <el-button
            type="primary"
            :icon="Plus"
            :disabled="!selectedIssueTypeCode || !selectedTemplateCode || setDefaultsMutation.isPending.value"
            @click="openCreateDialog"
          >
            New mapping
          </el-button>
        </div>
      </div>
    </div>

    <el-alert
      v-if="!activeIssueTypes.length && !issueTypesQuery.isLoading.value"
      title="Hay tao issue type truoc khi cau hinh field mapping."
      type="warning"
      show-icon
      :closable="false"
    />
    <el-alert
      v-if="selectedIssueTypeCode && !canSyncJiraFields"
      title="Issue type nay chua co Jira issue type id nen khong reload duoc Jira fields tu Jira."
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
      v-loading="
        fieldMappingsQuery.isLoading.value ||
        issueTypesQuery.isLoading.value ||
        templatesQuery.isLoading.value ||
        setDefaultsMutation.isPending.value ||
        syncJiraFieldsMutation.isPending.value
      "
      :data="fieldMappings"
      empty-text="No field mappings"
    >
      <el-table-column prop="sortOrder" label="#" width="70" />
      <el-table-column prop="sourcePath" label="Source path" min-width="180" />
      <el-table-column label="Jira field" min-width="220">
        <template #default="{ row }">
          <div class="stack-compact">
            <span>{{ row.jiraFieldName || row.jiraField }}</span>
            <small>{{ row.jiraField }}</small>
          </div>
        </template>
      </el-table-column>
      <el-table-column label="Description" min-width="220">
        <template #default="{ row }">{{ row.jiraFieldDescription || '-' }}</template>
      </el-table-column>
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
        <el-form-item v-if="jiraFields.length" label="Jira metadata">
          <el-select
            v-model="selectedJiraFieldId"
            :disabled="isSaving"
            filterable
            clearable
            placeholder="Select Jira field"
            class="full-width"
            @change="applySelectedJiraField"
          >
            <el-option
              v-for="field in jiraFields"
              :key="field.fieldId"
              :label="jiraFieldOptionLabel(field)"
              :value="field.fieldId"
            />
          </el-select>
        </el-form-item>

        <el-alert
          v-if="selectedJiraField"
          :title="selectedJiraFieldSummary"
          type="info"
          show-icon
          :closable="false"
          class="mb-16"
        />

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

        <el-form-item label="Jira field name">
          <el-input
            v-model="form.jiraFieldName"
            :disabled="isSaving"
            placeholder="Technical Issue Type"
          />
        </el-form-item>

        <el-form-item label="Description">
          <el-input
            v-model="form.jiraFieldDescription"
            :disabled="isSaving"
            type="textarea"
            :rows="3"
            placeholder="Mo ta y nghia field de nguoi tao issue nhap dung."
          />
        </el-form-item>

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
          <el-select
            v-if="defaultValueOptions.length && isDefaultMultiSelect"
            v-model="defaultArrayValue"
            :disabled="isSaving"
            multiple
            filterable
            clearable
            class="full-width"
          >
            <el-option
              v-for="option in defaultValueOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
              :disabled="option.disabled"
            />
          </el-select>
          <el-select
            v-else-if="defaultValueOptions.length"
            v-model="defaultScalarValue"
            :disabled="isSaving"
            filterable
            clearable
            class="full-width"
          >
            <el-option
              v-for="option in defaultValueOptions"
              :key="option.value"
              :label="option.label"
              :value="option.value"
              :disabled="option.disabled"
            />
          </el-select>
          <el-date-picker
            v-else-if="form.valueType === 'date'"
            v-model="form.defaultValue"
            :disabled="isSaving"
            type="date"
            value-format="YYYY-MM-DD"
            placeholder="YYYY-MM-DD"
            class="full-width"
          />
          <el-input-number
            v-else-if="form.valueType === 'number'"
            v-model="defaultNumberValue"
            :disabled="isSaving"
            class="full-width"
          />
          <el-switch
            v-else-if="form.valueType === 'boolean'"
            v-model="defaultBooleanValue"
            :disabled="isSaving"
          />
          <el-input v-else v-model="form.defaultValue" :disabled="isSaving" clearable />
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

    <el-dialog
      v-model="templateDialogVisible"
      :title="templateDialogTitle"
      width="520px"
      destroy-on-close
    >
      <el-form :model="templateForm" label-position="top">
        <el-form-item label="Template code">
          <el-input v-model="templateForm.templateCode" placeholder="SUPPORT_FAST" />
        </el-form-item>
        <el-form-item label="Name">
          <el-input v-model="templateForm.name" placeholder="Support fast create" />
        </el-form-item>
        <el-form-item label="Copy mappings">
          <el-switch v-model="templateForm.copyMappings" />
        </el-form-item>
        <el-form-item label="Source template">
          <el-select
            v-model="templateForm.sourceTemplateCode"
            :disabled="!templateForm.copyMappings"
            class="full-width"
          >
            <el-option
              v-for="template in activeTemplates"
              :key="template.templateCode"
              :label="`${template.name} (${template.templateCode})`"
              :value="template.templateCode"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="Description">
          <el-input
            v-model="templateForm.description"
            type="textarea"
            :rows="3"
            placeholder="Ghi chu ngan ve muc dich template."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :disabled="createTemplateMutation.isPending.value" @click="templateDialogVisible = false">
          Cancel
        </el-button>
        <el-button
          type="primary"
          :loading="createTemplateMutation.isPending.value"
          @click="createTemplate"
        >
          Create
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { CopyDocument, Plus, Refresh } from '@element-plus/icons-vue'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { computed, reactive, ref, watch } from 'vue'

import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import type {
  CreateFieldMappingTemplateAdminRequest,
  IssueFieldMappingAdminResponse,
  JiraAllowedValueAdminResponse,
  JiraFieldMetadataAdminResponse,
  UpsertIssueFieldMappingAdminRequest,
} from '../types/admin'

const valueTypes = ['string', 'number', 'boolean', 'date', 'object', 'array']
const valueShapes = ['raw', 'name', 'id', 'value', 'arrayOfName', 'arrayOfId']

type DefaultValueOption = {
  label: string
  value: string
  disabled: boolean
}

const props = defineProps<{
  productCode: string
}>()

const queryClient = useQueryClient()
const formRef = ref<FormInstance>()
const selectedIssueTypeCode = ref('')
const selectedTemplateCode = ref('')
const selectedJiraFieldId = ref('')
const dialogVisible = ref(false)
const templateDialogVisible = ref(false)
const templateDialogMode = ref<'create' | 'copy'>('create')
const dialogMode = ref<'create' | 'edit'>('create')
const editingId = ref<number>()
const templateForm = reactive({
  templateCode: '',
  name: '',
  description: '',
  sourceTemplateCode: '',
  copyMappings: true,
})
const form = reactive({
  sourcePath: '',
  jiraField: '',
  jiraFieldName: '',
  jiraFieldDescription: '',
  jiraSchemaType: '',
  jiraSchemaItems: '',
  jiraSchemaSystem: '',
  jiraSchemaCustom: '',
  jiraAllowedValuesJson: '',
  jiraDefaultValueJson: '',
  jiraAutoCompleteUrl: '',
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
  queryKey: ['admin', 'fieldMappings', computed(() => props.productCode), selectedIssueTypeCode, selectedTemplateCode],
  queryFn: () => adminApi.getFieldMappings(
    props.productCode,
    selectedIssueTypeCode.value,
    selectedTemplateCode.value,
  ),
  enabled: computed(() => Boolean(props.productCode && selectedIssueTypeCode.value && selectedTemplateCode.value)),
})

const templatesQuery = useQuery({
  queryKey: ['admin', 'fieldMappingTemplates', computed(() => props.productCode), selectedIssueTypeCode],
  queryFn: () => adminApi.getFieldMappingTemplates(props.productCode, selectedIssueTypeCode.value),
  enabled: computed(() => Boolean(props.productCode && selectedIssueTypeCode.value)),
})

const createMutation = useMutation({
  mutationFn: (payload: UpsertIssueFieldMappingAdminRequest) =>
    adminApi.createFieldMapping(
      props.productCode,
      selectedIssueTypeCode.value,
      payload,
      selectedTemplateCode.value,
    ),
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

const setDefaultsMutation = useMutation({
  mutationFn: () =>
    adminApi.setEasSubTaskDefaultFieldMappings(
      props.productCode,
      selectedIssueTypeCode.value,
      selectedTemplateCode.value,
    ),
  onSuccess: async (result) => {
    ElMessage.success(`Set ${result.total} default field mappings.`)
    await invalidateFieldMappings()
    await queryClient.invalidateQueries({ queryKey: ['admin', 'issueTypes', props.productCode] })
  },
})

const createTemplateMutation = useMutation({
  mutationFn: (payload: CreateFieldMappingTemplateAdminRequest) =>
    adminApi.createFieldMappingTemplate(props.productCode, selectedIssueTypeCode.value, payload),
  onSuccess: async (template) => {
    ElMessage.success(`Template ${template.templateCode} created.`)
    templateDialogVisible.value = false
    selectedTemplateCode.value = template.templateCode
    await invalidateTemplates()
    await invalidateFieldMappings()
  },
})

const activeIssueTypes = computed(() => (issueTypesQuery.data.value ?? []).filter((item) => item.isActive))
const activeTemplates = computed(() => (templatesQuery.data.value ?? []).filter((item) => item.isActive))
const selectedTemplate = computed(() =>
  activeTemplates.value.find((item) => item.templateCode === selectedTemplateCode.value),
)
const selectedIssueType = computed(() =>
  activeIssueTypes.value.find((item) => item.issueTypeCode === selectedIssueTypeCode.value),
)
const canReadJiraFields = computed(() => Boolean(props.productCode && selectedIssueTypeCode.value))
const canSyncJiraFields = computed(() =>
  Boolean(canReadJiraFields.value && selectedIssueType.value?.jiraIssueTypeId),
)
const jiraFieldsQuery = useQuery({
  queryKey: computed(() => ['admin', 'jiraFields', props.productCode, selectedIssueTypeCode.value]),
  queryFn: () => adminApi.getJiraFields(props.productCode, selectedIssueTypeCode.value),
  enabled: computed(() => canReadJiraFields.value),
  staleTime: 5 * 60 * 1000,
})
const syncJiraFieldsMutation = useMutation({
  mutationFn: () => adminApi.syncJiraFieldsFromJira(props.productCode, selectedIssueTypeCode.value),
  onSuccess: async (result) => {
    ElMessage.success(`Reloaded ${result.total} Jira fields.`)
    queryClient.setQueryData(['admin', 'jiraFields', props.productCode, selectedIssueTypeCode.value], result)
  },
})
const fieldMappings = computed(() => fieldMappingsQuery.data.value ?? [])
const jiraFields = computed(() => jiraFieldsQuery.data.value?.fields ?? [])
const jiraFieldsUpdatedAt = computed(() => jiraFieldsQuery.data.value?.updatedAt ?? '')
const jiraFieldsUpdatedLabel = computed(() =>
  jiraFieldsUpdatedAt.value ? `Updated ${formatDateTime(jiraFieldsUpdatedAt.value)}` : 'Not synced',
)
const selectedJiraField = computed(() => {
  const selectedById = jiraFields.value.find((field) => field.fieldId === selectedJiraFieldId.value)
  if (selectedById) {
    return selectedById
  }

  return jiraFields.value.find(
    (field) => field.fieldId === form.jiraField || recommendedJiraField(field) === form.jiraField,
  )
})
const selectedJiraFieldSummary = computed(() => {
  const field = selectedJiraField.value
  if (!field) {
    return ''
  }

  const schema = [field.schemaType, field.schemaItems].filter(Boolean).join(':') || 'unknown'
  const allowedCount = field.allowedValues.length
  return `${field.name} (${field.fieldId}) / ${schema} / ${allowedCount} options`
})
const formAllowedValues = computed(() => selectedJiraField.value?.allowedValues ?? parseAllowedValuesJson(form.jiraAllowedValuesJson))
const defaultValueOptions = computed<DefaultValueOption[]>(() =>
  formAllowedValues.value
    .map((value) => ({
      label: allowedValueLabel(value),
      value: allowedValueOutput(value),
      disabled: value.disabled,
    }))
    .filter((option) => option.value),
)
const isDefaultMultiSelect = computed(
  () =>
    form.valueType === 'array' ||
    form.valueShape === 'arrayOfId' ||
    form.valueShape === 'arrayOfName' ||
    form.jiraField === 'componentIds',
)
const defaultScalarValue = computed({
  get: () => form.defaultValue,
  set: (value: string) => {
    form.defaultValue = value ?? ''
  },
})
const defaultArrayValue = computed<string[]>({
  get: () => parseDefaultArray(form.defaultValue),
  set: (value) => {
    form.defaultValue = value.length ? JSON.stringify(value) : ''
  },
})
const defaultNumberValue = computed<number | undefined>({
  get: () => {
    const value = Number(form.defaultValue)
    return Number.isFinite(value) ? value : undefined
  },
  set: (value) => {
    form.defaultValue = value === undefined || value === null ? '' : String(value)
  },
})
const defaultBooleanValue = computed({
  get: () => form.defaultValue.toLowerCase() === 'true',
  set: (value: boolean) => {
    form.defaultValue = String(value)
  },
})
const errorMessage = computed(() => {
  const error =
    issueTypesQuery.error.value ??
    templatesQuery.error.value ??
    fieldMappingsQuery.error.value ??
    jiraFieldsQuery.error.value ??
    syncJiraFieldsMutation.error.value ??
    createTemplateMutation.error.value ??
    setDefaultsMutation.error.value
  return error ? describeApiError(error, 'Khong tai duoc field mappings.') : ''
})
const isSaving = computed(
  () =>
    createMutation.isPending.value ||
    updateMutation.isPending.value ||
    setDefaultsMutation.isPending.value ||
    createTemplateMutation.isPending.value,
)
const canSetEasSubTaskDefaults = computed(
  () =>
    props.productCode.trim().toUpperCase() === 'EAS' &&
    normalizeIdentifier(selectedIssueTypeCode.value) === 'SUBTASK',
)
const templateDialogTitle = computed(() =>
  templateDialogMode.value === 'copy'
    ? `Copy ${selectedTemplate.value?.name ?? 'template'}`
    : 'Create field mapping template',
)

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

watch(
  activeTemplates,
  (templates) => {
    if (!templates.some((template) => template.templateCode === selectedTemplateCode.value)) {
      selectedTemplateCode.value =
        templates.find((template) => template.isDefault)?.templateCode ?? templates[0]?.templateCode ?? ''
    }
  },
  { immediate: true },
)

watch(selectedIssueTypeCode, () => {
  selectedJiraFieldId.value = ''
  selectedTemplateCode.value = ''
})

function openCreateDialog() {
  dialogMode.value = 'create'
  editingId.value = undefined
  selectedJiraFieldId.value = ''
  Object.assign(form, {
    sourcePath: '',
    jiraField: '',
    jiraFieldName: '',
    jiraFieldDescription: '',
    jiraSchemaType: '',
    jiraSchemaItems: '',
    jiraSchemaSystem: '',
    jiraSchemaCustom: '',
    jiraAllowedValuesJson: '',
    jiraDefaultValueJson: '',
    jiraAutoCompleteUrl: '',
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

function openTemplateDialog() {
  templateDialogMode.value = 'create'
  templateForm.templateCode = ''
  templateForm.name = ''
  templateForm.description = ''
  templateForm.sourceTemplateCode = ''
  templateForm.copyMappings = false
  templateDialogVisible.value = true
}

function openCopyTemplateDialog() {
  const sourceTemplate = selectedTemplate.value
  if (!sourceTemplate) {
    ElMessage.warning('Hay chon template can copy.')
    return
  }

  templateDialogMode.value = 'copy'
  templateForm.templateCode = nextCopyTemplateCode(sourceTemplate.templateCode)
  templateForm.name = `Copy of ${sourceTemplate.name}`
  templateForm.description = sourceTemplate.description
    ? `Copied from ${sourceTemplate.templateCode}. ${sourceTemplate.description}`
    : `Copied from ${sourceTemplate.templateCode}.`
  templateForm.sourceTemplateCode = sourceTemplate.templateCode
  templateForm.copyMappings = true
  templateDialogVisible.value = true
}

async function createTemplate() {
  const templateCode = templateForm.templateCode.trim()
  const name = templateForm.name.trim()
  if (!templateCode || !name) {
    ElMessage.warning('Template code va name la bat buoc.')
    return
  }

  await createTemplateMutation.mutateAsync({
    templateCode,
    name,
    description: optional(templateForm.description),
    sourceTemplateCode: optional(templateForm.sourceTemplateCode),
    copyMappings: templateForm.copyMappings,
    isActive: true,
  })
}

function openEditDialog(mapping: IssueFieldMappingAdminResponse) {
  dialogMode.value = 'edit'
  editingId.value = mapping.id
  selectedJiraFieldId.value = findMetadataFieldId(mapping)
  Object.assign(form, {
    sourcePath: mapping.sourcePath,
    jiraField: mapping.jiraField,
    jiraFieldName: mapping.jiraFieldName ?? '',
    jiraFieldDescription: mapping.jiraFieldDescription ?? '',
    jiraSchemaType: mapping.jiraSchemaType ?? '',
    jiraSchemaItems: mapping.jiraSchemaItems ?? '',
    jiraSchemaSystem: mapping.jiraSchemaSystem ?? '',
    jiraSchemaCustom: mapping.jiraSchemaCustom ?? '',
    jiraAllowedValuesJson: mapping.jiraAllowedValuesJson ?? '',
    jiraDefaultValueJson: mapping.jiraDefaultValueJson ?? '',
    jiraAutoCompleteUrl: mapping.jiraAutoCompleteUrl ?? '',
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

async function setEasSubTaskDefaults() {
  try {
    await ElMessageBox.confirm(
      'Tat ca field mapping hien tai cua EAS / SUB TASK se bi ghi de bang bo gia tri mac dinh.',
      'Set default field mappings',
      {
        type: 'warning',
        confirmButtonText: 'Set defaults',
        cancelButtonText: 'Cancel',
      },
    )
  } catch {
    return
  }

  await setDefaultsMutation.mutateAsync()
}

async function reloadJiraFields() {
  if (!canSyncJiraFields.value) {
    return
  }

  await syncJiraFieldsMutation.mutateAsync()
}

function applySelectedJiraField(value: string | number | boolean | undefined) {
  const fieldId = String(value ?? '')
  if (!fieldId) {
    return
  }

  const field = jiraFields.value.find((item) => item.fieldId === fieldId)
  if (!field) {
    return
  }

  form.jiraField = recommendedJiraField(field)
  form.jiraFieldName = field.name
  if (dialogMode.value === 'create' || !form.jiraFieldDescription.trim()) {
    form.jiraFieldDescription = field.name
  }
  form.jiraSchemaType = field.schemaType ?? ''
  form.jiraSchemaItems = field.schemaItems ?? ''
  form.jiraSchemaSystem = field.schemaSystem ?? ''
  form.jiraSchemaCustom = field.schemaCustom ?? ''
  form.jiraAllowedValuesJson = serializeAllowedValues(field.allowedValues)
  form.jiraDefaultValueJson = field.defaultValueJson ?? ''
  form.jiraAutoCompleteUrl = field.autoCompleteUrl ?? ''
  form.valueType = recommendedValueType(field)
  form.valueShape = recommendedValueShape(field)
  form.isRequired = field.required

  if (dialogMode.value === 'create' || !form.sourcePath.trim()) {
    form.sourcePath = recommendedSourcePath(field)
  }

  form.defaultValue = defaultValueFromJira(field)
}

function jiraFieldOptionLabel(field: JiraFieldMetadataAdminResponse) {
  return `${field.name} (${field.fieldId})`
}

function findMetadataFieldId(mapping: IssueFieldMappingAdminResponse) {
  return (
    jiraFields.value.find(
      (field) =>
        field.fieldId === mapping.jiraField ||
        recommendedJiraField(field) === mapping.jiraField ||
        field.name === mapping.jiraFieldName,
    )?.fieldId ?? ''
  )
}

function recommendedJiraField(field: JiraFieldMetadataAdminResponse) {
  if (field.fieldId === 'parent') {
    return 'parentKey'
  }
  if (field.fieldId === 'components') {
    return 'componentIds'
  }
  if (field.fieldId === 'worklog') {
    return 'worklogs'
  }
  return field.fieldId
}

function recommendedValueType(field: JiraFieldMetadataAdminResponse) {
  if (field.fieldId === 'components' || field.fieldId === 'worklog') {
    return 'array'
  }
  if (field.fieldId === 'parent') {
    return 'string'
  }
  return field.recommendedValueType
}

function recommendedValueShape(field: JiraFieldMetadataAdminResponse) {
  if (field.fieldId === 'components' || field.fieldId === 'parent' || field.fieldId === 'worklog') {
    return 'raw'
  }
  return field.recommendedValueShape
}

function recommendedSourcePath(field: JiraFieldMetadataAdminResponse) {
  const specialPaths: Record<string, string> = {
    components: 'data.componentIds',
    fixVersions: 'data.fixVersionIds',
    parent: 'data.parentKey',
    worklog: 'data.worklogs',
  }

  if (specialPaths[field.fieldId]) {
    return specialPaths[field.fieldId]
  }

  const namePath = toCamelCase(field.name)
  return namePath ? `data.${namePath}` : `data.customFields.${field.fieldId}`
}

function serializeAllowedValues(values: JiraAllowedValueAdminResponse[]) {
  const rawValues = values.map((value) => parseJson(value.rawJson) ?? toRawAllowedValue(value))
  return rawValues.length ? JSON.stringify(rawValues) : ''
}

function defaultValueFromJira(field: JiraFieldMetadataAdminResponse) {
  if (!field.defaultValueJson) {
    return ''
  }

  const parsed = parseJson(field.defaultValueJson)
  if (parsed === undefined || parsed === null) {
    return ''
  }

  if (Array.isArray(parsed)) {
    return JSON.stringify(parsed)
  }

  if (isRecord(parsed)) {
    return valueFromRecord(parsed, recommendedValueShape(field))
  }

  return String(parsed)
}

function parseAllowedValuesJson(raw: string): JiraAllowedValueAdminResponse[] {
  const parsed = parseJson(raw)
  if (!Array.isArray(parsed)) {
    return []
  }

  return parsed
    .filter(isRecord)
    .map((item) => ({
      id: optionalUnknown(item.id),
      key: optionalUnknown(item.key),
      name: optionalUnknown(item.name),
      value: optionalUnknown(item.value),
      description: optionalUnknown(item.description),
      disabled: item.disabled === true,
      rawJson: JSON.stringify(item),
    }))
}

function allowedValueLabel(value: JiraAllowedValueAdminResponse) {
  const primary = value.value ?? value.name ?? value.key ?? value.id ?? '-'
  const details = [
    value.name && value.name !== primary ? value.name : '',
    value.id && value.id !== primary ? `id:${value.id}` : '',
  ].filter(Boolean)

  return details.length ? `${primary} (${details.join(', ')})` : primary
}

function allowedValueOutput(value: JiraAllowedValueAdminResponse) {
  if (form.jiraField === 'componentIds' || form.valueShape === 'id' || form.valueShape === 'arrayOfId') {
    return value.id ?? value.value ?? value.name ?? value.key ?? ''
  }
  if (form.valueShape === 'name' || form.valueShape === 'arrayOfName') {
    return value.name ?? value.value ?? value.id ?? value.key ?? ''
  }
  if (form.valueShape === 'value') {
    return value.value ?? value.name ?? value.id ?? value.key ?? ''
  }
  return value.value ?? value.name ?? value.key ?? value.id ?? ''
}

function parseDefaultArray(raw: string) {
  const parsed = parseJson(raw)
  if (Array.isArray(parsed)) {
    return parsed.map((item) => String(item)).filter(Boolean)
  }
  return raw.trim() ? [raw.trim()] : []
}

function valueFromRecord(record: Record<string, unknown>, valueShape: string) {
  if (valueShape === 'id' || valueShape === 'arrayOfId') {
    return optionalUnknown(record.id) ?? ''
  }
  if (valueShape === 'name' || valueShape === 'arrayOfName') {
    return optionalUnknown(record.name) ?? ''
  }
  if (valueShape === 'value') {
    return optionalUnknown(record.value) ?? ''
  }
  return optionalUnknown(record.value) ?? optionalUnknown(record.name) ?? optionalUnknown(record.id) ?? ''
}

function toRawAllowedValue(value: JiraAllowedValueAdminResponse) {
  return {
    id: value.id,
    key: value.key,
    name: value.name,
    value: value.value,
    description: value.description,
    disabled: value.disabled,
  }
}

function parseJson(raw: string) {
  if (!raw.trim()) {
    return undefined
  }

  try {
    return JSON.parse(raw) as unknown
  } catch {
    return undefined
  }
}

function optionalUnknown(value: unknown) {
  if (value === null || value === undefined) {
    return undefined
  }
  const text = String(value).trim()
  return text ? text : undefined
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function toCamelCase(value: string) {
  const words = value
    .normalize('NFD')
    .replace(/[đĐ]/g, (character) => (character === 'Đ' ? 'D' : 'd'))
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9]+/g, ' ')
    .trim()
    .split(/\s+/)
    .filter(Boolean)

  if (!words.length) {
    return ''
  }

  return words
    .map((word, index) => {
      const lower = word.toLowerCase()
      return index === 0 ? lower : lower.charAt(0).toUpperCase() + lower.slice(1)
    })
    .join('')
}

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

function toPayload(): UpsertIssueFieldMappingAdminRequest {
  return {
    sourcePath: form.sourcePath.trim(),
    jiraField: form.jiraField.trim(),
    jiraFieldName: optional(form.jiraFieldName),
    jiraFieldDescription: optional(form.jiraFieldDescription),
    jiraSchemaType: optional(form.jiraSchemaType),
    jiraSchemaItems: optional(form.jiraSchemaItems),
    jiraSchemaSystem: optional(form.jiraSchemaSystem),
    jiraSchemaCustom: optional(form.jiraSchemaCustom),
    jiraAllowedValuesJson: optional(form.jiraAllowedValuesJson),
    jiraDefaultValueJson: optional(form.jiraDefaultValueJson),
    jiraAutoCompleteUrl: optional(form.jiraAutoCompleteUrl),
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

function nextCopyTemplateCode(sourceTemplateCode: string) {
  const existingCodes = new Set(activeTemplates.value.map((template) => template.templateCode))
  const baseCode = `${sourceTemplateCode}_COPY`
  if (!existingCodes.has(baseCode)) {
    return baseCode
  }

  for (let index = 2; index < 100; index += 1) {
    const candidate = `${baseCode}_${index}`
    if (!existingCodes.has(candidate)) {
      return candidate
    }
  }

  return `${baseCode}_${Date.now()}`
}

async function invalidateFieldMappings() {
  await queryClient.invalidateQueries({
    queryKey: ['admin', 'fieldMappings', props.productCode, selectedIssueTypeCode.value, selectedTemplateCode.value],
  })
  await invalidateTemplates()
}

async function invalidateTemplates() {
  await queryClient.invalidateQueries({
    queryKey: ['admin', 'fieldMappingTemplates', props.productCode, selectedIssueTypeCode.value],
  })
}

function optional(value: string) {
  const trimmed = value.trim()
  return trimmed ? trimmed : undefined
}

function normalizeIdentifier(value: string) {
  return value.replace(/[^a-z0-9]/gi, '').toUpperCase()
}
</script>

<style scoped>
.field-mapping-toolbar {
  align-items: flex-start;
  flex-wrap: wrap;
}

.field-mapping-toolbar .toolbar-title {
  flex: 1 1 220px;
}

.field-mapping-toolbar__controls {
  display: flex;
  min-width: min(100%, 720px);
  flex: 2 1 720px;
  flex-direction: column;
  gap: 10px;
}

.field-mapping-toolbar__selectors,
.field-mapping-toolbar__actions {
  display: flex;
  min-width: 0;
  flex-wrap: wrap;
  gap: 10px;
  align-items: flex-end;
  justify-content: flex-end;
}

.field-mapping-control {
  display: flex;
  min-width: 180px;
  flex: 1 1 220px;
  flex-direction: column;
  gap: 4px;
}

.field-mapping-control span {
  color: #6b7280;
  font-size: 12px;
  line-height: 1.4;
}

.field-mapping-control__input {
  width: 100%;
}

.field-mapping-toolbar__updated {
  max-width: 100%;
  min-height: 32px;
  line-height: 30px;
}

.field-mapping-toolbar__actions .el-button {
  flex: 0 1 auto;
  min-width: 132px;
}

.field-mapping-toolbar__actions .el-button + .el-button {
  margin-left: 0;
}

@media (max-width: 860px) {
  .field-mapping-toolbar__controls,
  .field-mapping-toolbar__selectors,
  .field-mapping-toolbar__actions {
    width: 100%;
  }

  .field-mapping-toolbar__selectors,
  .field-mapping-toolbar__actions {
    justify-content: stretch;
  }

  .field-mapping-control,
  .field-mapping-toolbar__actions .el-button {
    flex: 1 1 100%;
    min-width: 0;
  }
}
</style>
