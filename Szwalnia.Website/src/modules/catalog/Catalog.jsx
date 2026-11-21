import { useState } from 'react'
import { api } from '../../shared/api'
import { money, number } from '../../shared/format'
import { Table } from '../../shared/Table'

function Details({ tab, row, materials }) {
  switch (tab) {
    case 'products':
      return (
        <>
          <p>
            {number(row.metersPerSet)} m / komplet · szerokość {row.widthCm} cm
          </p>
          <p className="mt-1 text-xs text-slate-600">
            Krojenie: {money(row.cutCostPerSet)}/komplet +{' '}
            {money(row.cutSetupCost)} · szycie: {money(row.sewCostPerSet)}
            /komplet
          </p>
        </>
      )
    case 'rolls':
      return (
        <>
          <p>
            {number(row.availableMeters)} m dostępne ·{' '}
            {number(row.reservedMeters)} m zarezerwowane
          </p>
          <p className="mt-1 text-xs text-slate-600">
            Szerokość {row.widthCm} cm ·{' '}
            {materials.find((material) => material.id === row.materialId)?.name}
          </p>
        </>
      )
    case 'batches':
      return (
        <>
          <p>
            {row.available} dostępnych / {row.quantity} skrojonych kompletów
          </p>
          <p className="mt-1 text-xs text-slate-600">
            Zlecenie krojenia #{row.cutOrderId} · przydzielono{' '}
            {row.allocatedQuantity}
          </p>
        </>
      )
    case 'seamstresses':
      return row.phone || '—'
    default:
      return 'Materiał produkcyjny'
  }
}

export function Catalog({ tab, options, busy, run, edit, onAssign }) {
  const [search, setSearch] = useState('')
  const batches = tab === 'batches'
  const rows = (options[tab] ?? []).filter((row) =>
    `${row.name ?? row.product ?? ''} ${row.variant ?? ''}`
      .toLowerCase()
      .includes(search.trim().toLowerCase()),
  )
  function remove(row) {
    if (!window.confirm('Usunąć rekord z katalogu?')) return
    run(
      () =>
        api(
          `/catalog/${tab}/${row.id}?version=${encodeURIComponent(row.version)}`,
          { method: 'DELETE' },
        ),
      'Usunięto rekord.',
    )
  }
  return (
    <>
      <label className="mb-5 max-w-md">
        Szukaj
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          className="mt-1"
          placeholder="Wpisz nazwę lub wariant"
        />
      </label>
      {tab === 'products' && (
        <p className="mb-4 text-sm text-slate-600">
          Stawki i normy są kopiowane do nowych zleceń.
        </p>
      )}
      <Table
        headings={[
          batches ? 'Partia / produkt' : 'Nazwa',
          'Szczegóły',
          'Akcje',
        ]}
      >
        {rows.map((row) => (
          <tr key={row.id}>
            <td>
              <div className="font-semibold">
                {row.name || `#${row.id} · ${row.product}`}
              </div>
              {row.variant && (
                <div className="mt-1 text-xs text-slate-600">{row.variant}</div>
              )}
            </td>
            <td>
              <Details tab={tab} row={row} materials={options.materials} />
            </td>
            <td>
              <div className="flex flex-wrap gap-2">
                {batches ? (
                  row.available > 0 && (
                    <button
                      type="button"
                      className="btn secondary"
                      disabled={busy}
                      onClick={() => onAssign(row)}
                    >
                      Przekaż do szycia
                    </button>
                  )
                ) : (
                  <>
                    <button
                      type="button"
                      className="btn secondary"
                      disabled={busy}
                      onClick={() => edit({ resource: tab, row })}
                    >
                      Edytuj
                    </button>
                    <button
                      type="button"
                      className="btn danger"
                      disabled={busy}
                      onClick={() => remove(row)}
                    >
                      Usuń
                    </button>
                  </>
                )}
              </div>
            </td>
          </tr>
        ))}
      </Table>
    </>
  )
}
