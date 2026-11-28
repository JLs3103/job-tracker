import { useEffect, useRef } from 'react'

interface ConfirmDialogProps {
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  tone?: 'danger' | 'primary'
  isAlertOnly?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  title,
  message,
  confirmLabel = 'Ya, Lanjutkan',
  cancelLabel = 'Batal',
  tone = 'primary',
  isAlertOnly = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const actionButtonRef = useRef<HTMLButtonElement>(null)
  const mouseDownOnBackdropRef = useRef(false)

  useEffect(() => {
    actionButtonRef.current?.focus()
  }, [])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onCancel()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onCancel])

  return (
    <div
      className="modal-backdrop"
      role="alertdialog"
      aria-modal="true"
      aria-labelledby="dialog-title"
      aria-describedby="dialog-message"
      onMouseDown={(event) => {
        mouseDownOnBackdropRef.current =
          event.target === event.currentTarget
      }}
      onClick={(event) => {
        const clickedBackdrop =
          mouseDownOnBackdropRef.current &&
          event.target === event.currentTarget
        mouseDownOnBackdropRef.current = false
        if (clickedBackdrop) {
          onCancel()
        }
      }}
    >
      <div className="modal-panel dialog-panel">
        <div className="modal-header">
          <div>
            <h2 id="dialog-title">{title}</h2>
          </div>
          <button
            className="button button-quiet"
            type="button"
            onClick={onCancel}
          >
            Tutup
          </button>
        </div>

        <div className="modal-body">
          <p id="dialog-message" className="dialog-message">
            {message}
          </p>
        </div>

        <div className="modal-actions">
          {!isAlertOnly && (
            <button
              className="button button-secondary"
              type="button"
              onClick={onCancel}
            >
              {cancelLabel}
            </button>
          )}
          <button
            ref={actionButtonRef}
            className={`button ${
              tone === 'danger' ? 'button-danger' : 'button-primary'
            }`}
            type="button"
            onClick={onConfirm}
          >
            {isAlertOnly ? 'Mengerti' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
