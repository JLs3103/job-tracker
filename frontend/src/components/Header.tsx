export type AppPage = 'jobs' | 'career'

interface HeaderProps {
  activePage: AppPage
  onPageChange: (page: AppPage) => void
  lastRefresh: string | null
  isRefreshing: boolean
  onRefresh: () => void
  onOpenProfile: () => void
}

const dateFormatter = new Intl.DateTimeFormat('id-ID', {
  hour: '2-digit',
  minute: '2-digit',
})

export function Header({
  activePage,
  onPageChange,
  lastRefresh,
  isRefreshing,
  onRefresh,
  onOpenProfile,
}: HeaderProps) {
  const refreshLabel = lastRefresh
    ? `Terakhir diperbarui pukul ${dateFormatter.format(new Date(lastRefresh))}`
    : 'Belum pernah diperbarui'

  return (
    <header className="app-header">
      <div className="header-left">
        <a
          className="brand"
          href="/"
          aria-label="Kerja IT, beranda"
          onClick={(event) => {
            event.preventDefault()
            onPageChange('jobs')
          }}
        >
          <img
            className="brand-icon"
            src="/favicon.svg"
            width="40"
            height="40"
            alt=""
            aria-hidden="true"
          />
          <span>
            <strong>Kerja IT</strong>
            <small>Personal job tracker</small>
          </span>
        </a>
        <nav className="primary-nav" aria-label="Menu utama">
          <button
            type="button"
            className={`primary-nav-item${activePage === 'jobs' ? ' is-active' : ''}`}
            aria-current={activePage === 'jobs' ? 'page' : undefined}
            onClick={() => onPageChange('jobs')}
          >
            Lowongan IT
          </button>
          <button
            type="button"
            className={`primary-nav-item${activePage === 'career' ? ' is-active' : ''}`}
            aria-current={activePage === 'career' ? 'page' : undefined}
            onClick={() => onPageChange('career')}
          >
            Career Tracker
          </button>
        </nav>
      </div>
      <div className="header-actions">
        <p className="last-refresh" aria-live="polite">{refreshLabel}</p>
        <button
          className="button button-secondary"
          type="button"
          onClick={onOpenProfile}
        >
          Profil CV
        </button>
        <button
          className="button button-primary refresh-button"
          type="button"
          onClick={onRefresh}
          disabled={isRefreshing}
        >
          {isRefreshing && <span className="spinner" aria-hidden="true" />}
          {isRefreshing ? 'Memeriksa...' : 'Refresh Loker'}
        </button>
      </div>
    </header>
  )
}
