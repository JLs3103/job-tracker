import type {
  ApplicationResult,
  CareerSite,
  CreateCareerSiteRequest,
  CreateJobApplicationRequest,
  CreateOnGoingJobRequest,
  JobApplication,
  OnGoingJob,
  UpdateCareerSiteRequest,
  UpdateJobApplicationRequest,
} from '../types/career'
import { isMockApi } from './jobApi'

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ??
  (import.meta.env.DEV ? 'http://localhost:5227' : '')

const mockApplications: JobApplication[] = []
const mockOnGoing: OnGoingJob[] = []
const mockSites: CareerSite[] = []

async function readCareerResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.text()
    let detail = body || response.statusText
    if (body && response.headers.get('content-type')?.includes('json')) {
      const problem = JSON.parse(body) as { detail?: unknown; title?: unknown }
      if (typeof problem.detail === 'string') detail = problem.detail
      else if (typeof problem.title === 'string') detail = problem.title
    }
    throw new Error(`Backend error (${response.status}): ${detail}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export async function getApplications(filters?: {
  result?: ApplicationResult | 'ALL'
  search?: string
}): Promise<JobApplication[]> {
  if (isMockApi) return [...mockApplications]

  const params = new URLSearchParams()
  if (filters?.result && filters.result !== 'ALL') {
    params.set('result', filters.result)
  }
  if (filters?.search?.trim()) {
    params.set('search', filters.search.trim())
  }

  const query = params.toString()
  const response = await fetch(
    `${apiBaseUrl}/api/career/applications${query ? `?${query}` : ''}`,
  )
  return readCareerResponse<JobApplication[]>(response)
}

export async function createApplication(
  request: CreateJobApplicationRequest,
): Promise<JobApplication> {
  if (isMockApi) {
    const now = new Date().toISOString()
    const item: JobApplication = {
      id: Date.now(),
      company: request.company,
      jobTitle: request.jobTitle,
      appliedVia: request.appliedVia,
      appliedLink: request.appliedLink ?? null,
      appliedDate: request.appliedDate ?? now,
      lastUpdated: request.lastUpdated ?? request.appliedDate ?? now,
      status: request.status ?? 'Applied',
      result: request.result ?? 'InProgress',
      sourceJobPostingId: null,
    }
    mockApplications.unshift(item)
    return item
  }

  const response = await fetch(`${apiBaseUrl}/api/career/applications`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  return readCareerResponse<JobApplication>(response)
}

export async function updateApplication(
  id: number,
  request: UpdateJobApplicationRequest,
): Promise<JobApplication> {
  if (isMockApi) {
    const index = mockApplications.findIndex((item) => item.id === id)
    if (index === -1) throw new Error('Lamaran tidak ditemukan.')
    const updated: JobApplication = {
      ...mockApplications[index],
      ...request,
      lastUpdated: request.lastUpdated ?? new Date().toISOString(),
    }
    mockApplications[index] = updated
    return updated
  }

  const response = await fetch(`${apiBaseUrl}/api/career/applications/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  return readCareerResponse<JobApplication>(response)
}

export async function deleteApplication(id: number): Promise<void> {
  if (isMockApi) {
    const index = mockApplications.findIndex((item) => item.id === id)
    if (index !== -1) mockApplications.splice(index, 1)
    return
  }

  const response = await fetch(`${apiBaseUrl}/api/career/applications/${id}`, {
    method: 'DELETE',
  })
  await readCareerResponse<void>(response)
}

export async function getOnGoingJobs(search?: string): Promise<OnGoingJob[]> {
  if (isMockApi) return [...mockOnGoing]

  const params = new URLSearchParams()
  if (search?.trim()) params.set('search', search.trim())
  const query = params.toString()
  const response = await fetch(
    `${apiBaseUrl}/api/career/ongoing${query ? `?${query}` : ''}`,
  )
  return readCareerResponse<OnGoingJob[]>(response)
}

export async function createOnGoingJob(
  request: CreateOnGoingJobRequest,
): Promise<OnGoingJob> {
  if (isMockApi) {
    const item: OnGoingJob = {
      id: Date.now(),
      company: request.company,
      jobTitle: request.jobTitle,
      link: request.link,
      notes: request.notes ?? null,
      createdAt: new Date().toISOString(),
    }
    mockOnGoing.unshift(item)
    return item
  }

  const response = await fetch(`${apiBaseUrl}/api/career/ongoing`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  return readCareerResponse<OnGoingJob>(response)
}

export async function moveOnGoingToApplied(
  id: number,
  appliedVia?: string,
): Promise<JobApplication> {
  if (isMockApi) {
    const index = mockOnGoing.findIndex((item) => item.id === id)
    if (index === -1) throw new Error('Item On Going tidak ditemukan.')
    const [removed] = mockOnGoing.splice(index, 1)
    return createApplication({
      company: removed.company,
      jobTitle: removed.jobTitle,
      appliedVia: appliedVia ?? removed.notes ?? 'Career Site',
      appliedLink: removed.link,
    })
  }

  const response = await fetch(`${apiBaseUrl}/api/career/ongoing/${id}/apply`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ appliedVia: appliedVia ?? null }),
  })
  return readCareerResponse<JobApplication>(response)
}

export async function deleteOnGoingJob(id: number): Promise<void> {
  if (isMockApi) {
    const index = mockOnGoing.findIndex((item) => item.id === id)
    if (index !== -1) mockOnGoing.splice(index, 1)
    return
  }

  const response = await fetch(`${apiBaseUrl}/api/career/ongoing/${id}`, {
    method: 'DELETE',
  })
  await readCareerResponse<void>(response)
}

export async function getCareerSites(): Promise<CareerSite[]> {
  if (isMockApi) return [...mockSites]

  const response = await fetch(`${apiBaseUrl}/api/career/sites`)
  return readCareerResponse<CareerSite[]>(response)
}

export async function createCareerSite(
  request: CreateCareerSiteRequest,
): Promise<CareerSite> {
  if (isMockApi) {
    const item: CareerSite = {
      id: Date.now(),
      name: request.name,
      url: request.url,
      lastJobUpdated: request.lastJobUpdated ?? null,
      lastCheckedAt: request.lastJobUpdated ? new Date().toISOString() : null,
    }
    mockSites.push(item)
    return item
  }

  const response = await fetch(`${apiBaseUrl}/api/career/sites`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  return readCareerResponse<CareerSite>(response)
}

export async function updateCareerSite(
  id: number,
  request: UpdateCareerSiteRequest,
): Promise<CareerSite> {
  if (isMockApi) {
    const index = mockSites.findIndex((item) => item.id === id)
    if (index === -1) throw new Error('Career site tidak ditemukan.')
    const updated: CareerSite = {
      ...mockSites[index],
      ...request,
      lastCheckedAt: new Date().toISOString(),
    }
    mockSites[index] = updated
    return updated
  }

  const response = await fetch(`${apiBaseUrl}/api/career/sites/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  return readCareerResponse<CareerSite>(response)
}

export async function deleteCareerSite(id: number): Promise<void> {
  if (isMockApi) {
    const index = mockSites.findIndex((item) => item.id === id)
    if (index !== -1) mockSites.splice(index, 1)
    return
  }

  const response = await fetch(`${apiBaseUrl}/api/career/sites/${id}`, {
    method: 'DELETE',
  })
  await readCareerResponse<void>(response)
}
