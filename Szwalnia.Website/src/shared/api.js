export class ApiError extends Error {
  constructor(message, status) {
    super(message)
    this.status = status
  }
}
export async function api(path, options = {}) {
  let response
  try {
    response = await fetch('/api' + path, {
      credentials: 'same-origin',
      ...options,
      headers: {
        'Content-Type': 'application/json',
        'X-Requested-With': 'Szwalnia',
        ...options.headers,
      },
      body:
        options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw new ApiError('Nie można połączyć się z serwerem.', 0)
  }
  if (!response.ok) {
    let data = {}
    try {
      data = await response.json()
    } catch {
      /* status fallback */
    }
    const details = data.errors
      ? Object.values(data.errors).flat().join(' ')
      : ''
    const fallback =
      {
        401: 'Sesja wygasła. Zaloguj się ponownie.',
        403: 'Nie masz uprawnień do tej operacji.',
        429: 'Zbyt wiele prób. Spróbuj ponownie za minutę.',
      }[response.status] || 'Operacja nie powiodła się.'
    throw new ApiError(details || data.title || fallback, response.status)
  }
  return response.status === 204 ? null : response.json()
}
