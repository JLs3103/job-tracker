import type {
  CandidateProfile,
  JobPosting,
  JobSource,
  JobStatus,
  RefreshResponse,
  RefreshStateResponse,
} from '../types/job'

interface JobFilters {
  status?: JobStatus | 'ALL'
  source?: JobSource | 'ALL'
  search?: string
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5227'
export const isMockApi = import.meta.env.VITE_USE_MOCK_API !== 'false'
const mockSources: JobSource[] = [
  'Disnakerja',
  'Bukajobs',
  'BrowseLoker',
  'LokerBlog',
  'LokerBUMN',
  'BersamaBUMN',
  'OpenKerja',
]

const hoursAgo = (hours: number) =>
  new Date(Date.now() - hours * 60 * 60 * 1000).toISOString()

const mockJobs: JobPosting[] = [
  {
    id: 1,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-cloud-backend',
    source: 'Disnakerja',
    companyName: 'Demo Cloud Nusantara',
    jobTitle: 'Backend Developer',
    qualifications: [
      'Menguasai C# atau Node.js',
      'Memahami REST API dan SQL',
      'Terbuka untuk lulusan baru',
    ],
    targetRoles: ['Backend Developer', 'Full Stack Developer'],
    technicalSkills: ['C#', 'Node.js', 'REST API', 'SQL'],
    location: 'Jakarta Selatan',
    matchScore: 86,
    matchReason: 'Cocok dengan skill C#, REST API, dan terbuka untuk lulusan baru.',
    status: 'NEW',
    publishedAt: hoursAgo(2),
    createdAt: hoursAgo(2),
  },
  {
    id: 2,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-security-analyst',
    source: 'Bukajobs',
    companyName: 'Demo Sagara Security',
    jobTitle: 'Junior SOC Analyst',
    qualifications: [
      'Memahami dasar jaringan dan Linux',
      'Tertarik pada incident response',
      'Bersedia bekerja dengan sistem shift',
    ],
    targetRoles: ['SOC Analyst', 'IT Security / Blue Team'],
    technicalSkills: ['Linux', 'Jaringan', 'Incident Response', 'SOC'],
    location: 'Bandung',
    matchScore: 91,
    matchReason: 'Sesuai target Blue Team / SOC Analyst dan pemahaman dasar jaringan serta Linux.',
    status: 'NEW',
    publishedAt: hoursAgo(5),
    createdAt: hoursAgo(5),
  },
  {
    id: 3,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-frontend-engineer',
    source: 'BrowseLoker',
    companyName: 'Demo Karsa Digital',
    jobTitle: 'Frontend Engineer',
    qualifications: [
      'Menguasai React dan TypeScript',
      'Memahami responsive design',
      'Memiliki portofolio proyek web',
    ],
    targetRoles: ['Frontend Developer'],
    technicalSkills: ['React', 'TypeScript', 'Responsive Design'],
    location: 'Yogyakarta',
    matchScore: 94,
    matchReason: 'Sangat cocok dengan keahlian React dan TypeScript untuk posisi Frontend Engineer.',
    status: 'NEW',
    publishedAt: hoursAgo(9),
    createdAt: hoursAgo(9),
  },
  {
    id: 4,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-qa-engineer',
    source: 'LokerBlog',
    companyName: 'Demo Awan Teknologi',
    jobTitle: 'QA Engineer',
    qualifications: [
      'Memahami software testing',
      'Teliti dalam menulis test case',
    ],
    targetRoles: ['Software QA Engineer'],
    technicalSkills: ['Software Testing', 'Test Case'],
    location: 'Remote',
    matchScore: 62,
    matchReason: 'Relevan di bidang IT, namun fokus pada QA testing dibandingkan pengembangan atau security.',
    status: 'VIEWED',
    publishedAt: hoursAgo(28),
    createdAt: hoursAgo(28),
  },
  {
    id: 5,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-cloud-ops',
    source: 'Disnakerja',
    companyName: 'Demo Arunika Systems',
    jobTitle: 'Cloud Operations Associate',
    qualifications: [
      'Memahami Linux dan jaringan komputer',
      'Mengenal layanan cloud',
    ],
    targetRoles: ['Cloud Operations', 'Sysadmin'],
    technicalSkills: ['Linux', 'Jaringan', 'Cloud'],
    location: 'Surabaya',
    matchScore: 74,
    matchReason: 'Mendukung keahlian Linux dan jaringan komputer pada lingkungan cloud.',
    status: 'SAVED',
    publishedAt: hoursAgo(44),
    createdAt: hoursAgo(44),
  },
  {
    id: 6,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-data-analyst',
    source: 'Bukajobs',
    companyName: 'Demo Loka Data',
    jobTitle: 'Junior Data Analyst',
    qualifications: [
      'Menguasai SQL dasar',
      'Mampu menyampaikan temuan dengan jelas',
    ],
    targetRoles: ['Data Analyst'],
    technicalSkills: ['SQL'],
    location: 'Jakarta',
    matchScore: 55,
    matchReason: 'Menggunakan SQL dasar, namun peran berfokus pada analisis data bisnis.',
    status: 'APPLIED',
    publishedAt: hoursAgo(72),
    createdAt: hoursAgo(72),
  },
  {
    id: 7,
    canRetryExtraction: false,
    externalUrl: 'https://example.com/jobs/demo-it-support',
    source: 'BrowseLoker',
    companyName: 'Demo Reka Informatika',
    jobTitle: 'IT Support',
    qualifications: [
      'Memahami troubleshooting perangkat',
      'Komunikatif dan berorientasi pada layanan',
    ],
    targetRoles: ['IT Support'],
    technicalSkills: ['Troubleshooting', 'Hardware', 'Software'],
    location: 'Semarang',
    matchScore: 78,
    matchReason: 'Sesuai dengan target posisi IT Support dan troubleshooting perangkat.',
    status: 'IGNORED',
    publishedAt: hoursAgo(96),
    createdAt: hoursAgo(96),
  },
]

let refreshCount = 0
let mockLastRefresh: string | null = null
let mockProfile: CandidateProfile = {
  targetRoles: [
    'Full Stack Developer',
    'Frontend Developer',
    'IT Security / Blue Team',
    'SOC Analyst',
    'IT Support',
  ],
  technicalSkills: [
    'React',
    'TypeScript',
    'C#',
    '.NET',
    'Linux',
    'Jaringan',
    'SOC',
  ],
  experienceLevel: 'Fresh Graduate',
  summary:
    'Lulusan baru IT dengan fokus Full Stack, Frontend, IT Security / Blue Team, dan IT Support.',
}

function wait(milliseconds: number) {
  return new Promise((resolve) => window.setTimeout(resolve, milliseconds))
}

async function readResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.text()
    let detail = body || response.statusText
    if (body && response.headers.get('content-type')?.includes('json')) {
      const problem = JSON.parse(body) as {
        detail?: unknown
        title?: unknown
      }
      if (typeof problem.detail === 'string') {
        detail = problem.detail
      } else if (typeof problem.title === 'string') {
        detail = problem.title
      }
    }

    throw new Error(
      `Backend error (${response.status}): ${detail}`,
    )
  }

  return (await response.json()) as T
}

function filterJobs(jobs: JobPosting[], filters: JobFilters) {
  const search = filters.search?.trim().toLocaleLowerCase()

  return jobs
    .filter((job) => !filters.status || filters.status === 'ALL' || job.status === filters.status)
    .filter((job) => !filters.source || filters.source === 'ALL' || job.source === filters.source)
    .filter((job) => {
      if (!search) return true

      return [
        job.companyName,
        job.jobTitle,
        job.location ?? '',
        ...job.qualifications,
      ]
        .join(' ')
        .toLocaleLowerCase()
        .includes(search)
    })
    .sort(
      (first, second) =>
        new Date(second.publishedAt).getTime() -
        new Date(first.publishedAt).getTime(),
    )
}

export async function getJobs(filters: JobFilters = {}): Promise<JobPosting[]> {
  if (isMockApi) {
    await wait(180)
    return filterJobs(mockJobs, filters)
  }

  const params = new URLSearchParams({
    status: filters.status ?? 'ALL',
    source: filters.source ?? 'ALL',
    search: filters.search ?? '',
  })
  const response = await fetch(`${apiBaseUrl}/api/jobs?${params}`)
  return readResponse<JobPosting[]>(response)
}

export async function refreshJobs(): Promise<RefreshResponse> {
  if (isMockApi) {
    await wait(650)
    refreshCount += 1
    const checkedAt = new Date().toISOString()
    const hasNewJob = refreshCount % 3 !== 0

    if (hasNewJob) {
      const id = Math.max(...mockJobs.map((job) => job.id)) + 1
      const source = mockSources[(refreshCount - 1) % mockSources.length]
      mockJobs.unshift({
        id,
        canRetryExtraction: false,
        externalUrl: `https://example.com/jobs/demo-refresh-${id}`,
        source,
        companyName: `Demo Talenta Digital ${id}`,
        jobTitle: 'IT Support Engineer',
        qualifications: [
          'Memahami sistem operasi dan jaringan dasar',
          'Mampu menyelesaikan masalah secara sistematis',
          'Terbuka untuk lulusan baru',
        ],
        targetRoles: ['IT Support'],
        technicalSkills: ['Sistem Operasi', 'Jaringan'],
        location: 'Indonesia',
        matchScore: 80,
        matchReason: 'Cocok untuk lulusan baru dengan target IT Support.',
        status: 'NEW',
        publishedAt: checkedAt,
        createdAt: checkedAt,
      })
    }

    const newJobsCount = hasNewJob ? 1 : 0
    mockLastRefresh = checkedAt
    return {
      newJobsCount,
      message: hasNewJob
        ? `Ditemukan ${newJobsCount} lowongan IT baru.`
        : 'Tidak ada lowongan IT baru sejak refresh terakhir.',
      checkedAt,
    }
  }

  const response = await fetch(`${apiBaseUrl}/api/jobs/refresh`, {
    method: 'POST',
  })
  return readResponse<RefreshResponse>(response)
}

export async function getRefreshState(): Promise<RefreshStateResponse> {
  if (isMockApi) {
    return { lastSuccessfulRefreshAt: mockLastRefresh }
  }

  const response = await fetch(`${apiBaseUrl}/api/jobs/refresh-state`)
  return readResponse<RefreshStateResponse>(response)
}

export async function updateJobStatus(
  id: number,
  status: JobStatus,
): Promise<JobPosting> {
  if (isMockApi) {
    await wait(160)
    const job = mockJobs.find((item) => item.id === id)
    if (!job) throw new Error(`Lowongan dengan ID ${id} tidak ditemukan.`)

    job.status = status
    return { ...job }
  }

  const response = await fetch(`${apiBaseUrl}/api/jobs/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status }),
  })
  return readResponse<JobPosting>(response)
}

export async function retryJobExtraction(id: number): Promise<JobPosting> {
  if (isMockApi) {
    throw new Error('Ekstraksi AI tidak tersedia dalam mode demo.')
  }

  const response = await fetch(`${apiBaseUrl}/api/jobs/${id}/reextract`, {
    method: 'POST',
  })
  return readResponse<JobPosting>(response)
}

export async function getProfile(): Promise<CandidateProfile> {
  if (isMockApi) {
    return {
      ...mockProfile,
      targetRoles: [...mockProfile.targetRoles],
      technicalSkills: [...mockProfile.technicalSkills],
    }
  }

  const response = await fetch(`${apiBaseUrl}/api/profile`)
  return readResponse<CandidateProfile>(response)
}

export async function updateProfile(
  profile: CandidateProfile,
): Promise<CandidateProfile> {
  if (isMockApi) {
    mockProfile = {
      ...profile,
      targetRoles: [...profile.targetRoles],
      technicalSkills: [...profile.technicalSkills],
    }
    return getProfile()
  }

  const response = await fetch(`${apiBaseUrl}/api/profile`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(profile),
  })
  return readResponse<CandidateProfile>(response)
}

export async function parseCvProfile(file: File): Promise<CandidateProfile> {
  if (isMockApi) {
    await wait(500)
    return {
      targetRoles: ['Full Stack Developer', 'Frontend Developer', 'Backend Developer'],
      technicalSkills: ['React', 'TypeScript', 'C#', '.NET', 'PostgreSQL', 'Docker'],
      experienceLevel: 'Fresh Graduate',
      summary: `Ringkasan otomatis dari ${file.name}: lulusan baru IT dengan fokus pengembangan aplikasi web React, TypeScript, dan .NET.`,
    }
  }

  const formData = new FormData()
  formData.append('file', file)
  const response = await fetch(`${apiBaseUrl}/api/profile/parse-cv`, {
    method: 'POST',
    body: formData,
  })
  return readResponse<CandidateProfile>(response)
}
