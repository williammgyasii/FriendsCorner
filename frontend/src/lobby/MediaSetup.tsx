import { Mic, Video } from 'lucide-react'
import { useEffect, useState, useSyncExternalStore } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import type { Faces } from '../faces.ts'
import { cameraChosen, micChosen, setMedia } from '../store/devicesSlice.ts'
import type { AppDispatch, RootState } from '../store/index.ts'

type Device = { id: string; label: string }

function useDevices(kind: MediaDeviceKind, stream: MediaStream | null) {
  const [devices, setDevices] = useState<Device[]>([])
  useEffect(() => {
    const media = typeof navigator === 'undefined' ? undefined : navigator.mediaDevices
    if (!media?.enumerateDevices) {
      return
    }
    // Labels only appear after camera permission, so list again once a stream exists.
    void media.enumerateDevices().then((all) =>
      setDevices(
        all
          .filter((device) => device.kind === kind && device.deviceId)
          .map((device, index) => ({ id: device.deviceId, label: device.label || `${kind === 'videoinput' ? 'Camera' : 'Mic'} ${index + 1}` })),
      ),
    )
  }, [kind, stream])
  return devices
}

function useMicLevel(stream: MediaStream | null, on: boolean) {
  const [level, setLevel] = useState(0)
  useEffect(() => {
    const track = stream?.getAudioTracks()[0]
    if (!stream || !track || !on || typeof AudioContext === 'undefined') {
      setLevel(0)
      return
    }
    const context = new AudioContext()
    const analyser = context.createAnalyser()
    analyser.fftSize = 256
    context.createMediaStreamSource(new MediaStream([track])).connect(analyser)
    const samples = new Uint8Array(analyser.frequencyBinCount)
    let frame = 0
    const read = () => {
      analyser.getByteFrequencyData(samples)
      const average = samples.reduce((sum, value) => sum + value, 0) / samples.length
      setLevel(Math.min(1, average / 60))
      frame = requestAnimationFrame(read)
    }
    read()
    return () => {
      cancelAnimationFrame(frame)
      void context.close()
    }
  }, [stream, on])
  return level
}

export function MediaSetup({ faces }: { faces: Faces }) {
  const dispatch = useDispatch<AppDispatch>()
  const devices = useSelector((state: RootState) => state.devices)
  const { local } = useSyncExternalStore(faces.subscribe, faces.get)
  const cameras = useDevices('videoinput', local)
  const mics = useDevices('audioinput', local)
  const level = useMicLevel(local, devices.mic)

  return (
    <section aria-label="Camera and mic" className="flex flex-col gap-4 rounded-3xl bg-card p-4 shadow-sm ring-1 ring-border">
      <h2 className="text-lg font-semibold">Your camera and mic</h2>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex min-w-0 flex-col gap-2">
          <label className="flex items-center justify-between gap-3">
            <span className="flex items-center gap-2 font-medium">
              <Video className="size-4" aria-hidden /> Camera
            </span>
            <Switch aria-label="Camera" checked={devices.camera} onCheckedChange={(camera) => dispatch(setMedia({ camera }))} />
          </label>
          {cameras.length > 1 && (
            <Select value={devices.cameraId ?? cameras[0].id} onValueChange={(id) => dispatch(cameraChosen(id))}>
              <SelectTrigger aria-label="Choose camera" className="w-full min-w-0">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="tw">
                {cameras.map((camera) => (
                  <SelectItem key={camera.id} value={camera.id}>
                    {camera.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        </div>
        <div className="flex min-w-0 flex-col gap-2">
          <label className="flex items-center justify-between gap-3">
            <span className="flex items-center gap-2 font-medium">
              <Mic className="size-4" aria-hidden /> Mic
            </span>
            <Switch aria-label="Mic" checked={devices.mic} onCheckedChange={(mic) => dispatch(setMedia({ mic }))} />
          </label>
          {mics.length > 1 && (
            <Select value={devices.micId ?? mics[0].id} onValueChange={(id) => dispatch(micChosen(id))}>
              <SelectTrigger aria-label="Choose mic" className="w-full min-w-0">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="tw">
                {mics.map((mic) => (
                  <SelectItem key={mic.id} value={mic.id}>
                    {mic.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          <div className="flex h-3 items-end gap-1" aria-hidden>
            {Array.from({ length: 12 }, (_, bar) => (
              <span
                key={bar}
                className="flex-1 rounded-full transition-all duration-75"
                style={{
                  height: level * 12 > bar ? '100%' : '35%',
                  background: level * 12 > bar ? (bar > 9 ? 'var(--seat-b)' : '#17a864') : 'var(--muted)',
                }}
              />
            ))}
          </div>
        </div>
      </div>
      {!local && <p className="text-sm text-muted-foreground">Allow camera access to see yourself here.</p>}
    </section>
  )
}
