<template>
  <el-form ref="formRef" :model="modelValue" :rules="rules" label-position="top">
    <el-row :gutter="16">
      <el-col :xs="24" :md="12">
        <el-form-item label="Product code" prop="code">
          <el-input
            :model-value="modelValue.code"
            :disabled="mode === 'edit' || loading"
            placeholder="CRM"
            @update:model-value="updateField('code', $event)"
          />
        </el-form-item>
      </el-col>
      <el-col :xs="24" :md="12">
        <el-form-item label="Name" prop="name">
          <el-input
            :model-value="modelValue.name"
            :disabled="loading"
            placeholder="CRM"
            @update:model-value="updateField('name', $event)"
          />
        </el-form-item>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <el-col :xs="24" :md="12">
        <el-form-item label="Jira project key" prop="jiraProjectKey">
          <el-input
            :model-value="modelValue.jiraProjectKey"
            :disabled="loading"
            placeholder="CRM"
            @update:model-value="updateField('jiraProjectKey', $event)"
          />
        </el-form-item>
      </el-col>
      <el-col :xs="24" :md="12">
        <el-form-item label="Jira version" prop="jiraVersion">
          <el-select
            :model-value="modelValue.jiraVersion"
            :disabled="loading"
            filterable
            allow-create
            default-first-option
            @update:model-value="updateField('jiraVersion', String($event))"
          >
            <el-option label="Jira Server v2" value="ServerV2" />
          </el-select>
        </el-form-item>
      </el-col>
    </el-row>

    <el-form-item label="Jira base URL" prop="jiraBaseUrl">
      <el-input
        :model-value="modelValue.jiraBaseUrl"
        :disabled="loading"
        placeholder="https://jira.example.com"
        @update:model-value="updateField('jiraBaseUrl', $event)"
      />
    </el-form-item>

    <el-form-item label="Jira API base path" prop="jiraApiBasePath">
      <el-input
        :model-value="modelValue.jiraApiBasePath"
        :disabled="loading"
        placeholder="/rest/api/2"
        @update:model-value="updateField('jiraApiBasePath', $event)"
      />
    </el-form-item>

    <el-form-item label="Active">
      <el-switch
        :model-value="modelValue.isActive"
        :disabled="loading"
        @update:model-value="updateField('isActive', Boolean($event))"
      />
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import type { FormInstance, FormRules } from 'element-plus'
import { ref } from 'vue'

import type { ProductFormModel } from '../types/forms'

const props = withDefaults(
  defineProps<{
    modelValue: ProductFormModel
    mode?: 'create' | 'edit'
    loading?: boolean
  }>(),
  {
    mode: 'create',
    loading: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: ProductFormModel]
}>()

const formRef = ref<FormInstance>()

const rules: FormRules = {
  code: [{ required: true, message: 'Product code is required.', trigger: 'blur' }],
  name: [{ required: true, message: 'Name is required.', trigger: 'blur' }],
  jiraProjectKey: [{ required: true, message: 'Jira project key is required.', trigger: 'blur' }],
  jiraBaseUrl: [
    { required: true, message: 'Jira base URL is required.', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (!String(value ?? '').startsWith('http')) {
          callback(new Error('Jira base URL should start with http or https.'))
          return
        }

        callback()
      },
      trigger: 'blur',
    },
  ],
  jiraApiBasePath: [{ required: true, message: 'Jira API base path is required.', trigger: 'blur' }],
  jiraVersion: [{ required: true, message: 'Jira version is required.', trigger: 'blur' }],
}

function updateField<K extends keyof ProductFormModel>(field: K, value: ProductFormModel[K]) {
  emit('update:modelValue', {
    ...props.modelValue,
    [field]: value,
  })
}

async function validate() {
  await formRef.value?.validate()
}

function clearValidate() {
  formRef.value?.clearValidate()
}

defineExpose({
  validate,
  clearValidate,
})
</script>
