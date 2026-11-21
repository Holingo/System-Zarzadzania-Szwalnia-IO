import { number } from '../../shared/format'
import { ResourceStatus } from '../../shared/ResourceStatus'
import { useResource } from '../../shared/useResource'

const cuttingMetrics = [
  ['queued', 'W kolejce do krojenia'],
  ['cutting', 'W trakcie krojenia'],
]
const sewingMetrics = [
  ['readyBatches', 'Partie gotowe do przydziału'],
  ['availableSets', 'Komplety gotowe do szycia'],
  ['incomingSets', 'Komplety przed zakończeniem krojenia'],
  ['sewing', 'W trakcie szycia'],
  ['overdue', 'Opóźnione zlecenia szycia'],
]

export function Dashboard({
  query,
  options,
  canCut,
  canSew,
  navigate,
  onAssign,
  edit,
  busy,
}) {
  const summary = useResource('/dashboard', query, true)
  const metrics = [
    ...(canCut ? cuttingMetrics : []),
    ...(canSew ? sewingMetrics : []),
  ]
  const batches = options.batches
    .filter((batch) => batch.available > 0)
    .slice(0, 5)
  return (
    <>
      <ResourceStatus resource={summary} />
      {summary.data && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {metrics.map(([key, label]) => (
            <article
              key={key}
              className={`panel p-5 ${key === 'availableSets' ? '!bg-mint' : ''}`}
            >
              <p className="text-sm text-slate-600">{label}</p>
              <p className="mt-3 text-4xl font-semibold text-forest">
                {number(summary.data[key])}
              </p>
            </article>
          ))}
        </div>
      )}
      {canSew && (
        <section className="panel mt-6 p-6">
          <header className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-xl font-semibold text-forest">
              Partie do przekazania do szycia
            </h2>
            <button
              type="button"
              className="btn secondary"
              disabled={busy}
              onClick={() => navigate('batches')}
            >
              Wszystkie partie
            </button>
          </header>
          {batches.length ? (
            <ul className="mt-4 divide-y divide-slate-200">
              {batches.map((batch) => (
                <li
                  key={batch.id}
                  className="flex flex-wrap items-center justify-between gap-3 py-4"
                >
                  <div>
                    <p className="font-semibold">
                      #{batch.id} · {batch.product} · {batch.variant}
                    </p>
                    <p className="mt-1 text-sm text-slate-500">
                      Dostępne komplety: {number(batch.available)}
                    </p>
                  </div>
                  <button
                    type="button"
                    className="btn"
                    disabled={busy}
                    onClick={() => onAssign(batch)}
                  >
                    Przydziel chałupniczce
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="mt-4 text-sm text-slate-600">
              Brak gotowych kompletów do przydziału.
            </p>
          )}
        </section>
      )}
      {canCut && (
        <div className="mt-6 flex flex-wrap gap-3">
          <button
            type="button"
            className="btn"
            disabled={busy || !options.products.length}
            onClick={() => edit({ resource: 'cuts' })}
          >
            Dodaj zlecenie krojenia
          </button>
          <button
            type="button"
            className="btn secondary"
            disabled={busy}
            onClick={() => navigate('cuts')}
          >
            Lista zleceń krojenia
          </button>
        </div>
      )}
    </>
  )
}
