import type { JobPosting, JobStatus } from '../types/job'

interface JobCardProps {
  job: JobPosting
  profileSkills?: string[]
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

function getMatchTier(score: number): 'high' | 'medium' | 'low' {
  if (score >= 75) return 'high'
  if (score >= 50) return 'medium'
  return 'low'
}

export function JobCard({
  job,
  profileSkills = [],
  isUpdating,
  isRetryingExtraction,
  onRetryExtraction,
  onStatusChange,
}: JobCardProps) {
  const normalizedProfileSkills = new Set(
    profileSkills.map((skill) => skill.trim().toLocaleLowerCase()),
  )
  const targetRoles = job.targetRoles ?? []
  const technicalSkills = job.technicalSkills ?? []

  return (
    <article className="job-card">
      <div className="job-card-topline">
        <div className="job-card-meta-left">
          <span className="source-label">{job.source}</span>
          {job.matchScore !== null && (
            <span
              className={`match-badge match-${getMatchTier(job.matchScore)}`}
            >
              {job.matchScore}% Cocok
            </span>
          )}
        </div>
        <time dateTime={job.publishedAt}>{formatPublishedAt(job.publishedAt)}</time>
      </div>

      <div className="job-heading">
        <div>
          <h2>{job.jobTitle}</h2>
          <p className="company-name">{job.companyName}</p>
        </div>
        {job.location && <span className="location-label">{job.location}</span>}
      </div>

      {job.matchReason && (
        <p className="match-reason">{job.matchReason}</p>
      )}

      {(targetRoles.length > 0 || technicalSkills.length > 0) && (
        <div className="job-taxonomy">
          {targetRoles.length > 0 && (
            <div className="job-taxonomy-row">
              <span className="taxonomy-label">Target Peran:</span>
              <ul className="taxonomy-chips" aria-label="Target peran lowongan">
                {targetRoles.map((role) => (
                  <li className="role-chip" key={role}>
                    {role}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {technicalSkills.length > 0 && (
            <div className="job-taxonomy-row">
              <span className="taxonomy-label">Skill Teknis:</span>
              <ul className="taxonomy-chips" aria-label="Skill teknis lowongan">
                {technicalSkills.map((skill) => {
                  const isMatched = normalizedProfileSkills.has(
                    skill.trim().toLocaleLowerCase(),
                  )
                  return (
                    <li
                      className={`skill-chip${isMatched ? ' is-matched' : ''}`}
                      key={skill}
                    >
                      {skill}
                    </li>
                  )
                })}
              </ul>
            </div>
          )}
        </div>
      )}

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
