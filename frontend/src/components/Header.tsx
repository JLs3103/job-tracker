interface HeaderProps {
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
      <a className="brand" href="/" aria-label="Kerja IT, beranda">
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
