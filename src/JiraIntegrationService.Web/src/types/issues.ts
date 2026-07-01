export type IssueData = Record<string, unknown>

export interface CreateIssueRequest {
  productCode: string
  issueTypeCode: string
  templateCode?: string
  data: IssueData
}

export interface CreateIssueResult {
  jiraIssueId?: string
  jiraIssueKey?: string
}

export interface CreateIssuePreviewResult {
  jiraRequest: JiraCreateIssueRequest
}

export interface JiraCreateIssueRequest {
  projectKey: string
  issueTypeName?: string
  summary: string
  description?: string
  priorityName?: string
  reporterName?: string
  assigneeName?: string
  customFields?: Record<string, unknown>
  issueTypeId?: string
  parentKey?: string
  componentIds?: string[]
  worklogs?: JiraWorklogEntry[]
}

export interface JiraWorklogEntry {
  started: string
  timeSpent: string
  comment?: string
}

export interface UpdateIssueStatusRequest {
  productCode: string
  jiraIssueId?: string
  jiraIssueKey?: string
  issueTypeCode?: string
  standardStatus: string
}

export interface UpdateIssueStatusResult {
  jiraIssueId?: string
  jiraIssueKey?: string
  standardStatus: string
}

export interface GetIssueStatusRequest {
  productCode: string
  jiraIssueId?: string
  jiraIssueKey?: string
  issueTypeCode?: string
}

export interface GetIssueStatusResult {
  standardStatus: string
}
