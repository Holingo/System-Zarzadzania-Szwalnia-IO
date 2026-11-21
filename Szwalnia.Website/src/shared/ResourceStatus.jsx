export function ResourceStatus({ resource }) {
  if (resource.error)
    return (
      <p
        role="alert"
        className="mb-4 rounded-lg bg-red-50 p-4 text-sm text-red-800"
      >
        {resource.error}{' '}
        <button
          type="button"
          className="ml-2 underline"
          onClick={resource.retry}
        >
          Spróbuj ponownie
        </button>
      </p>
    )
  if (resource.loading)
    return (
      <p role="status" className="mb-4 text-sm text-slate-500">
        Ładowanie danych…
      </p>
    )
  return null
}
