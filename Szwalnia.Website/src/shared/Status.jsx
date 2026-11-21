const labels = {
  Queued: 'W kolejce',
  Cutting: 'W trakcie krojenia',
  Cut: 'Skrojone',
  New: 'Nowe',
  Sewing: 'W trakcie szycia',
  Sewn: 'Uszyte',
  Pending: 'Oczekuje',
  Running: 'Generowanie',
  Completed: 'Gotowy',
  Failed: 'Błąd',
}

export const statusLabel = (status) => labels[status] ?? status

export function Status({ value }) {
  const done = ['Cut', 'Sewn', 'Completed'].includes(value)
  return (
    <span className={done ? 'badge !bg-mint !text-forest' : 'badge'}>
      {statusLabel(value)}
    </span>
  )
}
