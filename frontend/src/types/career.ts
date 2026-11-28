export const applicationResults = ['InProgress', 'Rejected', 'Accepted'] as const

export type ApplicationResult = (typeof applicationResults)[number]

export interface JobApplication {
  id: number
  company: string
  jobTitle: string
  appliedVia: string
  appliedLink: string | null
  appliedDate: string
  lastUpdated: string
  status: string
  result: ApplicationResult
  sourceJobPostingId: number | null
}

export interface CreateJobApplicationRequest {
  company: string
  jobTitle: string
  appliedVia: string
  appliedLink?: string | null
  appliedDate?: string
  lastUpdated?: string
  status?: string
  result?: ApplicationResult
}

export interface UpdateJobApplicationRequest {
  company?: string
  jobTitle?: string
  appliedVia?: string
  appliedLink?: string | null
  appliedDate?: string
  lastUpdated?: string
  status?: string
  result?: ApplicationResult
}

export interface OnGoingJob {
  id: number
  company: string
  jobTitle: string
  link: string
  notes: string | null
  createdAt: string
}

export interface CreateOnGoingJobRequest {
  company: string
  jobTitle: string
  link: string
  notes?: string | null
}

export interface CareerSite {
  id: number
  name: string
  url: string
  lastJobUpdated: string | null
  lastCheckedAt: string | null
}

export interface CreateCareerSiteRequest {
  name: string
  url: string
  lastJobUpdated?: string | null
}

export interface UpdateCareerSiteRequest {
  name?: string
  url?: string
  lastJobUpdated?: string | null
}
