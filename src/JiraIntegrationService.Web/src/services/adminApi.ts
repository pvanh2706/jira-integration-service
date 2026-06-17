import { apiRequest } from './http'
import type {
  CreateIssueTypeMappingAdminRequest,
  CreateProductAdminRequest,
  DeleteAdminResponse,
  IssueFieldMappingAdminResponse,
  IssueTypeMappingAdminResponse,
  JiraCredentialAdminResponse,
  ProductAdminResponse,
  StatusMappingAdminResponse,
  UpdateIssueTypeMappingAdminRequest,
  UpdateProductAdminRequest,
  UpsertIssueFieldMappingAdminRequest,
  UpsertJiraCredentialAdminRequest,
  UpsertStatusMappingAdminRequest,
  ValidateCreateIssueConfigAdminRequest,
  ValidateCreateIssueConfigAdminResponse,
} from '../types/admin'

export const adminApi = {
  getProducts() {
    return apiRequest<ProductAdminResponse[]>({
      method: 'GET',
      url: '/admin/products',
    })
  },

  createProduct(payload: CreateProductAdminRequest) {
    return apiRequest<ProductAdminResponse>({
      method: 'POST',
      url: '/admin/products',
      data: payload,
    })
  },

  getProduct(code: string) {
    return apiRequest<ProductAdminResponse>({
      method: 'GET',
      url: `/admin/products/${segment(code)}`,
    })
  },

  updateProduct(code: string, payload: UpdateProductAdminRequest) {
    return apiRequest<ProductAdminResponse>({
      method: 'PUT',
      url: `/admin/products/${segment(code)}`,
      data: payload,
    })
  },

  deleteProduct(code: string) {
    return apiRequest<DeleteAdminResponse>({
      method: 'DELETE',
      url: `/admin/products/${segment(code)}`,
    })
  },

  getCredential(code: string) {
    return apiRequest<JiraCredentialAdminResponse>({
      method: 'GET',
      url: `/admin/products/${segment(code)}/credential`,
    })
  },

  upsertCredential(code: string, payload: UpsertJiraCredentialAdminRequest) {
    return apiRequest<JiraCredentialAdminResponse>({
      method: 'PUT',
      url: `/admin/products/${segment(code)}/credential`,
      data: payload,
    })
  },

  getIssueTypes(code: string) {
    return apiRequest<IssueTypeMappingAdminResponse[]>({
      method: 'GET',
      url: `/admin/products/${segment(code)}/issue-types`,
    })
  },

  createIssueType(code: string, payload: CreateIssueTypeMappingAdminRequest) {
    return apiRequest<IssueTypeMappingAdminResponse>({
      method: 'POST',
      url: `/admin/products/${segment(code)}/issue-types`,
      data: payload,
    })
  },

  updateIssueType(
    code: string,
    issueTypeCode: string,
    payload: UpdateIssueTypeMappingAdminRequest,
  ) {
    return apiRequest<IssueTypeMappingAdminResponse>({
      method: 'PUT',
      url: `/admin/products/${segment(code)}/issue-types/${segment(issueTypeCode)}`,
      data: payload,
    })
  },

  getFieldMappings(code: string, issueTypeCode: string) {
    return apiRequest<IssueFieldMappingAdminResponse[]>({
      method: 'GET',
      url: `/admin/products/${segment(code)}/issue-types/${segment(issueTypeCode)}/field-mappings`,
    })
  },

  createFieldMapping(
    code: string,
    issueTypeCode: string,
    payload: UpsertIssueFieldMappingAdminRequest,
  ) {
    return apiRequest<IssueFieldMappingAdminResponse>({
      method: 'POST',
      url: `/admin/products/${segment(code)}/issue-types/${segment(issueTypeCode)}/field-mappings`,
      data: payload,
    })
  },

  updateFieldMapping(id: number, payload: UpsertIssueFieldMappingAdminRequest) {
    return apiRequest<IssueFieldMappingAdminResponse>({
      method: 'PUT',
      url: `/admin/field-mappings/${id}`,
      data: payload,
    })
  },

  deleteFieldMapping(id: number) {
    return apiRequest<DeleteAdminResponse>({
      method: 'DELETE',
      url: `/admin/field-mappings/${id}`,
    })
  },

  getStatusMappings(code: string, issueTypeCode: string) {
    return apiRequest<StatusMappingAdminResponse[]>({
      method: 'GET',
      url: `/admin/products/${segment(code)}/issue-types/${segment(issueTypeCode)}/status-mappings`,
    })
  },

  createStatusMapping(
    code: string,
    issueTypeCode: string,
    payload: UpsertStatusMappingAdminRequest,
  ) {
    return apiRequest<StatusMappingAdminResponse>({
      method: 'POST',
      url: `/admin/products/${segment(code)}/issue-types/${segment(issueTypeCode)}/status-mappings`,
      data: payload,
    })
  },

  updateStatusMapping(id: number, payload: UpsertStatusMappingAdminRequest) {
    return apiRequest<StatusMappingAdminResponse>({
      method: 'PUT',
      url: `/admin/status-mappings/${id}`,
      data: payload,
    })
  },

  deleteStatusMapping(id: number) {
    return apiRequest<DeleteAdminResponse>({
      method: 'DELETE',
      url: `/admin/status-mappings/${id}`,
    })
  },

  validateCreateIssueConfig(code: string, payload: ValidateCreateIssueConfigAdminRequest) {
    return apiRequest<ValidateCreateIssueConfigAdminResponse>({
      method: 'POST',
      url: `/admin/products/${segment(code)}/validate-create-issue-config`,
      data: payload,
    })
  },
}

function segment(value: string) {
  return encodeURIComponent(value.trim())
}
