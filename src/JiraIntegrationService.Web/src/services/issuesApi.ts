import { apiRequest } from './http'
import type {
  CreateIssueRequest,
  CreateIssuePreviewResult,
  CreateIssueResult,
  GetIssueStatusRequest,
  GetIssueStatusResult,
  UpdateIssueStatusRequest,
  UpdateIssueStatusResult,
} from '../types/issues'

export const issuesApi = {
  createIssue(payload: CreateIssueRequest) {
    return apiRequest<CreateIssueResult>({
      method: 'POST',
      url: '/issues/create',
      data: payload,
    })
  },

  previewCreateIssue(payload: CreateIssueRequest) {
    return apiRequest<CreateIssuePreviewResult>({
      method: 'POST',
      url: '/issues/create/preview',
      data: payload,
    })
  },

  updateIssueStatus(payload: UpdateIssueStatusRequest) {
    return apiRequest<UpdateIssueStatusResult>({
      method: 'POST',
      url: '/issues/status/update',
      data: payload,
    })
  },

  getIssueStatus(params: GetIssueStatusRequest) {
    return apiRequest<GetIssueStatusResult>({
      method: 'GET',
      url: '/issues/status',
      params,
    })
  },
}
