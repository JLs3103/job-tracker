import type { JobPosting, JobStatus } from '../types/job'

interface JobCardProps {
  job: JobPosting
  isUpdating: boolean
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
          onClick={() => {
            if (job.status === 'NEW') onStatusChange(job.id, 'VIEWED')
          }}
        >
          Lihat Lowongan
        </a>
        <div className="secondary-actions" aria-label="Aksi lowongan">
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
