import {
  useEffect,
  useRef,
  useState,
  type ChangeEvent,
  type DragEvent,
  type FormEvent,
  type KeyboardEvent as ReactKeyboardEvent,
} from 'react'
import { parseCvProfile } from '../services/jobApi'
import type { CandidateProfile } from '../types/job'

interface ProfileModalProps {
  profile: CandidateProfile
  isSaving: boolean
  errorMessage: string | null
  onClose: () => void
  onSave: (profile: CandidateProfile) => Promise<void>
}

interface TagInputFieldProps {
  label: string
  items: string[]
  placeholder: string
  disabled: boolean
  inputRef?: React.RefObject<HTMLInputElement | null>
  onChange: (items: string[]) => void
}

function mergeUniqueItems(existing: string[], incoming: string[]): string[] {
  const seen = new Set(existing.map((item) => item.toLocaleLowerCase()))
  const next = [...existing]

  for (const raw of incoming) {
    const trimmed = raw.trim()
    if (!trimmed) continue
    const key = trimmed.toLocaleLowerCase()
    if (!seen.has(key)) {
      seen.add(key)
      next.push(trimmed)
    }
  }

  return next
}

function TagInputField({
  label,
  items,
  placeholder,
  disabled,
  inputRef,
  onChange,
}: TagInputFieldProps) {
  const [draft, setDraft] = useState('')

  const commitDraft = (rawText = draft) => {
    const parts = rawText.split(/[,\n]/).map((item) => item.trim())
    const next = mergeUniqueItems(items, parts)
    if (next.length !== items.length) {
      onChange(next)
    }
    setDraft('')
  }

  const handleKeyDown = (event: ReactKeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault()
      if (draft.trim()) {
        commitDraft()
      }
      return
    }

    if (event.key === 'Backspace' && !draft && items.length > 0) {
      onChange(items.slice(0, -1))
    }
  }

  return (
    <div className="profile-field">
      <div className="profile-field-header">
        <span className="field-label">{label}</span>
        <span className="field-count">{items.length} item</span>
      </div>
      <div className={`tag-editor${disabled ? ' is-disabled' : ''}`}>
        {items.length > 0 && (
          <ul className="tag-list" aria-label={label}>
            {items.map((item) => (
              <li className="tag-chip" key={item}>
                <span>{item}</span>
                <button
                  type="button"
                  className="tag-remove"
                  aria-label={`Hapus ${item}`}
                  disabled={disabled}
                  onClick={() =>
                    onChange(items.filter((entry) => entry !== item))
                  }
                >
                  ×
                </button>
              </li>
            ))}
          </ul>
        )}
        <div className="tag-input-row">
          <input
            ref={inputRef}
            type="text"
            value={draft}
            disabled={disabled}
            placeholder={placeholder}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={handleKeyDown}
            onBlur={() => {
              if (draft.trim()) commitDraft()
            }}
          />
          <button
            type="button"
            className="button button-secondary tag-add-button"
            disabled={disabled || !draft.trim()}
            onClick={() => commitDraft()}
          >
            Tambah
          </button>
        </div>
      </div>
    </div>
  )
}

export function ProfileModal({
  profile,
  isSaving,
  errorMessage,
  onClose,
  onSave,
}: ProfileModalProps) {
  const [targetRoles, setTargetRoles] = useState<string[]>(profile.targetRoles)
  const [technicalSkills, setTechnicalSkills] = useState<string[]>(
    profile.technicalSkills,
  )
  const [experienceLevel, setExperienceLevel] = useState(
    profile.experienceLevel,
  )
  const [summary, setSummary] = useState(profile.summary)
  const [isParsingCv, setIsParsingCv] = useState(false)
  const [isDraggingCv, setIsDraggingCv] = useState(false)
  const [cvStatusMessage, setCvStatusMessage] = useState<string | null>(null)
  const [cvErrorMessage, setCvErrorMessage] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const firstInputRef = useRef<HTMLInputElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const mouseDownOnBackdropRef = useRef(false)
  const isBusy = isSaving || isParsingCv

  useEffect(() => {
    firstInputRef.current?.focus()
  }, [])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isBusy) {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isBusy, onClose])

  const processCvFile = async (file: File) => {
    if (!file.name.toLocaleLowerCase().endsWith('.pdf')) {
      setCvErrorMessage('Format file harus PDF (.pdf).')
      return
    }

    setIsParsingCv(true)
    setCvErrorMessage(null)
    setCvStatusMessage(null)
    setValidationError(null)

    try {
      const parsed = await parseCvProfile(file)
      const mergedRoles = mergeUniqueItems(targetRoles, parsed.targetRoles)
      const mergedSkills = mergeUniqueItems(
        technicalSkills,
        parsed.technicalSkills,
      )
      const addedRolesCount = mergedRoles.length - targetRoles.length
      const addedSkillsCount = mergedSkills.length - technicalSkills.length

      setTargetRoles(mergedRoles)
      setTechnicalSkills(mergedSkills)
      setExperienceLevel(parsed.experienceLevel)
      setSummary(parsed.summary)
      setCvStatusMessage(
        `CV "${file.name}" selesai dianalisis: +${addedRolesCount} target peran dan +${addedSkillsCount} skill teknis ditambahkan (isian sebelumnya tetap dipertahankan), sedangkan level & ringkasan diperbarui. Klik Simpan Profil CV untuk menerapkan.`,
      )
    } catch (error) {
      setCvErrorMessage(
        error instanceof Error ? error.message : 'Gagal menganalisis CV PDF.',
      )
    } finally {
      setIsParsingCv(false)
    }
  }

  const handleCvUpload = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    await processCvFile(file)
  }

  const handleDrop = async (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setIsDraggingCv(false)
    if (isBusy) return

    const file = event.dataTransfer.files?.[0]
    if (file) {
      await processCvFile(file)
    }
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setValidationError(null)

    if (targetRoles.length === 0) {
      setValidationError('Minimal masukkan 1 target peran.')
      return
    }

    if (technicalSkills.length === 0) {
      setValidationError('Minimal masukkan 1 skill teknis.')
      return
    }

    await onSave({
      targetRoles,
      technicalSkills,
      experienceLevel: experienceLevel.trim() || 'Fresh Graduate',
      summary: summary.trim(),
    })
  }

  const activeError = validationError ?? cvErrorMessage ?? errorMessage

  return (
    <div
      className="modal-backdrop"
      role="dialog"
      aria-modal="true"
      aria-labelledby="profile-modal-title"
      onMouseDown={(event) => {
        mouseDownOnBackdropRef.current =
          event.target === event.currentTarget
      }}
      onClick={(event) => {
        const clickedBackdrop =
          mouseDownOnBackdropRef.current &&
          event.target === event.currentTarget
        mouseDownOnBackdropRef.current = false

        if (clickedBackdrop && !isBusy && !cvStatusMessage) {
          onClose()
        }
      }}
    >
      <div className="modal-panel">
        <div className="modal-header">
          <div>
            <h2 id="profile-modal-title">Profil CV & Target Kerja</h2>
            <p className="modal-subtitle">
              Digunakan AI untuk menghitung skor kecocokan pada lowongan baru atau ekstraksi ulang.
            </p>
          </div>
          <button
            className="button button-quiet"
            type="button"
            onClick={onClose}
            disabled={isBusy}
          >
            Tutup
          </button>
        </div>

        <div className="modal-body">
          <div
            className={`cv-upload-box${isDraggingCv ? ' is-dragging' : ''}`}
            onDragOver={(event) => {
              event.preventDefault()
              if (!isBusy) setIsDraggingCv(true)
            }}
            onDragLeave={() => setIsDraggingCv(false)}
            onDrop={handleDrop}
          >
            <div>
              <p className="field-label">Isi Otomatis dari CV / Resume (PDF)</p>
              <p className="cv-upload-hint">
                Tarik & lepas file PDF ke sini atau pilih file (maks. 5 MB). Target peran & skill baru ditambahkan tanpa menghapus isian manual; level & ringkasan diperbarui.
              </p>
            </div>
            <input
              ref={fileInputRef}
              className="sr-only"
              type="file"
              accept=".pdf,application/pdf"
              onChange={handleCvUpload}
              disabled={isBusy}
            />
            <button
              className="button button-secondary"
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={isBusy}
            >
              {isParsingCv && <span className="spinner" aria-hidden="true" />}
              {isParsingCv ? 'Menganalisis PDF...' : 'Upload CV (PDF)'}
            </button>
          </div>

          {cvStatusMessage && (
            <div className="refresh-banner has-new-jobs" role="status">
              <span className="refresh-indicator" aria-hidden="true" />
              <p>{cvStatusMessage}</p>
            </div>
          )}

          {activeError && (
            <div className="error-banner" role="alert">
              <p>{activeError}</p>
            </div>
          )}

          <form
            id="profile-cv-form"
            className="profile-form"
            onSubmit={handleSubmit}
          >
            <TagInputField
              label="Target Peran"
              items={targetRoles}
              placeholder="Ketik peran (mis. Frontend Developer) lalu tekan Enter"
              disabled={isBusy}
              inputRef={firstInputRef}
              onChange={setTargetRoles}
            />

            <TagInputField
              label="Skill Teknis"
              items={technicalSkills}
              placeholder="Ketik skill (mis. React, .NET, Linux) lalu tekan Enter"
              disabled={isBusy}
              onChange={setTechnicalSkills}
            />

            <label className="profile-field">
              <span className="field-label">Level Pengalaman</span>
              <input
                type="text"
                value={experienceLevel}
                onChange={(event) => setExperienceLevel(event.target.value)}
                placeholder="Fresh Graduate"
                disabled={isBusy}
                required
              />
            </label>

            <label className="profile-field">
              <span className="field-label">Ringkasan CV / Profil</span>
              <textarea
                rows={4}
                value={summary}
                onChange={(event) => setSummary(event.target.value)}
                placeholder="Ringkasan singkat pengalaman, proyek, dan fokus karier Anda"
                disabled={isBusy}
              />
            </label>
          </form>
        </div>

        <div className="modal-actions">
          <button
            className="button button-secondary"
            type="button"
            onClick={onClose}
            disabled={isBusy}
          >
            Batal
          </button>
          <button
            className="button button-primary"
            type="submit"
            form="profile-cv-form"
            disabled={isBusy}
          >
            {isSaving ? 'Menyimpan...' : 'Simpan Profil CV'}
          </button>
        </div>
      </div>
    </div>
  )
}
