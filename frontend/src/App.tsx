import { useCallback, useEffect, useMemo, useState } from 'react'
import { EmptyState } from './components/EmptyState'
import { Header } from './components/Header'
import { JobCard } from './components/JobCard'
import { JobFilters } from './components/JobFilters'
import { RefreshBanner } from './components/RefreshBanner'
import { StatusTabs } from './components/StatusTabs'
import {
  getJobs,
  isMockApi,
  refreshJobs,
  updateJobStatus,
} from './services/jobApi'
import {
  jobStatuses,
  type JobPosting,
  type JobSource,
  type JobStatus,
  type RefreshResponse,
} from './types/job'
import './App.css'

function App() {
  const [jobs, setJobs] = useState<JobPosting[]>([])
  const [activeStatus, setActiveStatus] = useState<JobStatus>('NEW')
  const [source, setSource] = useState<JobSource | 'ALL'>('ALL')
  const [search, setSearch] = useState('')
  const [lastRefresh, setLastRefresh] = useState<string | null>(null)
  const [refreshResult, setRefreshResult] = useState<RefreshResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [updatingIds, setUpdatingIds] = useState<number[]>([])
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadJobs = useCallback(async () => {
    const result = await getJobs()
    setJobs(result)
  }, [])

  useEffect(() => {
    let active = true

    getJobs()
      .then((result) => {
        if (active) setJobs(result)
      })
      .catch((error: unknown) => {
        if (active) {
          setErrorMessage(
            error instanceof Error ? error.message : 'Gagal memuat lowongan.',
          )
        }
      })
      .finally(() => {
        if (active) setIsLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  const counts = useMemo(
    () =>
      Object.fromEntries(
        jobStatuses.map((status) => [
          status,
          jobs.filter((job) => job.status === status).length,
        ]),
      ) as Record<JobStatus, number>,
    [jobs],
  )

  const visibleJobs = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase()

    return jobs
      .filter((job) => job.status === activeStatus)
      .filter((job) => source === 'ALL' || job.source === source)
      .filter((job) => {
        if (!normalizedSearch) return true

        return [
          job.companyName,
          job.jobTitle,
          job.location ?? '',
          ...job.qualifications,
        ]
          .join(' ')
          .toLocaleLowerCase()
          .includes(normalizedSearch)
      })
      .sort(
        (first, second) =>
          new Date(second.publishedAt).getTime() -
          new Date(first.publishedAt).getTime(),
      )
  }, [activeStatus, jobs, search, source])

  const handleRefresh = async () => {
    setIsRefreshing(true)
    setErrorMessage(null)

    try {
      const result = await refreshJobs()
      await loadJobs()
      setRefreshResult(result)
      setLastRefresh(result.checkedAt)
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal memperbarui lowongan.',
      )
    } finally {
      setIsRefreshing(false)
    }
  }

  const handleStatusChange = async (id: number, status: JobStatus) => {
    setErrorMessage(null)
    setUpdatingIds((current) => [...current, id])

    try {
      const updatedJob = await updateJobStatus(id, status)
      setJobs((current) =>
        current.map((job) => (job.id === id ? updatedJob : job)),
      )
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : 'Status lowongan gagal diperbarui.',
      )
    } finally {
      setUpdatingIds((current) => current.filter((item) => item !== id))
    }
  }

  return (
    <div className="app-shell">
      <Header
        lastRefresh={lastRefresh}
        isRefreshing={isRefreshing}
        onRefresh={handleRefresh}
      />
      <main className="main-content">
        <section className="page-intro" aria-labelledby="page-title">
          <div>
            <p className="eyebrow">RUANG PENCARIAN KERJA</p>
            <h1 id="page-title">Lowongan IT</h1>
            <p className="page-description">
              Pantau peluang baru dan atur langkah berikutnya.
            </p>
          </div>
          {isMockApi && (
            <span className="demo-label">Mode demo · data simulasi</span>
          )}
        </section>

        {refreshResult && (
          <RefreshBanner
            message={refreshResult.message}
            newJobsCount={refreshResult.newJobsCount}
          />
        )}
        {errorMessage && (
          <div className="error-banner" role="alert">
            <p>{errorMessage}</p>
            <button
              className="button button-quiet"
              type="button"
              onClick={() => setErrorMessage(null)}
            >
              Tutup
            </button>
          </div>
        )}

        <StatusTabs
          activeStatus={activeStatus}
          counts={counts}
          onStatusChange={setActiveStatus}
        />
        <JobFilters
          search={search}
          source={source}
          onSearchChange={setSearch}
          onSourceChange={setSource}
        />

        {isLoading ? (
          <section className="loading-state" role="status" aria-live="polite">
            <span className="spinner" aria-hidden="true" />
            <p>Memuat lowongan...</p>
          </section>
        ) : visibleJobs.length > 0 ? (
          <section className="job-list" aria-label="Daftar lowongan">
            {visibleJobs.map((job) => (
              <JobCard
                key={job.id}
                job={job}
                isUpdating={updatingIds.includes(job.id)}
                onStatusChange={handleStatusChange}
              />
            ))}
          </section>
        ) : (
          <EmptyState
            hasSearch={Boolean(search.trim())}
            hasSourceFilter={source !== 'ALL'}
          />
        )}
      </main>
      <footer className="app-footer">
        <p>Kerja IT</p>
        <p>Lowongan terkurasi untuk langkah karier berikutnya.</p>
      </footer>
    </div>
  )
}

export default App
