import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  createApplication,
  createCareerSite,
  createOnGoingJob,
  deleteApplication,
  deleteCareerSite,
  deleteOnGoingJob,
  getApplications,
  getCareerSites,
  getOnGoingJobs,
  moveOnGoingToApplied,
  updateApplication,
  updateCareerSite,
} from '../services/careerApi'
import { ConfirmDialog } from './ConfirmDialog'
import type {
  ApplicationResult,
  CareerSite,
  JobApplication,
  OnGoingJob,
} from '../types/career'

type CareerTab = 'applied' | 'ongoing' | 'sites'

const ITEMS_PER_PAGE = 20

const dateTimeFormatter = new Intl.DateTimeFormat('id-ID', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const resultLabels: Record<ApplicationResult, string> = {
  InProgress: 'In Progress',
  Rejected: 'Rejected',
  Accepted: 'Accepted',
}

const statusPresets = [
  'Applied',
  'Screening',
  'HR Interview',
  'Technical Test',
  'User Interview',
  'Offering',
]

function toLocalDateTimeInput(isoString?: string): string {
  const date = isoString ? new Date(isoString) : new Date()
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

function formatDateTime(isoString: string | null): string {
  if (!isoString) return '-'
  return dateTimeFormatter.format(new Date(isoString))
}

function inferOnGoingSource(link: string, notes: string | null): string {
  if (notes?.trim()) return notes.trim()
  try {
    const host = new URL(link).hostname.toLocaleLowerCase()
    if (host.includes('disnakerja.com')) return 'Disnakerja'
    if (host.includes('bukajobs.com')) return 'Bukajobs'
    if (host.includes('browseloker.')) return 'BrowseLoker'
    if (host.includes('lokerblog.com')) return 'Loker Blog'
    if (host.includes('lokerbumn.com')) return 'LokerBUMN'
    if (host.includes('bersamabumn.com')) return 'BersamaBUMN'
    if (host.includes('openkerja.id')) return 'OpenKerja'
    if (host.includes('okejobs.com')) return 'OkeJobs'
    if (host.includes('instagram.com')) return 'Instagram'
    if (host.includes('linkedin.com') || host.includes('lnkd.in')) return 'LinkedIn'
    if (host.includes('indeed.com')) return 'Indeed'
    if (host.includes('glints.com')) return 'Glints'
    if (host.includes('kalibrr.com')) return 'Kalibrr'
    if (host.includes('dealls.com')) return 'Dealls'
    if (host.includes('jobstreet.')) return 'JobStreet'
    if (host.includes('glassdoor.')) return 'Glassdoor'
    if (host.includes('pintarnya.com')) return 'Pintarnya'
    if (host.includes('cake.me') || host.includes('cakeresume.com')) return 'Cake'
    if (host.includes('techinasia.com')) return 'Tech in Asia'

    const parts = host.split('.').filter(Boolean)
    const prefixes = new Set([
      'www',
      'karir',
      'career',
      'careers',
      'jobs',
      'job',
      'rekrutmen',
      'recruitment',
      'join',
      'm',
    ])
    while (parts.length > 2 && prefixes.has(parts[0])) {
      parts.shift()
    }
    const brand = parts[0]
    if (brand) {
      return brand.length <= 3
        ? brand.toUpperCase()
        : brand[0].toUpperCase() + brand.slice(1)
    }
  } catch {
    // ignore invalid URL
  }
  return 'Bookmark Manual'
}

export function CareerTracker() {
  const [activeTab, setActiveTab] = useState<CareerTab>('applied')
  const [applications, setApplications] = useState<JobApplication[]>([])
  const [onGoingJobs, setOnGoingJobs] = useState<OnGoingJob[]>([])
  const [careerSites, setCareerSites] = useState<CareerSite[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [noticeMessage, setNoticeMessage] = useState<string | null>(null)
  const [pendingDelete, setPendingDelete] = useState<{
    type: 'application' | 'ongoing' | 'site'
    id: number
    label: string
  } | null>(null)

  const [appliedSearch, setAppliedSearch] = useState('')
  const [resultFilter, setResultFilter] = useState<ApplicationResult | 'ALL'>('ALL')
  const [appliedPage, setAppliedPage] = useState(1)
  const [appliedPageInput, setAppliedPageInput] = useState<string | null>(null)
  const [showAddApplied, setShowAddApplied] = useState(false)

  const [newCompany, setNewCompany] = useState('')
  const [newJobTitle, setNewJobTitle] = useState('')
  const [newAppliedVia, setNewAppliedVia] = useState('Career Site')
  const [newAppliedLink, setNewAppliedLink] = useState('')
  const [newAppliedDate, setNewAppliedDate] = useState(() => toLocalDateTimeInput())
  const [newStatus, setNewStatus] = useState('Applied')
  const [newResult, setNewResult] = useState<ApplicationResult>('InProgress')
  const [isSubmittingApplied, setIsSubmittingApplied] = useState(false)

  const [editingStatusId, setEditingStatusId] = useState<number | null>(null)
  const [editingStatusDraft, setEditingStatusDraft] = useState('')
  const [busyIds, setBusyIds] = useState<number[]>([])

  const [onGoingSearch, setOnGoingSearch] = useState('')
  const [showAddOnGoing, setShowAddOnGoing] = useState(false)
  const [ogCompany, setOgCompany] = useState('')
  const [ogJobTitle, setOgJobTitle] = useState('')
  const [ogLink, setOgLink] = useState('')
  const [ogNotes, setOgNotes] = useState('')
  const [isSubmittingOnGoing, setIsSubmittingOnGoing] = useState(false)

  const [siteSearch, setSiteSearch] = useState('')
  const [showAddSite, setShowAddSite] = useState(false)
  const [siteName, setSiteName] = useState('')
  const [siteUrl, setSiteUrl] = useState('')
  const [siteLastJob, setSiteLastJob] = useState('')
  const [siteDrafts, setSiteDrafts] = useState<Record<number, string>>({})
  const [isSubmittingSite, setIsSubmittingSite] = useState(false)

  useEffect(() => {
    let active = true
    Promise.all([getApplications(), getOnGoingJobs(), getCareerSites()])
      .then(([apps, ongoing, sites]) => {
        if (!active) return
        setApplications(apps)
        setOnGoingJobs(ongoing)
        setCareerSites(sites)
        setSiteDrafts(
          Object.fromEntries(sites.map((s) => [s.id, s.lastJobUpdated ?? ''])),
        )
      })
      .catch((error: unknown) => {
        if (active) {
          setErrorMessage(
            error instanceof Error ? error.message : 'Gagal memuat Career Tracker.',
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

  const filteredApplications = useMemo(() => {
    const query = appliedSearch.trim().toLocaleLowerCase()
    return applications
      .filter((item) => resultFilter === 'ALL' || item.result === resultFilter)
      .filter((item) => {
        if (!query) return true
        return [item.company, item.jobTitle, item.appliedVia, item.status]
          .join(' ')
          .toLocaleLowerCase()
          .includes(query)
      })
  }, [applications, appliedSearch, resultFilter])

  const appliedPageCount = Math.max(
    1,
    Math.ceil(filteredApplications.length / ITEMS_PER_PAGE),
  )
  const safeAppliedPage = Math.min(appliedPage, appliedPageCount)
  const pagedApplications = useMemo(
    () =>
      filteredApplications.slice(
        (safeAppliedPage - 1) * ITEMS_PER_PAGE,
        safeAppliedPage * ITEMS_PER_PAGE,
      ),
    [filteredApplications, safeAppliedPage],
  )

  const filteredOnGoing = useMemo(() => {
    const query = onGoingSearch.trim().toLocaleLowerCase()
    if (!query) return onGoingJobs
    return onGoingJobs.filter((item) =>
      [item.company, item.jobTitle, item.notes ?? '']
        .join(' ')
        .toLocaleLowerCase()
        .includes(query),
    )
  }, [onGoingJobs, onGoingSearch])

  const filteredSites = useMemo(() => {
    const query = siteSearch.trim().toLocaleLowerCase()
    if (!query) return careerSites
    return careerSites.filter((item) =>
      [item.name, item.lastJobUpdated ?? '']
        .join(' ')
        .toLocaleLowerCase()
        .includes(query),
    )
  }, [careerSites, siteSearch])

  const changeAppliedPage = (page: number) => {
    setAppliedPage(page)
    setAppliedPageInput(null)
  }

  const handleAppliedPageJump = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const requestedPage = Number(appliedPageInput ?? safeAppliedPage)
    if (
      !Number.isInteger(requestedPage) ||
      requestedPage < 1 ||
      requestedPage > appliedPageCount
    ) {
      setAppliedPageInput(null)
      return
    }
    changeAppliedPage(requestedPage)
  }

  const markBusy = (id: number) => setBusyIds((prev) => [...prev, id])
  const unmarkBusy = (id: number) =>
    setBusyIds((prev) => prev.filter((item) => item !== id))

  const handleAddApplication = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmittingApplied(true)
    setErrorMessage(null)

    try {
      const isoDate = new Date(newAppliedDate).toISOString()
      const created = await createApplication({
        company: newCompany,
        jobTitle: newJobTitle,
        appliedVia: newAppliedVia,
        appliedLink: newAppliedLink.trim() || null,
        appliedDate: isoDate,
        lastUpdated: isoDate,
        status: newStatus,
        result: newResult,
      })
      setApplications((prev) => [created, ...prev])
      setNewCompany('')
      setNewJobTitle('')
      setNewAppliedLink('')
      setNewAppliedDate(toLocalDateTimeInput())
      setNewStatus('Applied')
      setNewResult('InProgress')
      setShowAddApplied(false)
      setAppliedPage(1)
      setNoticeMessage('Lamaran baru berhasil ditambahkan.')
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal menambah lamaran.',
      )
    } finally {
      setIsSubmittingApplied(false)
    }
  }

  const handleSaveStatus = async (id: number) => {
    const trimmed = editingStatusDraft.trim()
    if (!trimmed) return
    markBusy(id)
    setErrorMessage(null)
    try {
      const updated = await updateApplication(id, { status: trimmed })
      setApplications((prev) =>
        prev.map((item) => (item.id === id ? updated : item)),
      )
      setEditingStatusId(null)
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal memperbarui status.',
      )
    } finally {
      unmarkBusy(id)
    }
  }

  const handleResultChange = async (id: number, result: ApplicationResult) => {
    markBusy(id)
    setErrorMessage(null)
    try {
      const updated = await updateApplication(id, { result })
      setApplications((prev) =>
        prev.map((item) => (item.id === id ? updated : item)),
      )
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal memperbarui result.',
      )
    } finally {
      unmarkBusy(id)
    }
  }

  const handleDeleteApplication = (item: JobApplication) => {
    setPendingDelete({
      type: 'application',
      id: item.id,
      label: `${item.jobTitle} di ${item.company}`,
    })
  }

  const handleAddOnGoing = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmittingOnGoing(true)
    setErrorMessage(null)
    try {
      const created = await createOnGoingJob({
        company: ogCompany,
        jobTitle: ogJobTitle,
        link: ogLink,
        notes: ogNotes.trim() || null,
      })
      setOnGoingJobs((prev) => [created, ...prev])
      setOgCompany('')
      setOgJobTitle('')
      setOgLink('')
      setOgNotes('')
      setShowAddOnGoing(false)
      setNoticeMessage('Lowongan ditambahkan ke daftar On Going.')
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal menambah On Going.',
      )
    } finally {
      setIsSubmittingOnGoing(false)
    }
  }

  const handleMoveOnGoingToApplied = async (id: number) => {
    markBusy(id)
    setErrorMessage(null)
    try {
      const applied = await moveOnGoingToApplied(id)
      setOnGoingJobs((prev) => prev.filter((item) => item.id !== id))
      setApplications((prev) => [applied, ...prev])
      setNoticeMessage(
        `"${applied.jobTitle}" di ${applied.company} dipindahkan ke Job Applied.`,
      )
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal memindahkan ke Job Applied.',
      )
    } finally {
      unmarkBusy(id)
    }
  }

  const handleDeleteOnGoing = (item: OnGoingJob) => {
    setPendingDelete({
      type: 'ongoing',
      id: item.id,
      label: `${item.jobTitle} di ${item.company}`,
    })
  }

  const handleAddSite = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmittingSite(true)
    setErrorMessage(null)
    try {
      const created = await createCareerSite({
        name: siteName,
        url: siteUrl,
        lastJobUpdated: siteLastJob.trim() || null,
      })
      setCareerSites((prev) => [...prev, created])
      setSiteDrafts((prev) => ({
        ...prev,
        [created.id]: created.lastJobUpdated ?? '',
      }))
      setSiteName('')
      setSiteUrl('')
      setSiteLastJob('')
      setShowAddSite(false)
      setNoticeMessage('Career Site berhasil ditambahkan.')
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal menambah Career Site.',
      )
    } finally {
      setIsSubmittingSite(false)
    }
  }

  const handleSaveSiteProgress = async (id: number) => {
    markBusy(id)
    setErrorMessage(null)
    try {
      const updated = await updateCareerSite(id, {
        lastJobUpdated: siteDrafts[id] ?? '',
      })
      setCareerSites((prev) =>
        prev.map((item) => (item.id === id ? updated : item)),
      )
      setNoticeMessage(`Catatan terakhir untuk "${updated.name}" disimpan.`)
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal menyimpan catatan situs.',
      )
    } finally {
      unmarkBusy(id)
    }
  }

  const handleDeleteSite = (site: CareerSite) => {
    setPendingDelete({
      type: 'site',
      id: site.id,
      label: site.name,
    })
  }

  const executePendingDelete = async () => {
    if (!pendingDelete) return
    const target = pendingDelete
    setPendingDelete(null)
    markBusy(target.id)
    setErrorMessage(null)
    try {
      if (target.type === 'application') {
        await deleteApplication(target.id)
        setApplications((prev) => prev.filter((item) => item.id !== target.id))
      } else if (target.type === 'ongoing') {
        await deleteOnGoingJob(target.id)
        setOnGoingJobs((prev) => prev.filter((item) => item.id !== target.id))
      } else {
        await deleteCareerSite(target.id)
        setCareerSites((prev) => prev.filter((item) => item.id !== target.id))
      }
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : 'Gagal menghapus data.',
      )
    } finally {
      unmarkBusy(target.id)
    }
  }

  return (
    <section aria-labelledby="career-title">
      <section className="page-intro">
        <div>
          <p className="eyebrow">PELACAKAN KARIER PRIBADI</p>
          <h1 id="career-title">Career Tracker</h1>
          <p className="page-description">
            Kelola riwayat lamaran, bookmark manual, dan daftar portal karier yang dipantau.
          </p>
        </div>
      </section>

      {noticeMessage && (
        <div className="refresh-banner has-new-jobs" role="status">
          <span className="refresh-indicator" aria-hidden="true" />
          <p>{noticeMessage}</p>
          <button
            className="button button-quiet"
            type="button"
            onClick={() => setNoticeMessage(null)}
          >
            Tutup
          </button>
        </div>
      )}

      <nav className="status-tabs" aria-label="Tab Career Tracker">
        <button
          type="button"
          className={`status-tab${activeTab === 'applied' ? ' is-active' : ''}`}
          aria-pressed={activeTab === 'applied'}
          onClick={() => setActiveTab('applied')}
        >
          <span>Job Applied</span>
          <span className="status-count">{applications.length}</span>
        </button>
        <button
          type="button"
          className={`status-tab${activeTab === 'ongoing' ? ' is-active' : ''}`}
          aria-pressed={activeTab === 'ongoing'}
          onClick={() => setActiveTab('ongoing')}
        >
          <span>On Going</span>
          <span className="status-count">{onGoingJobs.length}</span>
        </button>
        <button
          type="button"
          className={`status-tab${activeTab === 'sites' ? ' is-active' : ''}`}
          aria-pressed={activeTab === 'sites'}
          onClick={() => setActiveTab('sites')}
        >
          <span>Career Site</span>
          <span className="status-count">{careerSites.length}</span>
        </button>
      </nav>

      {isLoading ? (
        <section className="loading-state" role="status" aria-live="polite">
          <span className="spinner" aria-hidden="true" />
          <p>Memuat data Career Tracker...</p>
        </section>
      ) : activeTab === 'applied' ? (
        <div className="career-section">
          <div className="career-toolbar">
            <label className="search-field">
              <span className="field-label">Cari lamaran</span>
              <input
                type="search"
                value={appliedSearch}
                placeholder="Perusahaan, posisi, via, atau status"
                onChange={(e) => {
                  setAppliedSearch(e.target.value)
                  setAppliedPage(1)
                }}
              />
            </label>
            <label className="source-field">
              <span className="field-label">Result</span>
              <select
                value={resultFilter}
                onChange={(e) => {
                  setResultFilter(e.target.value as ApplicationResult | 'ALL')
                  setAppliedPage(1)
                }}
              >
                <option value="ALL">Semua Result</option>
                <option value="InProgress">In Progress</option>
                <option value="Accepted">Accepted</option>
                <option value="Rejected">Rejected</option>
              </select>
            </label>
            <div className="career-toolbar-action">
              <button
                type="button"
                className="button button-primary"
                onClick={() => {
                  setNewAppliedDate(toLocalDateTimeInput())
                  setShowAddApplied((prev) => !prev)
                }}
              >
                {showAddApplied ? 'Tutup Form' : '+ Tambah Lamaran'}
              </button>
            </div>
          </div>

          {showAddApplied && (
            <form className="career-form-card" onSubmit={handleAddApplication}>
              <h2>Tambah Lamaran Manual</h2>
              <div className="career-form-grid">
                <label className="profile-field">
                  <span className="field-label">Company</span>
                  <input
                    type="text"
                    required
                    value={newCompany}
                    onChange={(e) => setNewCompany(e.target.value)}
                    placeholder="Contoh: PT Bank Central Asia Tbk"
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Job Title</span>
                  <input
                    type="text"
                    required
                    value={newJobTitle}
                    onChange={(e) => setNewJobTitle(e.target.value)}
                    placeholder="Contoh: Full Stack Engineer"
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Applied Via</span>
                  <input
                    type="text"
                    list="applied-via-presets"
                    required
                    value={newAppliedVia}
                    onChange={(e) => setNewAppliedVia(e.target.value)}
                  />
                  <datalist id="applied-via-presets">
                    <option value="Career Site" />
                    <option value="LinkedIn" />
                    <option value="Indeed" />
                    <option value="Glassdoor" />
                    <option value="Glints" />
                    <option value="Kalibrr" />
                    <option value="JobStreet" />
                  </datalist>
                </label>
                <label className="profile-field">
                  <span className="field-label">Applied Link</span>
                  <input
                    type="url"
                    value={newAppliedLink}
                    onChange={(e) => setNewAppliedLink(e.target.value)}
                    placeholder="https://..."
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Applied Date (Tanggal & Jam)</span>
                  <input
                    type="datetime-local"
                    required
                    value={newAppliedDate}
                    onChange={(e) => setNewAppliedDate(e.target.value)}
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Status</span>
                  <input
                    type="text"
                    list="status-presets"
                    required
                    value={newStatus}
                    onChange={(e) => setNewStatus(e.target.value)}
                  />
                  <datalist id="status-presets">
                    {statusPresets.map((preset) => (
                      <option value={preset} key={preset} />
                    ))}
                  </datalist>
                </label>
                <label className="profile-field">
                  <span className="field-label">Result</span>
                  <select
                    value={newResult}
                    onChange={(e) =>
                      setNewResult(e.target.value as ApplicationResult)
                    }
                  >
                    <option value="InProgress">In Progress</option>
                    <option value="Rejected">Rejected</option>
                    <option value="Accepted">Accepted</option>
                  </select>
                </label>
              </div>
              <div className="career-form-actions">
                <button
                  type="button"
                  className="button button-secondary"
                  onClick={() => setShowAddApplied(false)}
                >
                  Batal
                </button>
                <button
                  type="submit"
                  className="button button-primary"
                  disabled={isSubmittingApplied}
                >
                  {isSubmittingApplied ? 'Menyimpan...' : 'Simpan Lamaran'}
                </button>
              </div>
            </form>
          )}

          <div className="career-list">
            {pagedApplications.map((item) => {
              const isBusy = busyIds.includes(item.id)
              const isEditingStatus = editingStatusId === item.id

              return (
                <article className="career-card" key={item.id}>
                  <div className="career-card-top">
                    <div className="career-card-badges">
                      <span className="source-label">{item.appliedVia}</span>
                      <span
                        className={`result-badge result-${item.result.toLowerCase()}`}
                      >
                        {resultLabels[item.result]}
                      </span>
                    </div>
                    <div className="career-dates">
                      <span>Dilamar: {formatDateTime(item.appliedDate)}</span>
                      <span>Diperbarui: {formatDateTime(item.lastUpdated)}</span>
                    </div>
                  </div>

                  <div className="career-card-main">
                    <div>
                      <h2>{item.jobTitle}</h2>
                      <p className="company-name">{item.company}</p>
                    </div>
                    {item.appliedLink && (
                      <a
                        className="button button-secondary"
                        href={item.appliedLink}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        Buka Link
                      </a>
                    )}
                  </div>

                  <div className="career-card-controls">
                    <div className="career-status-control">
                      <span className="taxonomy-label">Status:</span>
                      {isEditingStatus ? (
                        <div className="inline-edit-group">
                          <input
                            type="text"
                            list="status-presets"
                            value={editingStatusDraft}
                            onChange={(e) => setEditingStatusDraft(e.target.value)}
                            disabled={isBusy}
                          />
                          <button
                            type="button"
                            className="button button-primary"
                            disabled={isBusy || !editingStatusDraft.trim()}
                            onClick={() => handleSaveStatus(item.id)}
                          >
                            Simpan
                          </button>
                          <button
                            type="button"
                            className="button button-quiet"
                            disabled={isBusy}
                            onClick={() => setEditingStatusId(null)}
                          >
                            Batal
                          </button>
                        </div>
                      ) : (
                        <button
                          type="button"
                          className="status-pill-button"
                          title="Klik untuk ubah status"
                          onClick={() => {
                            setEditingStatusId(item.id)
                            setEditingStatusDraft(item.status)
                          }}
                        >
                          {item.status} ✎
                        </button>
                      )}
                    </div>

                    <div className="career-right-controls">
                      <label className="result-select-label">
                        <span className="taxonomy-label">Result:</span>
                        <select
                          value={item.result}
                          disabled={isBusy}
                          onChange={(e) =>
                            handleResultChange(
                              item.id,
                              e.target.value as ApplicationResult,
                            )
                          }
                        >
                          <option value="InProgress">In Progress</option>
                          <option value="Rejected">Rejected</option>
                          <option value="Accepted">Accepted</option>
                        </select>
                      </label>
                      <button
                        type="button"
                        className="button button-quiet"
                        disabled={isBusy}
                        onClick={() => handleDeleteApplication(item)}
                      >
                        Hapus
                      </button>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>

          {appliedPageCount > 1 && (
            <nav className="pagination" aria-label="Paginasi Job Applied">
              <div className="pagination-main">
                <div className="pagination-navigation">
                  <button
                    type="button"
                    className="button button-secondary pagination-edge"
                    disabled={safeAppliedPage === 1}
                    onClick={() => changeAppliedPage(1)}
                  >
                    Awal
                  </button>
                  <button
                    type="button"
                    className="button button-secondary pagination-edge"
                    disabled={safeAppliedPage === 1}
                    onClick={() => changeAppliedPage(safeAppliedPage - 1)}
                  >
                    Sebelumnya
                  </button>
                  <button
                    type="button"
                    className="button button-secondary pagination-edge"
                    disabled={safeAppliedPage === appliedPageCount}
                    onClick={() => changeAppliedPage(safeAppliedPage + 1)}
                  >
                    Berikutnya
                  </button>
                  <button
                    type="button"
                    className="button button-secondary pagination-edge"
                    disabled={safeAppliedPage === appliedPageCount}
                    onClick={() => changeAppliedPage(appliedPageCount)}
                  >
                    Akhir
                  </button>
                </div>
                <span className="pagination-summary">
                  Halaman {safeAppliedPage} dari {appliedPageCount} ({filteredApplications.length} lamaran)
                </span>
              </div>
              <form className="pagination-jump" onSubmit={handleAppliedPageJump}>
                <label>
                  <span className="field-label">Ke halaman</span>
                  <input
                    type="number"
                    min="1"
                    max={appliedPageCount}
                    step="1"
                    required
                    value={appliedPageInput ?? String(safeAppliedPage)}
                    onChange={(event) => setAppliedPageInput(event.target.value)}
                  />
                </label>
                <button className="button button-secondary" type="submit">
                  Buka
                </button>
              </form>
            </nav>
          )}
        </div>
      ) : activeTab === 'ongoing' ? (
        <div className="career-section">
          <div className="career-toolbar">
            <label className="search-field">
              <span className="field-label">Cari bookmark On Going</span>
              <input
                type="search"
                value={onGoingSearch}
                placeholder="Perusahaan, posisi, atau catatan"
                onChange={(e) => setOnGoingSearch(e.target.value)}
              />
            </label>
            <div className="career-toolbar-action">
              <button
                type="button"
                className="button button-primary"
                onClick={() => setShowAddOnGoing((prev) => !prev)}
              >
                {showAddOnGoing ? 'Tutup Form' : '+ Tambah On Going'}
              </button>
            </div>
          </div>

          {showAddOnGoing && (
            <form className="career-form-card" onSubmit={handleAddOnGoing}>
              <h2>Tambah Bookmark On Going</h2>
              <div className="career-form-grid">
                <label className="profile-field">
                  <span className="field-label">Company</span>
                  <input
                    type="text"
                    required
                    value={ogCompany}
                    onChange={(e) => setOgCompany(e.target.value)}
                    placeholder="Contoh: PT Mayora Indah Tbk"
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Job Title</span>
                  <input
                    type="text"
                    required
                    value={ogJobTitle}
                    onChange={(e) => setOgJobTitle(e.target.value)}
                    placeholder="Contoh: Fullstack Developer"
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Link</span>
                  <input
                    type="url"
                    required
                    value={ogLink}
                    onChange={(e) => setOgLink(e.target.value)}
                    placeholder="https://www.instagram.com/p/..."
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Catatan / Sumber (Opsional)</span>
                  <input
                    type="text"
                    value={ogNotes}
                    onChange={(e) => setOgNotes(e.target.value)}
                    placeholder="Contoh: Instagram / LinkedIn Post"
                  />
                </label>
              </div>
              <div className="career-form-actions">
                <button
                  type="button"
                  className="button button-secondary"
                  onClick={() => setShowAddOnGoing(false)}
                >
                  Batal
                </button>
                <button
                  type="submit"
                  className="button button-primary"
                  disabled={isSubmittingOnGoing}
                >
                  {isSubmittingOnGoing ? 'Menyimpan...' : 'Simpan Bookmark'}
                </button>
              </div>
            </form>
          )}

          <div className="career-list">
            {filteredOnGoing.map((item) => {
              const isBusy = busyIds.includes(item.id)
              return (
                <article className="career-card" key={item.id}>
                  <div className="career-card-top">
                    <span className="source-label">
                      {inferOnGoingSource(item.link, item.notes)}
                    </span>
                    <span className="career-dates">
                      Disimpan: {formatDateTime(item.createdAt)}
                    </span>
                  </div>
                  <div className="career-card-main">
                    <div>
                      <h2>{item.jobTitle}</h2>
                      <p className="company-name">{item.company}</p>
                    </div>
                    <div className="career-right-controls">
                      <a
                        className="button button-secondary"
                        href={item.link}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        Buka Link
                      </a>
                      <button
                        type="button"
                        className="button button-primary"
                        disabled={isBusy}
                        onClick={() => handleMoveOnGoingToApplied(item.id)}
                      >
                        Tandai Dilamar
                      </button>
                      <button
                        type="button"
                        className="button button-quiet"
                        disabled={isBusy}
                        onClick={() => handleDeleteOnGoing(item)}
                      >
                        Hapus
                      </button>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        </div>
      ) : (
        <div className="career-section">
          <div className="career-toolbar">
            <label className="search-field">
              <span className="field-label">Cari Career Site</span>
              <input
                type="search"
                value={siteSearch}
                placeholder="Nama portal, perusahaan, atau catatan terakhir"
                onChange={(e) => setSiteSearch(e.target.value)}
              />
            </label>
            <div className="career-toolbar-action">
              <button
                type="button"
                className="button button-primary"
                onClick={() => setShowAddSite((prev) => !prev)}
              >
                {showAddSite ? 'Tutup Form' : '+ Tambah Career Site'}
              </button>
            </div>
          </div>

          {showAddSite && (
            <form className="career-form-card" onSubmit={handleAddSite}>
              <h2>Tambah Career Site Pantauan</h2>
              <div className="career-form-grid">
                <label className="profile-field">
                  <span className="field-label">Nama / Portal</span>
                  <input
                    type="text"
                    required
                    value={siteName}
                    onChange={(e) => setSiteName(e.target.value)}
                    placeholder="Contoh: Glints - Full Stack Developer"
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">URL Career Site</span>
                  <input
                    type="url"
                    required
                    value={siteUrl}
                    onChange={(e) => setSiteUrl(e.target.value)}
                    placeholder="https://..."
                  />
                </label>
                <label className="profile-field">
                  <span className="field-label">Last Job Updated (Opsional)</span>
                  <input
                    type="text"
                    value={siteLastJob}
                    onChange={(e) => setSiteLastJob(e.target.value)}
                    placeholder="Lowongan terakhir yang dilihat"
                  />
                </label>
              </div>
              <div className="career-form-actions">
                <button
                  type="button"
                  className="button button-secondary"
                  onClick={() => setShowAddSite(false)}
                >
                  Batal
                </button>
                <button
                  type="submit"
                  className="button button-primary"
                  disabled={isSubmittingSite}
                >
                  {isSubmittingSite ? 'Menyimpan...' : 'Simpan Career Site'}
                </button>
              </div>
            </form>
          )}

          <div className="career-list">
            {filteredSites.map((site) => {
              const isBusy = busyIds.includes(site.id)
              const draft = siteDrafts[site.id] ?? ''
              const hasChanged = draft !== (site.lastJobUpdated ?? '')

              return (
                <article className="career-card" key={site.id}>
                  <div className="career-card-main">
                    <div>
                      <h2>{site.name}</h2>
                      {site.lastCheckedAt && (
                        <p className="company-name">
                          Terakhir dicek: {formatDateTime(site.lastCheckedAt)}
                        </p>
                      )}
                    </div>
                    <div className="career-right-controls">
                      <a
                        className="button button-secondary"
                        href={site.url}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        Buka Situs
                      </a>
                      <button
                        type="button"
                        className="button button-quiet"
                        disabled={isBusy}
                        onClick={() => handleDeleteSite(site)}
                      >
                        Hapus
                      </button>
                    </div>
                  </div>
                  <div className="site-progress-row">
                    <label className="site-progress-input">
                      <span className="taxonomy-label">Last Job Updated</span>
                      <input
                        type="text"
                        value={draft}
                        disabled={isBusy}
                        placeholder="Catat lowongan terakhir yang dilihat di situs ini"
                        onChange={(e) =>
                          setSiteDrafts((prev) => ({
                            ...prev,
                            [site.id]: e.target.value,
                          }))
                        }
                      />
                    </label>
                    <button
                      type="button"
                      className="button button-primary"
                      disabled={isBusy || !hasChanged}
                      onClick={() => handleSaveSiteProgress(site.id)}
                    >
                      Simpan
                    </button>
                  </div>
                </article>
              )
            })}
          </div>
        </div>
      )}

      {pendingDelete && (
        <ConfirmDialog
          title="Konfirmasi Hapus"
          message={`Yakin ingin menghapus "${pendingDelete.label}"? Tindakan ini tidak dapat dibatalkan.`}
          confirmLabel="Ya, Hapus"
          tone="danger"
          onConfirm={executePendingDelete}
          onCancel={() => setPendingDelete(null)}
        />
      )}

      {errorMessage && (
        <ConfirmDialog
          title="Terjadi Kesalahan"
          message={errorMessage}
          isAlertOnly
          onConfirm={() => setErrorMessage(null)}
          onCancel={() => setErrorMessage(null)}
        />
      )}
    </section>
  )
}
