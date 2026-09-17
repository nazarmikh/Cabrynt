export function formatDuration(minutes: number) {
  const totalSeconds = Math.max(0, Math.round(minutes * 60))
  const wholeMinutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60

  return `${wholeMinutes} min ${seconds.toString().padStart(2, "0")} sec`
}

export function formatDurationCorrection(minutes: number) {
  const totalSeconds = Math.round(minutes * 60)
  const sign = totalSeconds > 0 ? "+" : totalSeconds < 0 ? "-" : ""
  const absoluteSeconds = Math.abs(totalSeconds)
  const wholeMinutes = Math.floor(absoluteSeconds / 60)
  const seconds = absoluteSeconds % 60

  return `${sign}${wholeMinutes} min ${seconds.toString().padStart(2, "0")} sec`
}
