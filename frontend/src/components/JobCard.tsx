import type { JobPosting, JobStatus } from '../types/job'

interface JobCardProps {
  job: JobPosting
  isUpdating: boolean
  isRetryingExtraction: boolean
  onRetryExtraction: (id: number) => void
  onStatusChange: (id: number, status: JobStatus) => void
}

const publishedFormatter = new Intl.RelativeTimeFormat('id-ID', {
  numeric: 'auto',
})

function formatPublishedAt(value: string) {
  const hours = Math.floor(
    (Date.now() - new Date(value).getTime()) / (60 * 60 * 1000),
  )

  if (hours < 1) return publishedFormatter.format(-1, 'minute')
  if (hours < 24) return publishedFormatter.format(-hours, 'hour')
  return publishedFormatter.format(-Math.floor(hours / 24), 'day')
}

export function JobCard({
  job,
  isUpdating,
  isRetryingExtraction,
  onRetryExtraction,
  onStatusChange,
}: JobCardProps) {
  return (
    <article className="job-card">
      <div className="job-card-topline">
        <span className="source-label">{job.source}</span>
        <time dateTime={job.publishedAt}>{formatPublishedAt(job.publishedAt)}</time>
      </div>

      <div className="job-heading">
        <div>
          <h2>{job.jobTitle}</h2>
          <p className="company-name">{job.companyName}</p>
        </div>
        {job.location && <span className="location-label">{job.location}</span>}
      </div>

      <ul className="qualification-list">
        {job.qualifications.map((qualification) => (
          <li key={qualification}>{qualification}</li>
        ))}
      </ul>

      <div className="job-card-actions">
        <a
          className="button button-primary"
          href={job.externalUrl}
          target="_blank"
          rel="noopener noreferrer"
          onClick={(event) => {
            const isOpenClick =
              event.button === 0 && !event.shiftKey && !event.altKey

            if (isOpenClick && !isUpdating && job.status === 'NEW') {
              onStatusChange(job.id, 'VIEWED')
            }
          }}
          onAuxClick={(event) => {
            if (event.button === 1 && !isUpdating && job.status === 'NEW') {
              onStatusChange(job.id, 'VIEWED')
            }
          }}
        >
          Lihat Lowongan
        </a>
        <div className="secondary-actions" aria-label="Aksi lowongan">
          {job.canRetryExtraction && (
            <button
              className="button button-secondary"
              type="button"
              disabled={isRetryingExtraction}
              aria-busy={isRetryingExtraction}
              aria-label={`Coba ekstrak ulang untuk ${job.jobTitle}`}
              onClick={() => onRetryExtraction(job.id)}
            >
              {isRetryingExtraction ? 'Mencoba ulang...' : 'Coba ekstrak ulang'}
            </button>
          )}
          {job.status !== 'SAVED' && (
            <button
              className="button button-secondary"
              type="button"
              disabled={isUpdating}
              onClick={() => onStatusChange(job.id, 'SAVED')}
            >
              Simpan
            </button>
          )}
          {job.status !== 'APPLIED' && (
            <button
              className="button button-secondary"
              type="button"
              disabled={isUpdating}
              onClick={() => onStatusChange(job.id, 'APPLIED')}
            >
              Tandai Dilamar
            </button>
          )}
          {job.status !== 'IGNORED' && (
            <button
              className="button button-quiet"
              type="button"
              disabled={isUpdating}
              onClick={() => onStatusChange(job.id, 'IGNORED')}
            >
              Abaikan
            </button>
          )}
        </div>
      </div>
    </article>
  )
}
