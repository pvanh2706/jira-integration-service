export interface ProductFormModel {
  code: string
  name: string
  jiraProjectKey: string
  jiraBaseUrl: string
  jiraApiBasePath: string
  jiraVersion: string
  isActive: boolean
}

export interface CredentialFormModel {
  authType: string
  username: string
  passwordOrToken: string
  isActive: boolean
}

export const DEFAULT_PRODUCT_FORM: ProductFormModel = {
  code: '',
  name: '',
  jiraProjectKey: '',
  jiraBaseUrl: '',
  jiraApiBasePath: '/rest/api/2',
  jiraVersion: 'ServerV2',
  isActive: true,
}

export const DEFAULT_CREDENTIAL_FORM: CredentialFormModel = {
  authType: 'Basic',
  username: '',
  passwordOrToken: '',
  isActive: true,
}
