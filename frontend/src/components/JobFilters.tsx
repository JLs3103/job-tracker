import { jobSources, type JobSource } from '../types/job'

interface JobFiltersProps {
  search: string
  source: JobSource | 'ALL'
  onSearchChange: (value: string) => void
  onSourceChange: (value: JobSource | 'ALL') => void
}

const sourceLabels: Record<JobSource | 'ALL', string> = {
  ALL: 'Semua sumber',
  Disnakerja: 'Disnakerja',
  Bukajobs: 'Bukajobs',
  BrowseLoker: 'BrowseLoker',
  LokerBlog: 'Loker Blog',
}

export function JobFilters({
  search,
  source,
  onSearchChange,
  onSourceChange,
}: JobFiltersProps) {
  return (
    <div className="job-filters">
      <label className="search-field">
        <span className="field-label">Cari lowongan</span>
        <input
          type="search"
          value={search}
          placeholder="Posisi, perusahaan, atau kualifikasi"
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </label>
      <label className="source-field">
        <span className="field-label">Sumber</span>
        <select
          value={source}
          onChange={(event) => {
            const selectedSource = event.target.value
            onSourceChange(
              selectedSource === 'ALL'
                ? 'ALL'
                : jobSources.find((jobSource) => jobSource === selectedSource) ??
                    'ALL',
            )
          }}
        >
          <option value="ALL">{sourceLabels.ALL}</option>
          {jobSources.map((jobSource) => (
            <option value={jobSource} key={jobSource}>
              {sourceLabels[jobSource]}
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}
