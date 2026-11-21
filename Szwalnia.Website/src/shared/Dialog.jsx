import { useEffect, useRef } from 'react'

export function Dialog({ title, busy, onClose, children }) {
  const element = useRef(null)
  useEffect(() => {
    const dialog = element.current
    dialog.showModal()
    return () => dialog.close()
  }, [])

  return (
    <dialog
      ref={element}
      aria-label={title}
      className="panel max-h-[90vh] w-[calc(100%-2rem)] max-w-2xl overflow-y-auto p-6 text-slate-800 backdrop:bg-slate-950/50"
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) onClose()
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget && !busy) onClose()
      }}
    >
      <div className="mb-6 flex items-center justify-between gap-4">
        <h2 className="text-xl font-bold">{title}</h2>
        <button
          type="button"
          className="btn secondary"
          disabled={busy}
          aria-label="Zamknij"
          onClick={onClose}
        >
          ×
        </button>
      </div>
      {children}
    </dialog>
  )
}
