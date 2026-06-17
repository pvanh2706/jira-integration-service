<template>
  <el-form ref="formRef" :model="modelValue" :rules="rules" label-position="top">
    <el-row :gutter="16">
      <el-col :xs="24" :md="12">
        <el-form-item label="Auth type" prop="authType">
          <el-select
            :model-value="modelValue.authType"
            :disabled="loading"
            filterable
            allow-create
            default-first-option
            @update:model-value="updateField('authType', String($event))"
          >
            <el-option label="Basic" value="Basic" />
          </el-select>
        </el-form-item>
      </el-col>
      <el-col :xs="24" :md="12">
        <el-form-item label="Active">
          <el-switch
            :model-value="modelValue.isActive"
            :disabled="loading"
            @update:model-value="updateField('isActive', Boolean($event))"
          />
        </el-form-item>
      </el-col>
    </el-row>

    <el-form-item label="Username" prop="username">
      <el-input
        :model-value="modelValue.username"
        :disabled="loading"
        placeholder="jira-user"
        @update:model-value="updateField('username', $event)"
      />
    </el-form-item>

    <el-form-item label="Password or token" prop="passwordOrToken">
      <el-input
        :model-value="modelValue.passwordOrToken"
        :disabled="loading"
        show-password
        placeholder="Nhap token moi khi luu"
        @update:model-value="updateField('passwordOrToken', $event)"
      />
      <p class="helper-text">
        Backend chi tra ve trang thai co token, khong tra secret. Khi save credential, hay nhap
        password/token moi.
      </p>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import type { FormInstance, FormRules } from 'element-plus'
import { ref } from 'vue'

import type { CredentialFormModel } from '../types/forms'

const props = withDefaults(
  defineProps<{
    modelValue: CredentialFormModel
    loading?: boolean
  }>(),
  {
    loading: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: CredentialFormModel]
}>()

const formRef = ref<FormInstance>()

const rules: FormRules = {
  authType: [{ required: true, message: 'Auth type is required.', trigger: 'blur' }],
  username: [{ required: true, message: 'Username is required.', trigger: 'blur' }],
  passwordOrToken: [
    { required: true, message: 'Password or token is required when saving.', trigger: 'blur' },
  ],
}

function updateField<K extends keyof CredentialFormModel>(field: K, value: CredentialFormModel[K]) {
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
