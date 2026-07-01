export interface CreateProductAdminRequest {
  code: string
  name: string
  jiraProjectKey: string
  jiraBaseUrl: string
  jiraApiBasePath?: string
  jiraVersion?: string
  isActive: boolean
}

export interface UpdateProductAdminRequest {
  name: string
  jiraProjectKey: string
  jiraBaseUrl: string
  jiraApiBasePath?: string
  jiraVersion?: string
  isActive: boolean
}

export interface ProductAdminResponse {
  id: number
  code: string
  name: string
  jiraProjectKey: string
  jiraBaseUrl: string
  jiraApiBasePath: string
  jiraVersion: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface UpsertJiraCredentialAdminRequest {
  authType?: string
  username: string
  passwordOrToken: string
  isActive: boolean
}

export interface JiraCredentialAdminResponse {
  id: number
  productId: number
  authType: string
  username: string
  hasPasswordOrToken: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateIssueTypeMappingAdminRequest {
  issueTypeCode: string
  jiraIssueTypeId?: string
  jiraIssueTypeName?: string
  isActive: boolean
}

export interface UpdateIssueTypeMappingAdminRequest {
  jiraIssueTypeId?: string
  jiraIssueTypeName?: string
  isActive: boolean
}

export interface IssueTypeMappingAdminResponse {
  id: number
  productId: number
  issueTypeCode: string
  jiraIssueTypeId?: string
  jiraIssueTypeName?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface SyncIssueTypesAdminResponse {
  productCode: string
  total: number
  issueTypes: IssueTypeMappingAdminResponse[]
}

export interface JiraAllowedValueAdminResponse {
  id?: string
  key?: string
  name?: string
  value?: string
  description?: string
  disabled: boolean
  rawJson: string
}

export interface JiraFieldMetadataAdminResponse {
  fieldId: string
  name: string
  required: boolean
  schemaType?: string
  schemaItems?: string
  schemaSystem?: string
  schemaCustom?: string
  schemaCustomId?: number
  hasDefaultValue: boolean
  defaultValueJson?: string
  autoCompleteUrl?: string
  operations: string[]
  allowedValues: JiraAllowedValueAdminResponse[]
  recommendedValueType: string
  recommendedValueShape: string
  updatedAt?: string
}

export interface JiraFieldsMetadataAdminResponse {
  productCode: string
  issueTypeCode: string
  updatedAt?: string
  total: number
  fields: JiraFieldMetadataAdminResponse[]
}

export interface CreateFieldMappingTemplateAdminRequest {
  templateCode: string
  name: string
  description?: string
  sourceTemplateCode?: string
  copyMappings: boolean
  isActive: boolean
}

export interface FieldMappingTemplateAdminResponse {
  id: number
  productId: number
  issueTypeMappingId: number
  templateCode: string
  name: string
  description?: string
  isDefault: boolean
  isActive: boolean
  mappingCount: number
  createdAt: string
  updatedAt: string
}

export interface UpsertIssueFieldMappingAdminRequest {
  sourcePath: string
  jiraField: string
  jiraFieldName?: string
  jiraFieldDescription?: string
  jiraSchemaType?: string
  jiraSchemaItems?: string
  jiraSchemaSystem?: string
  jiraSchemaCustom?: string
  jiraAllowedValuesJson?: string
  jiraDefaultValueJson?: string
  jiraAutoCompleteUrl?: string
  valueType?: string
  valueShape?: string
  isRequired: boolean
  defaultValue?: string
  sortOrder: number
  isActive: boolean
  transformConfigJson?: string
}

export interface IssueFieldMappingAdminResponse {
  id: number
  productId: number
  issueTypeMappingId?: number
  templateCode: string
  sourcePath: string
  jiraField: string
  jiraFieldName?: string
  jiraFieldDescription?: string
  jiraSchemaType?: string
  jiraSchemaItems?: string
  jiraSchemaSystem?: string
  jiraSchemaCustom?: string
  jiraAllowedValuesJson?: string
  jiraDefaultValueJson?: string
  jiraAutoCompleteUrl?: string
  valueType: string
  valueShape: string
  isRequired: boolean
  defaultValue?: string
  sortOrder: number
  isActive: boolean
  transformConfigJson?: string
  createdAt: string
  updatedAt: string
}

export interface SetDefaultFieldMappingsAdminResponse {
  productCode: string
  issueTypeCode: string
  total: number
  fieldMappings: IssueFieldMappingAdminResponse[]
}

export interface UpsertStatusMappingAdminRequest {
  standardStatus: string
  jiraStatusName: string
  jiraTransitionId?: string
  jiraTransitionName?: string
  isActive: boolean
}

export interface StatusMappingAdminResponse {
  id: number
  productId: number
  issueTypeMappingId?: number
  standardStatus: string
  jiraStatusName: string
  jiraTransitionId?: string
  jiraTransitionName?: string
  isActive: boolean
}

export interface ValidateCreateIssueConfigAdminRequest {
  issueTypeCode?: string
}

export interface ValidateCreateIssueConfigAdminResponse {
  productCode: string
  issueTypeCode?: string
  isValid: boolean
  errors: string[]
}

export interface DeleteAdminResponse {
  deleted: boolean
}
