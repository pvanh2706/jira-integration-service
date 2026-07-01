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
        <el-select
          v-if="selectOptions(mapping).length && isMultiSelect(mapping)"
          v-model="values[mapping.sourcePath]"
          multiple
          filterable
          clearable
          class="full-width"
          @change="emitData"
        >
          <el-option
            v-for="option in selectOptions(mapping)"
            :key="option.value"
            :label="option.label"
            :value="option.value"
            :disabled="option.disabled"
          />
        </el-select>
        <el-select
          v-else-if="selectOptions(mapping).length"
          v-model="values[mapping.sourcePath]"
          filterable
          clearable
          class="full-width"
          @change="emitData"
        >
          <el-option
            v-for="option in selectOptions(mapping)"
            :key="option.value"
            :label="option.label"
            :value="option.value"
            :disabled="option.disabled"
          />
        </el-select>
        <div v-else-if="isWorklogMapping(mapping)" class="worklog-editor">
          <div
            v-for="(entry, index) in worklogEntries(mapping)"
            :key="index"
            class="worklog-entry"
          >
            <div class="worklog-entry__grid">
              <label class="worklog-field">
                <span>Started</span>
                <el-date-picker
                  v-model="entry.started"
                  type="datetime"
                  format="YYYY-MM-DD HH:mm"
                  value-format="YYYY-MM-DDTHH:mm:ss.SSSZZ"
                  placeholder="Started"
                  class="full-width"
                  @change="emitData"
                />
              </label>
              <label class="worklog-field">
                <span>Time spent</span>
                <el-input
                  v-model="entry.timeSpent"
                  placeholder="4h"
                  clearable
                  @input="emitData"
                />
              </label>
              <el-button
                :icon="Delete"
                :disabled="worklogEntries(mapping).length === 1"
                @click="removeWorklog(mapping, index)"
              >
                Remove
              </el-button>
            </div>
            <label class="worklog-field worklog-entry__comment">
              <span>Comment</span>
              <el-input
                v-model="entry.comment"
                type="textarea"
                :rows="2"
                placeholder="Noi dung log work"
                @input="emitData"
              />
            </label>
          </div>
          <el-button :icon="Plus" @click="addWorklog(mapping)">Add log work</el-button>
        </div>
        <el-date-picker
          v-else-if="isDateTimeMapping(mapping)"
          v-model="values[mapping.sourcePath]"
          type="datetime"
          format="YYYY-MM-DD HH:mm"
          value-format="YYYY-MM-DDTHH:mm:ss.SSSZZ"
          placeholder="YYYY-MM-DD HH:mm"
          class="full-width"
          @change="emitData"
        />
        <el-date-picker
          v-else-if="mapping.valueType === 'date'"
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

        <p v-if="mapping.jiraFieldDescription" class="helper-text">
          {{ mapping.jiraFieldDescription }}
        </p>
        <p class="helper-text">
          {{ mapping.sourcePath }} -> {{ mapping.jiraField }} / {{ mapping.valueType }} /
          {{ mapping.valueShape }}
        </p>
      </el-form-item>
    </template>
  </el-form>
</template>

<script setup lang="ts">
import { Delete, Plus } from '@element-plus/icons-vue'
import { computed, reactive, watch } from 'vue'

import type { IssueFieldMappingAdminResponse } from '../types/admin'
import type { IssueData } from '../types/issues'

type SelectOption = {
  label: string
  value: string
  disabled: boolean
}

type AllowedValue = {
  id?: string
  key?: string
  name?: string
  value?: string
  description?: string
  disabled: boolean
}

type WorklogFormEntry = {
  started: string
  timeSpent: string
  comment: string
}

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
    if (isWorklogMapping(mapping)) {
      const validationError = validateWorklogs(mapping, value)
      if (validationError) {
        errors[mapping.sourcePath] = validationError
        isValid = false
      }
      continue
    }

    if (mapping.isRequired && isEmptyValue(value, mapping.valueType)) {
      errors[mapping.sourcePath] = 'Field is required.'
      isValid = false
      continue
    }

    if ((mapping.valueType === 'object' || mapping.valueType === 'array') && !isEmptyValue(value, mapping.valueType)) {
      const validationError = validateStructuredValue(mapping, value)
      if (validationError) {
        errors[mapping.sourcePath] = validationError
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
  if (isWorklogMapping(mapping)) {
    return parseDefaultWorklogs(mapping.defaultValue)
  }

  if (mapping.defaultValue !== undefined && mapping.defaultValue !== null && mapping.defaultValue !== '') {
    if (mapping.valueType === 'date') {
      return isDateTimeMapping(mapping)
        ? normalizeDateTimeDefault(mapping.defaultValue)
        : normalizeDateDefault(mapping.defaultValue)
    }
    if (isMultiSelect(mapping)) {
      return parseDefaultArray(mapping.defaultValue)
    }
    if (mapping.valueType === 'number') {
      const numberValue = Number(mapping.defaultValue)
      return Number.isNaN(numberValue) ? undefined : numberValue
    }
    if (mapping.valueType === 'boolean') {
      return mapping.defaultValue.toLowerCase() === 'true'
    }
    return mapping.defaultValue
  }

  if (isMultiSelect(mapping)) {
    return []
  }

  if (mapping.valueType === 'boolean') {
    return false
  }

  if (mapping.valueType === 'date') {
    return isDateTimeMapping(mapping) ? formatDateTimeForJira(new Date()) : formatDateOnly(new Date())
  }

  return ''
}

function normalizeValue(mapping: IssueFieldMappingAdminResponse, value: unknown) {
  if (isWorklogMapping(mapping)) {
    return normalizeWorklogValue(value)
  }

  if (mapping.valueType === 'array') {
    if (isEmptyValue(value, mapping.valueType)) {
      return undefined
    }
    return Array.isArray(value) ? value : JSON.parse(String(value))
  }

  if (mapping.valueType === 'object') {
    if (isEmptyValue(value, mapping.valueType)) {
      return undefined
    }
    if (isRecord(value)) {
      return value
    }
    return JSON.parse(String(value))
  }

  if (mapping.valueType === 'number') {
    return typeof value === 'number' && !Number.isNaN(value) ? value : undefined
  }

  return value
}

function worklogEntries(mapping: IssueFieldMappingAdminResponse) {
  const current = values[mapping.sourcePath]
  if (Array.isArray(current)) {
    return current as WorklogFormEntry[]
  }

  const entries = parseDefaultWorklogs(typeof current === 'string' ? current : undefined)
  values[mapping.sourcePath] = entries
  return entries
}

function addWorklog(mapping: IssueFieldMappingAdminResponse) {
  worklogEntries(mapping).push(createWorklogEntry())
  emitData()
}

function removeWorklog(mapping: IssueFieldMappingAdminResponse, index: number) {
  const entries = worklogEntries(mapping)
  if (entries.length <= 1) {
    return
  }

  entries.splice(index, 1)
  emitData()
}

function validateWorklogs(mapping: IssueFieldMappingAdminResponse, value: unknown) {
  const entries = normalizeWorklogEntries(value)
  const touchedEntries = entries.filter(isTouchedWorklogEntry)

  if (mapping.isRequired && touchedEntries.length === 0) {
    return 'At least one worklog entry is required.'
  }

  for (const [index, entry] of touchedEntries.entries()) {
    if (!entry.started.trim()) {
      return `Worklog #${index + 1}: started is required.`
    }
    if (!entry.timeSpent.trim()) {
      return `Worklog #${index + 1}: time spent is required.`
    }
  }

  return ''
}

function normalizeWorklogValue(value: unknown) {
  const entries = normalizeWorklogEntries(value)
    .filter((entry) => entry.timeSpent.trim())
    .map((entry) => {
      const result: { started: string; timeSpent: string; comment?: string } = {
        started: entry.started.trim() || formatDateTimeForJira(new Date()),
        timeSpent: entry.timeSpent.trim(),
      }
      const comment = entry.comment.trim()
      if (comment) {
        result.comment = comment
      }
      return result
    })

  return entries.length ? entries : undefined
}

function normalizeWorklogEntries(value: unknown) {
  const source = Array.isArray(value)
    ? value
    : typeof value === 'string'
      ? parseJson(value)
      : undefined
  if (!Array.isArray(source)) {
    return []
  }

  return source
    .map(toWorklogEntry)
    .filter((entry): entry is WorklogFormEntry => Boolean(entry))
}

function parseDefaultWorklogs(raw?: string) {
  const entries = normalizeWorklogEntries(raw ?? '')
  return entries.length ? entries : [createWorklogEntry()]
}

function toWorklogEntry(value: unknown): WorklogFormEntry | undefined {
  if (!isRecord(value)) {
    return undefined
  }

  const source = isRecord(value.add) ? value.add : value
  return {
    started: optionalUnknown(source.started) ?? formatDateTimeForJira(new Date()),
    timeSpent: optionalUnknown(source.timeSpent) ?? '',
    comment: optionalUnknown(source.comment) ?? '',
  }
}

function createWorklogEntry(): WorklogFormEntry {
  return {
    started: formatDateTimeForJira(new Date()),
    timeSpent: '',
    comment: '',
  }
}

function isTouchedWorklogEntry(entry: WorklogFormEntry) {
  return Boolean(entry.timeSpent.trim() || entry.comment.trim())
}

function fieldLabel(mapping: IssueFieldMappingAdminResponse) {
  return `${mapping.jiraFieldName || mapping.jiraField}${mapping.isRequired ? ' *' : ''}`
}

function jsonPlaceholder(valueType: string) {
  return valueType === 'array' ? '["value"]' : '{ "key": "value" }'
}

function isWorklogMapping(mapping: IssueFieldMappingAdminResponse) {
  const jiraField = mapping.jiraField.trim().toLowerCase()
  const sourcePath = mapping.sourcePath.trim().toLowerCase()
  return (
    jiraField === 'worklogs' ||
    jiraField === 'worklog' ||
    sourcePath.endsWith('.worklogs') ||
    sourcePath.endsWith('.worklog')
  )
}

function isDateTimeMapping(mapping: IssueFieldMappingAdminResponse) {
  if (mapping.valueType !== 'date') {
    return false
  }

  const metadataText = [
    mapping.jiraSchemaType,
    mapping.jiraSchemaCustom,
    mapping.jiraFieldName,
    mapping.jiraField,
    mapping.sourcePath,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return metadataText.includes('datetime') || metadataText.includes('date time') || metadataText.includes('time')
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

  if (Array.isArray(value)) {
    return value.length === 0
  }

  if (valueType === 'boolean') {
    return false
  }

  return typeof value === 'string' && value.trim() === ''
}

function validateStructuredValue(mapping: IssueFieldMappingAdminResponse, value: unknown) {
  if (mapping.valueType === 'array' && Array.isArray(value)) {
    return ''
  }
  if (mapping.valueType === 'object' && isRecord(value)) {
    return ''
  }

  try {
    const parsed = JSON.parse(String(value)) as unknown
    if (mapping.valueType === 'array' && !Array.isArray(parsed)) {
      return 'Value must be a JSON array.'
    }
    if (mapping.valueType === 'object' && !isRecord(parsed)) {
      return 'Value must be a JSON object.'
    }
    return ''
  } catch {
    return 'Value must be valid JSON.'
  }
}

function selectOptions(mapping: IssueFieldMappingAdminResponse): SelectOption[] {
  return parseAllowedValuesJson(mapping.jiraAllowedValuesJson ?? '')
    .map((value) => ({
      label: allowedValueLabel(value),
      value: allowedValueOutput(mapping, value),
      disabled: value.disabled,
    }))
    .filter((option) => option.value)
}

function isMultiSelect(mapping: IssueFieldMappingAdminResponse) {
  return (
    mapping.valueType === 'array' ||
    mapping.valueShape === 'arrayOfId' ||
    mapping.valueShape === 'arrayOfName' ||
    mapping.jiraField === 'componentIds'
  )
}

function normalizeDateDefault(value: string) {
  const trimmed = value.trim()
  if (!trimmed) {
    return formatDateOnly(new Date())
  }

  const date = new Date(trimmed)
  return Number.isNaN(date.getTime()) ? trimmed.slice(0, 10) : formatDateOnly(date)
}

function normalizeDateTimeDefault(value: string) {
  const trimmed = value.trim()
  if (!trimmed) {
    return formatDateTimeForJira(new Date())
  }

  if (/[tT]\d{2}:\d{2}/.test(trimmed)) {
    return trimmed
  }

  const date = new Date(trimmed)
  if (Number.isNaN(date.getTime())) {
    return `${trimmed.slice(0, 10)}T${formatTimeOnly(new Date())}.000${formatTimezoneOffset(new Date())}`
  }

  const now = new Date()
  date.setHours(now.getHours(), now.getMinutes(), 0, 0)
  return formatDateTimeForJira(date)
}

function formatDateOnly(date: Date) {
  const year = date.getFullYear()
  const month = pad2(date.getMonth() + 1)
  const day = pad2(date.getDate())
  return `${year}-${month}-${day}`
}

function formatTimeOnly(date: Date) {
  return `${pad2(date.getHours())}:${pad2(date.getMinutes())}:00`
}

function formatDateTimeForJira(date: Date) {
  return `${formatDateOnly(date)}T${formatTimeOnly(date)}.000${formatTimezoneOffset(date)}`
}

function formatTimezoneOffset(date: Date) {
  const offset = -date.getTimezoneOffset()
  const sign = offset >= 0 ? '+' : '-'
  const absolute = Math.abs(offset)
  const hours = pad2(Math.floor(absolute / 60))
  const minutes = pad2(absolute % 60)
  return `${sign}${hours}${minutes}`
}

function pad2(value: number) {
  return String(value).padStart(2, '0')
}

function parseAllowedValuesJson(raw: string): AllowedValue[] {
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
    }))
}

function allowedValueLabel(value: AllowedValue) {
  const primary = value.value ?? value.name ?? value.key ?? value.id ?? '-'
  const details = [
    value.name && value.name !== primary ? value.name : '',
    value.id && value.id !== primary ? `id:${value.id}` : '',
  ].filter(Boolean)

  return details.length ? `${primary} (${details.join(', ')})` : primary
}

function allowedValueOutput(mapping: IssueFieldMappingAdminResponse, value: AllowedValue) {
  if (mapping.jiraField === 'componentIds' || mapping.valueShape === 'id' || mapping.valueShape === 'arrayOfId') {
    return value.id ?? value.value ?? value.name ?? value.key ?? ''
  }
  if (mapping.valueShape === 'name' || mapping.valueShape === 'arrayOfName') {
    return value.name ?? value.value ?? value.id ?? value.key ?? ''
  }
  if (mapping.valueShape === 'value') {
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

defineExpose({
  validate,
  buildData,
})
</script>

<style scoped>
.worklog-editor {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
}

.worklog-entry {
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  padding: 12px;
  background: var(--el-fill-color-light);
}

.worklog-entry__grid {
  display: grid;
  grid-template-columns: minmax(220px, 1fr) minmax(140px, 180px) auto;
  gap: 12px;
  align-items: end;
}

.worklog-entry__comment {
  margin-top: 12px;
}

.worklog-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
  color: var(--el-text-color-regular);
  font-size: 13px;
}

@media (max-width: 720px) {
  .worklog-entry__grid {
    grid-template-columns: 1fr;
  }
}
</style>
