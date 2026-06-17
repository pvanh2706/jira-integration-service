<template>
  <el-form :model="values" label-position="top">
    <el-alert
      v-if="!hasSummaryMapping"
      title="Issue type nay chua co mapping den Jira field 'summary'. Tao issue se bi backend tu choi."
      type="warning"
      show-icon
      :closable="false"
      class="mb-16"
    />

    <el-empty v-if="!activeMappings.length" description="No active field mappings for this issue type." />

    <template v-else>
      <el-form-item
        v-for="mapping in activeMappings"
        :key="mapping.id"
        :label="fieldLabel(mapping)"
        :required="mapping.isRequired"
        :error="errors[mapping.sourcePath]"
      >
        <el-date-picker
          v-if="mapping.valueType === 'date'"
          v-model="values[mapping.sourcePath]"
          type="date"
          value-format="YYYY-MM-DD"
          placeholder="YYYY-MM-DD"
          class="full-width"
          @change="emitData"
        />
        <el-input-number
          v-else-if="mapping.valueType === 'number'"
          v-model="values[mapping.sourcePath]"
          class="full-width"
          @change="emitData"
        />
        <el-switch
          v-else-if="mapping.valueType === 'boolean'"
          v-model="values[mapping.sourcePath]"
          @change="emitData"
        />
        <el-input
          v-else-if="mapping.valueType === 'object' || mapping.valueType === 'array'"
          v-model="values[mapping.sourcePath]"
          type="textarea"
          :rows="4"
          :placeholder="jsonPlaceholder(mapping.valueType)"
          @input="emitData"
        />
        <el-input
          v-else
          v-model="values[mapping.sourcePath]"
          :placeholder="mapping.defaultValue || mapping.sourcePath"
          @input="emitData"
        />

        <p class="helper-text">
          {{ mapping.sourcePath }} -> {{ mapping.jiraField }} / {{ mapping.valueType }} /
          {{ mapping.valueShape }}
        </p>
      </el-form-item>
    </template>
  </el-form>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue'

import type { IssueFieldMappingAdminResponse } from '../types/admin'
import type { IssueData } from '../types/issues'

const props = defineProps<{
  mappings: IssueFieldMappingAdminResponse[]
}>()

const emit = defineEmits<{
  dataChange: [value: IssueData]
  validityChange: [value: boolean]
}>()

const values = reactive<Record<string, unknown>>({})
const errors = reactive<Record<string, string>>({})

const activeMappings = computed(() =>
  props.mappings
    .filter((mapping) => mapping.isActive)
    .slice()
    .sort((left, right) => left.sortOrder - right.sortOrder || left.sourcePath.localeCompare(right.sourcePath)),
)
const hasSummaryMapping = computed(() =>
  activeMappings.value.some((mapping) => mapping.jiraField.toLowerCase() === 'summary'),
)

watch(
  activeMappings,
  (mappings) => {
    const knownPaths = new Set(mappings.map((mapping) => mapping.sourcePath))
    for (const key of Object.keys(values)) {
      if (!knownPaths.has(key)) {
        delete values[key]
      }
    }

    for (const mapping of mappings) {
      if (!(mapping.sourcePath in values)) {
        values[mapping.sourcePath] = initialValue(mapping)
      }
    }

    emitData()
  },
  { immediate: true },
)

function validate() {
  clearErrors()
  let isValid = true

  for (const mapping of activeMappings.value) {
    const value = values[mapping.sourcePath]
    if (mapping.isRequired && isEmptyValue(value, mapping.valueType)) {
      errors[mapping.sourcePath] = 'Field is required.'
      isValid = false
      continue
    }

    if ((mapping.valueType === 'object' || mapping.valueType === 'array') && !isEmptyValue(value, mapping.valueType)) {
      try {
        const parsed = JSON.parse(String(value))
        if (mapping.valueType === 'array' && !Array.isArray(parsed)) {
          errors[mapping.sourcePath] = 'Value must be a JSON array.'
          isValid = false
        }
        if (mapping.valueType === 'object' && (Array.isArray(parsed) || parsed === null || typeof parsed !== 'object')) {
          errors[mapping.sourcePath] = 'Value must be a JSON object.'
          isValid = false
        }
      } catch {
        errors[mapping.sourcePath] = 'Value must be valid JSON.'
        isValid = false
      }
    }
  }

  emit('validityChange', isValid)
  return isValid
}

function buildData() {
  const data: IssueData = {}

  for (const mapping of activeMappings.value) {
    const value = normalizeValue(mapping, values[mapping.sourcePath])
    if (isEmptyValue(value, mapping.valueType)) {
      continue
    }

    setPathValue(data, normalizeSourcePath(mapping.sourcePath), value)
  }

  return data
}

function emitData() {
  clearErrors()
  const data = buildData()
  emit('dataChange', data)
  emit('validityChange', true)
}

function clearErrors() {
  for (const key of Object.keys(errors)) {
    delete errors[key]
  }
}

function initialValue(mapping: IssueFieldMappingAdminResponse) {
  if (mapping.defaultValue !== undefined && mapping.defaultValue !== null && mapping.defaultValue !== '') {
    if (mapping.valueType === 'number') {
      const numberValue = Number(mapping.defaultValue)
      return Number.isNaN(numberValue) ? undefined : numberValue
    }
    if (mapping.valueType === 'boolean') {
      return mapping.defaultValue.toLowerCase() === 'true'
    }
    return mapping.defaultValue
  }

  if (mapping.valueType === 'boolean') {
    return false
  }

  return ''
}

function normalizeValue(mapping: IssueFieldMappingAdminResponse, value: unknown) {
  if (mapping.valueType === 'object' || mapping.valueType === 'array') {
    if (isEmptyValue(value, mapping.valueType)) {
      return undefined
    }
    return JSON.parse(String(value))
  }

  if (mapping.valueType === 'number') {
    return typeof value === 'number' && !Number.isNaN(value) ? value : undefined
  }

  return value
}

function fieldLabel(mapping: IssueFieldMappingAdminResponse) {
  return `${mapping.jiraField}${mapping.isRequired ? ' *' : ''}`
}

function jsonPlaceholder(valueType: string) {
  return valueType === 'array' ? '["value"]' : '{ "key": "value" }'
}

function normalizeSourcePath(sourcePath: string) {
  const parts = sourcePath
    .split('.')
    .map((part) => part.trim())
    .filter(Boolean)

  return parts[0]?.toLowerCase() === 'data' ? parts.slice(1) : parts
}

function setPathValue(target: IssueData, path: string[], value: unknown) {
  if (path.length === 0) {
    return
  }

  let current: Record<string, unknown> = target
  for (let index = 0; index < path.length; index += 1) {
    const key = path[index]
    if (index === path.length - 1) {
      current[key] = value
      return
    }

    if (!current[key] || typeof current[key] !== 'object' || Array.isArray(current[key])) {
      current[key] = {}
    }
    current = current[key] as Record<string, unknown>
  }
}

function isEmptyValue(value: unknown, valueType: string) {
  if (value === null || value === undefined) {
    return true
  }

  if (valueType === 'boolean') {
    return false
  }

  return typeof value === 'string' && value.trim() === ''
}

defineExpose({
  validate,
  buildData,
})
</script>
