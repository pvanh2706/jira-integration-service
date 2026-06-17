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

export interface UpsertIssueFieldMappingAdminRequest {
  sourcePath: string
  jiraField: string
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
  sourcePath: string
  jiraField: string
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
