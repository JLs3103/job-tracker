interface EmptyStateProps {
  hasSearch: boolean
  hasSourceFilter: boolean
}

export function EmptyState({
  hasSearch,
  hasSourceFilter,
}: EmptyStateProps) {
  const isFiltered = hasSearch || hasSourceFilter

  return (
    <section className="empty-state" aria-live="polite">
      <span className="empty-index" aria-hidden="true">0</span>
      <div>
        <h2>
          {isFiltered ? 'Belum ada yang cocok' : 'Belum ada lowongan di sini'}
        </h2>
        <p>
          {isFiltered
            ? 'Coba ubah kata pencarian atau pilih sumber lain.'
            : 'Gunakan Refresh Loker untuk memeriksa lowongan IT terbaru.'}
        </p>
      </div>
    </section>
  )
}
