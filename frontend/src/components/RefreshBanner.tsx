interface RefreshBannerProps {
  message: string
  newJobsCount: number
}

export function RefreshBanner({
  message,
  newJobsCount,
}: RefreshBannerProps) {
  return (
    <div
      className={`refresh-banner${newJobsCount > 0 ? ' has-new-jobs' : ''}`}
      role="status"
    >
      <span className="refresh-indicator" aria-hidden="true" />
      <p>{message}</p>
    </div>
  )
}
