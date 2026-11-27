import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from 'react'
import { EmptyState } from './components/EmptyState'
import { Header } from './components/Header'
import { JobCard } from './components/JobCard'
import { JobFilters } from './components/JobFilters'
import { RefreshBanner } from './components/RefreshBanner'
import { StatusTabs } from './components/StatusTabs'
import {
  getJobs,
  getRefreshState,
  isMockApi,
  refreshJobs,
  retryJobExtraction,
  updateJobStatus,
} from './services/jobApi'
import {
  jobStatuses,
  type JobPosting,
  type JobSortOrder,
  type JobSource,
  type JobStatus,
  type RefreshResponse,
} from './types/job'
import './App.css'

const JOBS_PER_PAGE = 20

type PaginationItem = number | 'ellipsis'

function getPaginationItems(
  currentPage: number,
  pageCount: number,
): PaginationItem[] {
  if (pageCount <= 7) {
    return Array.from({ length: pageCount }, (_, index) => index + 1)
  }

  const firstMiddlePage = Math.max(2, currentPage - 1)
  const lastMiddlePage = Math.min(pageCount - 1, currentPage + 1)
  const pages: PaginationItem[] = [1]

  if (firstMiddlePage > 2) {
    pages.push('ellipsis')
  }

  for (let page = firstMiddlePage; page <= lastMiddlePage; page += 1) {
    pages.push(page)
  }

  if (lastMiddlePage < pageCount - 1) {
    pages.push('ellipsis')
  }

  pages.push(pageCount)
  return pages
}

function App() {
  const [jobs, setJobs] = useState<JobPosting[]>([])
  const [activeStatus, setActiveStatus] = useState<JobStatus>('NEW')
  const [source, setSource] = useState<JobSource | 'ALL'>('ALL')
  const [sortOrder, setSortOrder] = useState<JobSortOrder>('newest')
  const [search, setSearch] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [pageInput, setPageInput] = useState<string | null>(null)
  const [lastRefresh, setLastRefresh] = useState<string | null>(null)
  const [refreshResult, setRefreshResult] = useState<RefreshResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [updatingIds, setUpdatingIds] = useState<number[]>([])
  const [retryingExtractionIds, setRetryingExtractionIds] = useState<number[]>([])
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const loadJobs = useCallback(async () => {
    const result = await getJobs()
    setJobs(result)
  }, [])

  useEffect(() => {
    let active = true

    Promise.all([getJobs(), getRefreshState()])
      .then(([result, refreshState]) => {
        if (active) {
          setJobs(result)
          setLastRefresh(refreshState.lastSuccessfulRefreshAt)
        }
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
      .sort((first, second) => {
        const dateDifference =
          new Date(first.publishedAt).getTime() -
          new Date(second.publishedAt).getTime()

        if (dateDifference === 0) return first.id - second.id
        return sortOrder === 'oldest' ? dateDifference : -dateDifference
      })
  }, [activeStatus, jobs, search, sortOrder, source])

  const pageCount = Math.max(1, Math.ceil(visibleJobs.length / JOBS_PER_PAGE))
  const displayedPage = Math.min(currentPage, pageCount)
  const paginationItems = getPaginationItems(displayedPage, pageCount)
  const pageJobs = useMemo(
    () =>
      visibleJobs.slice(
        (displayedPage - 1) * JOBS_PER_PAGE,
        displayedPage * JOBS_PER_PAGE,
      ),
    [displayedPage, visibleJobs],
  )

  const changePage = (page: number) => {
    setCurrentPage(page)
    setPageInput(null)
  }

  const handleRefresh = async () => {
    setIsRefreshing(true)
    setErrorMessage(null)

    try {
      const result = await refreshJobs()
      await loadJobs()
      changePage(1)
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
      setPageInput(null)
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

  const handleRetryExtraction = async (id: number) => {
    setErrorMessage(null)
    setRetryingExtractionIds((current) => [...current, id])

    try {
      const updatedJob = await retryJobExtraction(id)
      setJobs((current) =>
        current.map((job) => (job.id === id ? updatedJob : job)),
      )
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : 'Ekstraksi ulang lowongan gagal.',
      )
    } finally {
      setRetryingExtractionIds((current) =>
        current.filter((item) => item !== id),
      )
    }
  }

  const handlePageJump = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const requestedPage = Number(pageInput ?? displayedPage)

    if (
      !Number.isInteger(requestedPage) ||
      requestedPage < 1 ||
      requestedPage > pageCount
    ) {
      setPageInput(null)
      return
    }

    changePage(requestedPage)
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
          onStatusChange={(status) => {
            setActiveStatus(status)
            changePage(1)
          }}
        />
        <JobFilters
          search={search}
          source={source}
          sortOrder={sortOrder}
          onSearchChange={(value) => {
            setSearch(value)
            changePage(1)
          }}
          onSourceChange={(value) => {
            setSource(value)
            changePage(1)
          }}
          onSortOrderChange={(value) => {
            setSortOrder(value)
            changePage(1)
          }}
        />

        {isLoading ? (
          <section className="loading-state" role="status" aria-live="polite">
            <span className="spinner" aria-hidden="true" />
            <p>Memuat lowongan...</p>
          </section>
        ) : visibleJobs.length > 0 ? (
          <>
            <section className="job-list" aria-label="Daftar lowongan">
              {pageJobs.map((job) => (
                <JobCard
                  key={job.id}
                  job={job}
                  isUpdating={updatingIds.includes(job.id)}
                  isRetryingExtraction={retryingExtractionIds.includes(job.id)}
                  onRetryExtraction={handleRetryExtraction}
                  onStatusChange={handleStatusChange}
                />
              ))}
            </section>
            {pageCount > 1 && (
              <nav
                className="pagination"
                aria-label="Navigasi halaman lowongan"
              >
                <div className="pagination-main">
                  <div className="pagination-navigation">
                    <button
                      className="button button-secondary pagination-edge"
                      type="button"
                      disabled={displayedPage === 1}
                      onClick={() => changePage(1)}
                    >
                      Awal
                    </button>
                    <button
                      className="button button-secondary pagination-edge"
                      type="button"
                      disabled={displayedPage === 1}
                      onClick={() => changePage(displayedPage - 1)}
                    >
                      Sebelumnya
                    </button>
                    {paginationItems.map((item, index) =>
                      item === 'ellipsis' ? (
                        <span
                          className="pagination-ellipsis"
                          aria-hidden="true"
                          key={`ellipsis-${index}`}
                        >
                          ...
                        </span>
                      ) : (
                        <button
                          className={`button button-secondary pagination-page${
                            item === displayedPage ? ' is-current' : ''
                          }`}
                          type="button"
                          aria-label={`Halaman ${item}`}
                          aria-current={
                            item === displayedPage ? 'page' : undefined
                          }
                          key={item}
                          onClick={() => changePage(item)}
                        >
                          {item}
                        </button>
                      ),
                    )}
                    <button
                      className="button button-secondary pagination-edge"
                      type="button"
                      disabled={displayedPage === pageCount}
                      onClick={() => changePage(displayedPage + 1)}
                    >
                      Berikutnya
                    </button>
                    <button
                      className="button button-secondary pagination-edge"
                      type="button"
                      disabled={displayedPage === pageCount}
                      onClick={() => changePage(pageCount)}
                    >
                      Akhir
                    </button>
                  </div>
                  <span className="pagination-summary" aria-live="polite">
                    Halaman {displayedPage} dari {pageCount}
                  </span>
                </div>
                <form className="pagination-jump" onSubmit={handlePageJump}>
                  <label>
                    <span className="field-label">Ke halaman</span>
                    <input
                      type="number"
                      min="1"
                      max={pageCount}
                      step="1"
                      required
                      value={pageInput ?? String(displayedPage)}
                      onChange={(event) => setPageInput(event.target.value)}
                    />
                  </label>
                  <button className="button button-secondary" type="submit">
                    Buka
                  </button>
                </form>
              </nav>
            )}
          </>
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
