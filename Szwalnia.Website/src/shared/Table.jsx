export function Table({ headings, children, emptyText }) {
  return (
    <div className="panel overflow-x-auto">
      <table>
        <thead>
          <tr>
            {headings.map((heading) => (
              <th scope="col" key={heading}>
                {heading}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {children.length ? (
            children
          ) : (
            <tr>
              <td
                colSpan={headings.length}
                className="py-10 text-center text-slate-500"
              >
                {emptyText ?? 'Brak danych do wyświetlenia.'}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
