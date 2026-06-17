<template>
  <div class="tab-stack">
    <div class="table-toolbar">
      <div class="toolbar-title">
        <h3>Validate create issue config</h3>
        <p>Kiem tra product, credential, issue type va mapping bat buoc.</p>
      </div>
      <el-select v-model="issueTypeCode" class="table-toolbar__filter" placeholder="All issue types">
        <el-option label="All issue types" value="" />
        <el-option
          v-for="issueType in activeIssueTypes"
          :key="issueType.issueTypeCode"
          :label="issueType.issueTypeCode"
          :value="issueType.issueTypeCode"
        />
      </el-select>
      <el-button type="primary" :loading="validateMutation.isPending.value" @click="validate">
        Validate
      </el-button>
    </div>

    <el-alert
      v-if="issueTypesError"
      :title="issueTypesError"
      type="error"
      show-icon
      :closable="false"
    />

    <el-result
      v-if="result"
      :icon="result.isValid ? 'success' : 'warning'"
      :title="result.isValid ? 'Configuration is valid' : 'Configuration needs attention'"
      :sub-title="`Product ${result.productCode}${result.issueTypeCode ? ` / ${result.issueTypeCode}` : ''}`"
    >
      <template #extra>
        <el-alert
          v-if="!result.isValid"
          title="Validation errors"
          type="error"
          :closable="false"
          show-icon
        >
          <ul class="error-list">
            <li v-for="error in result.errors" :key="error">{{ error }}</li>
          </ul>
        </el-alert>
      </template>
    </el-result>

    <el-empty v-else description="Bam Validate de kiem tra cau hinh hien tai." />
  </div>
</template>

<script setup lang="ts">
import { useMutation, useQuery } from '@tanstack/vue-query'
import { ElMessage } from 'element-plus'
import { computed, ref } from 'vue'

import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import type { ValidateCreateIssueConfigAdminResponse } from '../types/admin'

const props = defineProps<{
  productCode: string
}>()

const issueTypeCode = ref('')
const result = ref<ValidateCreateIssueConfigAdminResponse>()

const issueTypesQuery = useQuery({
  queryKey: ['admin', 'issueTypes', computed(() => props.productCode)],
  queryFn: () => adminApi.getIssueTypes(props.productCode),
  enabled: computed(() => Boolean(props.productCode)),
})

const validateMutation = useMutation({
  mutationFn: () =>
    adminApi.validateCreateIssueConfig(props.productCode, {
      issueTypeCode: issueTypeCode.value || undefined,
    }),
  onSuccess: (response) => {
    result.value = response
    ElMessage[response.isValid ? 'success' : 'warning'](
      response.isValid ? 'Configuration is valid.' : 'Configuration has validation errors.',
    )
  },
})

const activeIssueTypes = computed(() => (issueTypesQuery.data.value ?? []).filter((item) => item.isActive))
const issueTypesError = computed(() =>
  issueTypesQuery.error.value
    ? describeApiError(issueTypesQuery.error.value, 'Khong tai duoc issue types.')
    : '',
)

async function validate() {
  await validateMutation.mutateAsync()
}
</script>
