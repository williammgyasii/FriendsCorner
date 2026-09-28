import { useEffect, useRef } from 'react'

// Always muted: the call's own <video> plays the other person's sound once.
export function FaceVideo({ stream, mirrored }: { stream: MediaStream; mirrored?: boolean }) {
  const video = useRef<HTMLVideoElement>(null)

  useEffect(() => {
    const element = video.current
    if (!element) {
      return
    }
    element.srcObject = stream
    void element.play().catch(() => undefined)
  }, [stream])

  return (
    <video
      ref={video}
      autoPlay
      playsInline
      muted
      className={`size-full object-cover ${mirrored ? '-scale-x-100' : ''}`}
    />
  )
}
