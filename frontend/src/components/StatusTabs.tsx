import type { JobStatus } from '../types/job'

interface StatusTabsProps {
  activeStatus: JobStatus
  counts: Record<JobStatus, number>
  onStatusChange: (status: JobStatus) => void
}

const statusLabels: Record<JobStatus, string> = {
  NEW: 'Baru',
  VIEWED: 'Dilihat',
  SAVED: 'Disimpan',
  APPLIED: 'Dilamar',
  IGNORED: 'Diabaikan',
}

const statuses: JobStatus[] = [
  'NEW',
  'VIEWED',
  'SAVED',
  'APPLIED',
  'IGNORED',
]

export function StatusTabs({
  activeStatus,
  counts,
  onStatusChange,
}: StatusTabsProps) {
  return (
    <nav className="status-tabs" aria-label="Filter status lowongan">
      {statuses.map((status) => (
        <button
          className={`status-tab${activeStatus === status ? ' is-active' : ''}`}
          type="button"
          key={status}
          aria-pressed={activeStatus === status}
          onClick={() => onStatusChange(status)}
        >
          <span>{statusLabels[status]}</span>
          <span className="status-count">{counts[status]}</span>
        </button>
      ))}
    </nav>
  )
}
