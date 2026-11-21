const currency = new Intl.NumberFormat('pl-PL', {
  style: 'currency',
  currency: 'PLN',
})
const quantity = new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 2 })
const dateTime = new Intl.DateTimeFormat('pl-PL', {
  timeZone: 'Europe/Warsaw',
  dateStyle: 'short',
  timeStyle: 'short',
})

export const money = (value) => currency.format(value ?? 0)
export const number = (value) => quantity.format(value ?? 0)
export const localDateTime = (value) => dateTime.format(new Date(value))

export function today() {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Warsaw',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const values = Object.fromEntries(
    parts.map(({ type, value }) => [type, value]),
  )
  return [values.year, values.month, values.day].join('-')
}

export function monday() {
  const date = new Date(today() + 'T12:00:00Z')
  date.setUTCDate(date.getUTCDate() - ((date.getUTCDay() + 6) % 7))
  return date.toISOString().slice(0, 10)
}
