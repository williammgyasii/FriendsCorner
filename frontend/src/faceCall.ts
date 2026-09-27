import { describeFace, type FaceTile } from './face.ts'

export type SignalPayload =
  | { kind: 'unavailable' }
  | { kind: 'offer' | 'answer'; description: RTCSessionDescriptionInit }
  | { kind: 'ice'; candidate: RTCIceCandidateInit }

type RemoteFace = 'absent' | 'waiting' | 'live' | 'unavailable'

export class FaceCall {
  private pc: RTCPeerConnection | null = null
  private local: MediaStream | null = null
  private generation = 0
  private starting = false
  private ready = false
  private queue: SignalPayload[] = []
  private pendingIce: RTCIceCandidateInit[] = []
  private remoteDescriptionSet = false
  private ownsLocal = false
  private tail: Promise<void> = Promise.resolve()
  private readonly sendPayload: (payload: SignalPayload) => void
  private readonly video: HTMLVideoElement
  private readonly onTile: (tile: FaceTile) => void
  private readonly iceServers: () => Promise<RTCIceServer[]>

  constructor(
    sendPayload: (payload: SignalPayload) => void,
    video: HTMLVideoElement,
    onTile: (tile: FaceTile) => void,
    iceServers: () => Promise<RTCIceServer[]> = async () => [],
  ) {
    this.sendPayload = sendPayload
    this.video = video
    this.onTile = onTile
    this.iceServers = iceServers
  }

  otherLeft() {
    this.stop()
    this.publish('absent')
  }

  async start(seat: 'A' | 'B', shared: MediaStream | null = null) {
    if (this.pc || this.starting) {
      return
    }

    this.starting = true
    const generation = this.generation
    this.publish('waiting')
    const iceServers = await this.iceServers()
    if (generation !== this.generation) {
      return
    }
    const pc = new RTCPeerConnection({ iceServers })
    pc.onicecandidate = (event) => {
      if (event.candidate) {
        this.sendPayload({ kind: 'ice', candidate: event.candidate.toJSON() })
      }
    }
    pc.ontrack = (event) => {
      const [stream] = event.streams
      if (stream) {
        this.video.srcObject = stream
        void this.video.play().catch(() => undefined)
      }
      this.publish('live')
    }

    let stream = shared
    let denied = false
    if (!stream) {
      try {
        stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true })
        this.ownsLocal = true
      } catch {
        denied = true
      }
    }

    if (generation !== this.generation) {
      if (this.ownsLocal) {
        stream?.getTracks().forEach((track) => track.stop())
      }
      this.ownsLocal = false
      pc.close()
      return
    }

    this.pc = pc
    this.local = stream
    if (stream) {
      for (const track of stream.getTracks()) {
        pc.addTrack(track, stream)
      }
    } else {
      pc.addTransceiver('video', { direction: 'recvonly' })
      pc.addTransceiver('audio', { direction: 'recvonly' })
    }

    this.ready = true
    for (const payload of this.queue.splice(0)) {
      this.enqueue(payload)
    }

    if (denied) {
      this.sendPayload({ kind: 'unavailable' })
    }

    if (seat === 'A') {
      const offer = await pc.createOffer()
      await pc.setLocalDescription(offer)
      if (pc.localDescription) {
        this.sendPayload({ kind: 'offer', description: pc.localDescription })
      }
    }
  }

  receive(payload: SignalPayload) {
    if (!this.ready || !this.pc) {
      this.queue.push(payload)
      return
    }
    this.enqueue(payload)
  }

  stop() {
    this.generation += 1
    this.starting = false
    this.ready = false
    if (this.ownsLocal) {
      this.local?.getTracks().forEach((track) => track.stop())
    }
    this.ownsLocal = false
    this.local = null
    this.pc?.close()
    this.pc = null
    this.queue = []
    this.pendingIce = []
    this.remoteDescriptionSet = false
    this.tail = Promise.resolve()
    this.video.srcObject = null
  }

  private enqueue(payload: SignalPayload) {
    this.tail = this.tail.then(() => this.apply(payload)).catch(() => undefined)
  }

  private async apply(payload: SignalPayload) {
    const pc = this.pc
    if (!pc) {
      return
    }

    if (payload.kind === 'unavailable') {
      this.publish('unavailable')
      return
    }

    if (payload.kind === 'ice') {
      if (!this.remoteDescriptionSet) {
        this.pendingIce.push(payload.candidate)
        return
      }
      await pc.addIceCandidate(payload.candidate)
      return
    }

    await pc.setRemoteDescription(payload.description)
    this.remoteDescriptionSet = true
    const ice = this.pendingIce.splice(0)
    for (const candidate of ice) {
      await pc.addIceCandidate(candidate)
    }

    if (payload.kind === 'offer') {
      const answer = await pc.createAnswer()
      await pc.setLocalDescription(answer)
      if (pc.localDescription) {
        this.sendPayload({ kind: 'answer', description: pc.localDescription })
      }
    }
  }

  private publish(remote: RemoteFace) {
    this.onTile(describeFace(remote).tile)
  }
}
