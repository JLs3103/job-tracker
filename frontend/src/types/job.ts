export const jobStatuses = [
  'NEW',
  'VIEWED',
  'SAVED',
  'APPLIED',
  'IGNORED',
] as const

export type JobStatus = (typeof jobStatuses)[number]

export const jobSources = [
  'Disnakerja',
  'Bukajobs',
  'BrowseLoker',
  'LokerBlog',
] as const

export type JobSource = (typeof jobSources)[number]

export type JobSortOrder = 'newest' | 'oldest' | 'match'

export interface CandidateProfile {
  targetRoles: string[]
  technicalSkills: string[]
  experienceLevel: string
  summary: string
}

export interface JobPosting {
  id: number
  canRetryExtraction: boolean
  externalUrl: string
  source: JobSource
  companyName: string
  jobTitle: string
  qualifications: string[]
  targetRoles: string[]
  technicalSkills: string[]
  location: string | null
  matchScore: number | null
  matchReason: string | null
  status: JobStatus
  publishedAt: string
  createdAt: string
}

export interface RefreshResponse {
  newJobsCount: number
  message: string
  checkedAt: string
}

export interface RefreshStateResponse {
  lastSuccessfulRefreshAt: string | null
}
